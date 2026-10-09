using System.Globalization;
using ZEPVE.Abstractions;

namespace ZEPVE.Core;

internal interface IPveGameAdapter
{
    void Execute(string command);
    void MoveBots(PlayerTeam team, ServerLifetime lifetime);
}

/// <summary>Behavior-preserving legacy round policy. No respawn execution or recovery implementation.</summary>
internal sealed class PveRoundController : IDisposable
{
    private readonly PlayerRegistry _registry;
    private readonly LifecycleWorkScheduler _scheduler;
    private readonly IPveGameAdapter _game;
    private readonly Func<double> _now;
    private ICoreWorkScope _work;
    private ICoreWorkScope _mapWork;
    private PveResumePlan? _plan;
    private ServerLifetime _round;
    private string _map = "";
    private double? _mapSettingsDue;
    public PveRoundStatus Status { get; private set; } = new(PvePhase.Unbound, "off", 0, 0);
    public bool AuthorityActive => _registry.State.Loaded && SuiteRuntime.Legacy is not null;
    public PveRoundController(PlayerRegistry registry, LifecycleWorkScheduler scheduler, IPveGameAdapter game, Func<double> now)
    {
        _registry = registry;
        _scheduler = scheduler;
        _game = game;
        _now = now;
        _work = scheduler.CreateScope();
        _mapWork = scheduler.CreateScope();
    }
    public void BeginMap(string map)
    {
        ResetWork();
        _mapWork.Dispose();
        _mapWork = _scheduler.CreateScope();
        _map = map;
        _mapSettingsDue = null;
        _plan = null;
        SetStatus(new(PvePhase.Unbound, "off", 0, 0));
        ApplyMapSettings();
    }
    public void BridgeChanged()
    {
        _registry.Record(LifecycleEventKind.Authority, AuthorityActive ? "Core gameplay writer; adapted legacy writer removed" : "waiting for adapted legacy; no gameplay fallback");
        if (!AuthorityActive) { _work.Dispose(); _mapWork.Dispose(); return; }
        _mapWork.Dispose();
        _mapWork = _scheduler.CreateScope();
        if (_plan is not null && _registry.ValidateServer(_round, ServerValidity.Round, out _))
        {
            ResetWork();
            SchedulePending();
            if (_mapSettingsDue is not null) ScheduleMapSettings(_plan.Settings);
        }
        else ApplyMapSettings(); // Late attachment does not invent an elapsed infection deadline.
    }
    private void ApplyMapSettings()
    {
        var settings = SuiteRuntime.Legacy?.Settings;
        if (settings is null || !_registry.State.MapOpen) return;
        ValidateSettings(settings);
        _mapSettingsDue = _now() + 1;
        ScheduleMapSettings(settings);
    }
    private void ScheduleMapSettings(LegacyPveSettings settings)
    {
        var token = _registry.CaptureServer();
        _mapWork.TryScheduleServer(token, (float)Math.Max(0, _mapSettingsDue!.Value - _now()), () =>
        { _mapSettingsDue = null; WriteMapTime(settings, token, ServerValidity.Map); }, out _, ServerValidity.Map);
    }
    public void BeginRound()
    {
        ResetWork();
        _plan = null;
        var bridge = SuiteRuntime.Legacy;
        if (bridge is null) { SetStatus(new(PvePhase.Unbound, "off", 0, 0)); return; }
        var settings = bridge.Settings;
        ValidateSettings(settings);
        _registry.Reconcile();
        _round = _registry.CaptureServer();
        var countdown = Math.Max(3, (int)MathF.Ceiling(PickProfile(CountHumans(), settings).InfectionDelay));
        ExecuteRound($"zr_infect_spawn_time_min {countdown}");
        ExecuteRound($"zr_infect_spawn_time_max {countdown}");
        bridge.ShowInfectionCountdown(countdown);
        SetStatus(new(settings.Enabled ? PvePhase.Preparing : PvePhase.Disabled, "off", 0, 0));
        // Legacy applies after 1 second, THEN waits InfectionDelay. Preserve this offset.
        _plan = new(_map, settings, null, Status, _now() + 1, null, null, null);
        SchedulePending();
    }
    public void EndRound()
    {
        ResetWork();
        _plan = null;
        SetStatus(new(PvePhase.Closed, Status.Profile, Status.BotQuota, Status.RespawnDelay));
    }
    public PveResumePlan? ExportHotResume() => _plan is { } plan ? plan with { MapSettingsDue = _mapSettingsDue }
        : _mapSettingsDue is not null && SuiteRuntime.Legacy is { } bridge
            ? new(_map, bridge.Settings, null, Status, null, null, null, null, _mapSettingsDue) : null;
    public void ResumeHot(PveResumePlan plan)
    {
        if (plan.MapName != _map || plan.Status.Phase == PvePhase.Closed) return;
        ResetWork();
        _mapWork.Dispose();
        _mapWork = _scheduler.CreateScope();
        _mapSettingsDue = plan.MapSettingsDue;
        _plan = plan;
        _round = _registry.CaptureServer();
        SetStatus(plan.Status);
        SchedulePending();
        if (_mapSettingsDue is not null) ScheduleMapSettings(plan.Settings);
    }
    private void SchedulePending()
    {
        if (_plan is null || !AuthorityActive) return;
        if (_plan.ApplyDue is { } apply) ScheduleAt(apply, ApplyProfile);
        if (_plan.ReleaseDue is { } release) ScheduleAt(release, Release);
        if (_plan.QuotaDue is { } quota) ScheduleAt(quota, ApplyQuota);
        if (_plan.MoveDue is { } move) ScheduleAt(move, MoveReleasedBots);
    }
    private void ScheduleAt(double due, Action action)
    {
        if (!_work.TryScheduleServer(_round, (float)Math.Max(0, due - _now()), () =>
            { if (AuthorityActive) action(); }, out _))
            throw new InvalidOperationException("Unable to schedule Core round work; no legacy fallback.");
    }
    private void ApplyProfile()
    {
        var plan = _plan!;
        _plan = plan with { ApplyDue = null };
        if (!plan.Settings.Enabled) return;
        _registry.Reconcile();
        var settings = plan.Settings;
        var profile = PickProfile(CountHumans(), settings);
        var commands = new[]
        {
            "mp_autoteambalance 0", "mp_limitteams 0", "mp_humanteam CT", "mp_warmuptime 0",
            "mp_warmuptime_all_players_connected 0", "mp_warmup_pausetimer 0", "mp_freezetime 0"
        };
        foreach (var command in commands) ExecuteRound(command);
        WriteMapTime(settings, _round, ServerValidity.Round);
        foreach (var command in ProfileCommands(settings, profile)) ExecuteRound(command);
        if (!_registry.ValidateServer(_round, ServerValidity.Round, out _)) return;
        SetStatus(new(PvePhase.AwaitingRelease, profile.Name, profile.BotQuota, profile.RespawnDelay));
        ExecuteRound("bot_kick");
        // A kick can synchronously end a round; do not schedule obsolete follow-up work.
        if (!_registry.ValidateServer(_round, ServerValidity.Round, out _)) return;
        _plan = _plan! with { Profile = profile, Status = Status, ReleaseDue = _now() + profile.InfectionDelay };
        ScheduleAt(_plan.ReleaseDue.Value, Release);
    }
    private void Release()
    {
        var plan = _plan!;
        SetStatus(new(PvePhase.Released, plan.Profile!.Name, plan.Profile.BotQuota, plan.Profile.RespawnDelay));
        _plan = plan with { Status = Status, ReleaseDue = null };
        ExecuteRound("bot_join_team T");
        ExecuteRound("bot_kick");
        if (!_registry.ValidateServer(_round, ServerValidity.Round, out _)) return;
        _plan = _plan with { QuotaDue = _now() + plan.Settings.BotAddDelay, MoveDue = _now() + plan.Settings.ReleaseMoveDelay };
        ScheduleAt(_plan.QuotaDue.Value, ApplyQuota);
        ScheduleAt(_plan.MoveDue.Value, MoveReleasedBots);
    }
    private void ApplyQuota()
    {
        var quota = _plan!.Profile!.BotQuota;
        _plan = _plan with { QuotaDue = null };
        ExecuteRound("bot_quota_mode normal");
        ExecuteRound($"bot_quota {quota}");
    }
    private void MoveReleasedBots()
    {
        _plan = _plan! with { MoveDue = null };
        _game.MoveBots(PlayerTeam.Terrorist, _round);
    }
    private void WriteMapTime(LegacyPveSettings settings, ServerLifetime token, ServerValidity validity)
    {
        var minutes = _map.StartsWith("ze_", StringComparison.OrdinalIgnoreCase) ? settings.ZeRoundTimeMinutes : settings.NonZeRoundTimeMinutes;
        var value = minutes.ToString("0.##", CultureInfo.InvariantCulture);
        foreach (var command in new[] { $"mp_roundtime {value}", $"mp_roundtime_defuse {value}", $"mp_roundtime_hostage {value}" })
            if (AuthorityActive && _registry.ValidateServer(token, validity, out _)) _game.Execute(command);
    }
    private int CountHumans()
    {
        var count = 0;
        foreach (var p in _registry.Players)
            if (p.Connected && !p.IsBot && !p.IsHLTV && p.Team is PlayerTeam.CounterTerrorist or PlayerTeam.Terrorist) count++;
        return count;
    }
    private void SetStatus(PveRoundStatus status)
    {
        Status = status;
        _registry.Record(LifecycleEventKind.Policy, $"{status.Phase}; profile={status.Profile}; quota={status.BotQuota}; respawn={status.RespawnDelay}");
    }
    private void ResetWork() { _work.Dispose(); _work = _scheduler.CreateScope(); }
    private void ExecuteRound(string command)
    {
        if (AuthorityActive && _registry.ValidateServer(_round, ServerValidity.Round, out _)) _game.Execute(command);
    }
    public void Dispose() { _work.Dispose(); _mapWork.Dispose(); }

    internal static PveProfile PickProfile(int humans, LegacyPveSettings config) => humans <= 1
        ? new("solo", 2, 1, 16, 7, 4.8f, 4, config.SoloBotQuota, config.InfectionDelay)
        : humans <= 2 ? new("duo", 2, 1, 12, 5, 4, 4, config.DuoBotQuota, config.InfectionDelay)
        : humans <= 6 ? new("coop", 2, 1, 8, 4, 3.5f, 3.5f, Math.Clamp(humans * 2, config.CoopMinBotQuota, config.CoopMaxBotQuota), config.InfectionDelay)
        : new("group", 2, 2, 7, 3, 3, 3, Math.Clamp(humans * 2, config.GroupMinBotQuota, config.GroupMaxBotQuota), config.InfectionDelay);

    internal static string[] ProfileCommands(LegacyPveSettings s, PveProfile p) =>
    [
        $"mp_buytime {s.BuyTimeSeconds}", $"mp_buy_anywhere {Bool(s.BuyAnywhere)}", "bot_join_team CT",
        "bot_join_after_player 0", "bot_auto_vacate 0", $"bot_difficulty {s.BotDifficulty}",
        $"bot_coop_idle_max_vision_distance {s.BotIdleVisionDistance.ToString("0", CultureInfo.InvariantCulture)}",
        "bot_chatter off", "bot_defer_to_human_goals 0", "bot_defer_to_human_items 0", "bot_allow_rogues 1",
        "bot_stop 0", "bot_path_require_reachable_goal 0", "bot_quota_mode normal", "bot_quota 0",
        "zr_enable 1", $"zr_napalm_enable {Bool(s.ZrNapalmEnable)}", $"zr_infect_spawn_type {s.ZrInfectSpawnType}",
        $"zr_infect_spawn_warning {Bool(s.ZrInfectSpawnWarning)}", $"zr_default_winner_team {s.ZrDefaultWinnerTeam}",
        $"zr_infect_min_count_req {p.MinPlayersToInfect}", $"zr_infect_spawn_mz_min_count {p.MotherZombieMinCount}",
        $"zr_infect_spawn_mz_ratio {p.MotherZombieRatio}", $"zr_infect_spawn_time_min {Math.Max(3, (int)MathF.Ceiling(p.InfectionDelay))}",
        $"zr_infect_spawn_time_max {Math.Max(3, (int)MathF.Ceiling(p.InfectionDelay))}",
        $"zr_respawn_delay {p.RespawnDelay.ToString("0.0", CultureInfo.InvariantCulture)}",
        $"zr_knockback_scale {p.KnockbackScale.ToString("0.0", CultureInfo.InvariantCulture)}",
        $"zr_napalm_burn_duration {p.NapalmBurnDuration.ToString("0.0", CultureInfo.InvariantCulture)}"
    ];
    private static int Bool(bool value) => value ? 1 : 0;
    internal static void ValidateSettings(LegacyPveSettings settings)
    {
        if (settings.CoopMinBotQuota > settings.CoopMaxBotQuota || settings.GroupMinBotQuota > settings.GroupMaxBotQuota
            || settings.SoloBotQuota < 0 || settings.DuoBotQuota < 0 || settings.CoopMinBotQuota < 0 || settings.GroupMinBotQuota < 0
            || !float.IsFinite(settings.InfectionDelay) || settings.InfectionDelay < 0
            || !float.IsFinite(settings.BotAddDelay) || settings.BotAddDelay < 0
            || !float.IsFinite(settings.ReleaseMoveDelay) || settings.ReleaseMoveDelay < 0
            || !float.IsFinite(settings.ZeRoundTimeMinutes) || settings.ZeRoundTimeMinutes <= 0
            || !float.IsFinite(settings.NonZeRoundTimeMinutes) || settings.NonZeRoundTimeMinutes <= 0
            || !float.IsFinite(settings.BotIdleVisionDistance))
            throw new InvalidOperationException("Invalid legacy PvE lifecycle settings; Core writer fails closed.");
    }
}
