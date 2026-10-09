using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using ZEPVE.Abstractions;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace ZEPVE.Core;

public sealed class CorePlugin : BasePlugin
{
    public override string ModuleName => "ZEPVE Core";
    public override string ModuleVersion => "0.2.0-c";
    public override string ModuleAuthor => "ZEPVE";
    public override string ModuleDescription => "Lifecycle authority and migrated PvE round policy; ZombieReborn executes respawn.";
    private PlayerRegistry _registry = null!;
    private LifecycleWorkScheduler? _scheduler;
    private PveRoundController? _policy;
    private CoreRuntime? _api;
    private ICoreWorkScope? _probes;
    private Timer? _reconcileTimer;
    private bool _commandsRegistered;
    private ILegacyPveBridge? _boundLegacy;
    private readonly List<Action> _unsubscribe = new();

    public override void Load(bool hotReload)
    {
        if (SuiteRuntime.Current is { State.Loaded: true }) throw new InvalidOperationException("Core authority already active; duplicate provider refused.");
        _registry = new(new CounterStrikePlayerSource());
        _scheduler = new(_registry, new NativeTimerDispatcher(), error => Logger.LogError(error, "Core delayed work failed"));
        _policy = new(_registry, _scheduler, new NativePveGameAdapter(_registry), () => Server.CurrentTime);
        _api = new(_registry, _scheduler, _policy);
        SuiteRuntime.Publish(_api);
        try
        {
            SuiteRuntime.LegacyChanged += BridgeChanged;
            _unsubscribe.Add(() => SuiteRuntime.LegacyChanged -= BridgeChanged);
            _probes = _scheduler.CreateScope();
            Listen<Listeners.OnMapStart>(_ => InitializeMap());
            Listen<Listeners.OnMapEnd>(EndMap);
            Listen<Listeners.OnClientConnected>(slot => _registry.Connect(slot));
            Listen<Listeners.OnClientPutInServer>(slot => _registry.Observe(slot));
            Listen<Listeners.OnClientDisconnect>(slot => _registry.Disconnect(slot));
            Handle<EventPlayerSpawn>((e, _) => ObserveLife(e.Userid, false));
            Handle<EventPlayerDeath>((e, _) => ObserveLife(e.Userid, true));
            Handle<EventPlayerTeam>((e, _) => { if (e.Userid is { IsValid: true } player) _registry.Observe(player.Slot); return HookResult.Continue; });
            Handle<EventRoundPrestart>((_, _) => { CloseRound(); return HookResult.Continue; });
            Handle<EventRoundStart>((_, _) =>
            {
                _registry.BeginRound();
                _registry.Reconcile();
                _boundLegacy?.ObserveRoundStart();
                _policy.BeginRound();
                Logger.LogInformation("Core round start: MapEpoch={MapEpoch} RoundEpoch={RoundEpoch}", _registry.MapEpoch, _registry.RoundEpoch);
                return HookResult.Continue;
            });
            Handle<EventRoundEnd>((_, _) => { CloseRound(); return HookResult.Continue; });
            AddCommand("css_zepve_status", "Core lifecycle authority status.", Status);
            AddCommand("css_zepve_probe", "Delayed lifetime probe: <slot> [seconds].", Probe);
            AddCommand("css_zepve_events", "Read bounded lifecycle recorder: [count: 1..64].", Events);
            _commandsRegistered = true;
            if (PlayableMap()) InitializeMap(!hotReload);
            var resume = SuiteRuntime.ResumePlan;
            SuiteRuntime.ResumePlan = null;
            if (hotReload && resume is not null) _policy.ResumeHot(resume);
            BridgeChanged();
            Logger.LogInformation("Core lifecycle authority loaded (hotReload={HotReload}, PluginLifetime={Lifetime}, GameplayAuthority={Authority})", hotReload, _registry.PluginLifetime, _policy.AuthorityActive);
        }
        catch { Unload(false); throw; }
    }
    private static bool PlayableMap() => !string.IsNullOrWhiteSpace(Server.MapName) && !Server.MapName.Equals("<empty>", StringComparison.OrdinalIgnoreCase);
    private void BridgeChanged()
    {
        var bridge = SuiteRuntime.Legacy;
        if (ReferenceEquals(bridge, _boundLegacy))
        {
            _registry.Record(LifecycleEventKind.Authority, _policy!.AuthorityActive ? "Core writer active; adapted legacy paths disabled" : "waiting for adapted legacy");
            return;
        }
        if (!ReferenceEquals(bridge, _boundLegacy))
        {
            _boundLegacy?.SuspendMapServices();
            _boundLegacy = bridge;
            if (_registry.State.MapOpen) bridge?.ObserveMapStart(Server.MapName, true);
        }
        _policy?.BridgeChanged();
    }
    private void InitializeMap(bool resetLegacyData = true)
    {
        _reconcileTimer?.Kill();
        _registry.BeginMap();
        _registry.Reconcile();
        _policy!.BeginMap(Server.MapName);
        _boundLegacy = SuiteRuntime.Legacy;
        _boundLegacy?.ObserveMapStart(Server.MapName, resetLegacyData);
        _reconcileTimer = new(0.5f, _registry.Reconcile, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        Logger.LogInformation("Core map initialization: MapEpoch={MapEpoch} RoundEpoch={RoundEpoch}", _registry.MapEpoch, _registry.RoundEpoch);
    }
    private void CloseRound() { _registry.InvalidateRound(); _policy!.EndRound(); }
    private void EndMap()
    {
        _registry.EndMap();
        _boundLegacy?.SuspendMapServices();
        _policy!.EndRound();
        _reconcileTimer?.Kill();
        _reconcileTimer = null;
    }
    public override void Unload(bool hotReload)
    {
        if (_api is null) return;
        // Only atomic Core hot reload carries a value-only policy plan, never old tokens or entities.
        SuiteRuntime.ResumePlan = hotReload ? _policy?.ExportHotResume() : null;
        _boundLegacy?.SuspendMapServices();
        _registry.Unload();
        _policy?.Dispose();
        _probes?.Dispose();
        _scheduler?.Dispose();
        _reconcileTimer?.Kill();
        _reconcileTimer = null;
        foreach (var unsubscribe in _unsubscribe) unsubscribe();
        _unsubscribe.Clear();
        if (_commandsRegistered)
        {
            RemoveCommand("css_zepve_status", Status);
            RemoveCommand("css_zepve_probe", Probe);
            RemoveCommand("css_zepve_events", Events);
        }
        SuiteRuntime.Withdraw(_api);
        _api = null;
        _commandsRegistered = false;
        Logger.LogInformation("Core lifecycle authority unloaded (hotReload={HotReload})", hotReload);
    }
    private void Listen<T>(T listener) where T : Delegate { RegisterListener(listener); _unsubscribe.Add(() => RemoveListener(listener)); }
    private void Handle<T>(GameEventHandler<T> handler) where T : GameEvent
    { RegisterEventHandler(handler, HookMode.Post); _unsubscribe.Add(() => DeregisterEventHandler(handler, HookMode.Post)); }
    private HookResult ObserveLife(CCSPlayerController? player, bool death)
    {
        if (player is { IsValid: true })
        {
            var observed = _registry.ObserveLifeEvent(player.Slot, player.EntityHandle.Raw, player.UserId, death);
            // Forward spawn only AFTER Core generation advancement, independent of plugin handler order.
            if (observed && !death) _boundLegacy?.ObserveSpawn(player.Slot);
        }
        return HookResult.Continue;
    }
    private void Status(CCSPlayerController? player, CommandInfo command)
    {
        _registry.Reconcile();
        command.ReplyToCommand($"[ZEPVE Core] Authority=Core GameplayAuthority={_api!.GameplayAuthorityActive} Phase={_api.Round.Phase} PluginLifetime={_registry.PluginLifetime} MapEpoch={_registry.MapEpoch} RoundEpoch={_registry.RoundEpoch} Humans={_registry.Humans.Count} ZombieBots={_registry.ZombieBots.Count} PendingWork={_scheduler!.PendingCount} FlightRecords={_registry.Recorder.Count}/{_registry.Recorder.Capacity} Dropped={_registry.Recorder.Dropped}");
        command.ReplyToCommand($"Profile={_api.Round.Profile} BotQuotaPolicy={_api.Round.BotQuota} RespawnDelayPolicy={_api.Round.RespawnDelay} RespawnExecutor={_api.Round.RespawnExecutor}");
        foreach (var context in _registry.Players.OrderBy(p => p.Slot))
            command.ReplyToCommand($"Slot={context.Slot} Role={context.Role} Connected={context.Connected} Alive={context.Alive} ConnectionGeneration={context.ConnectionGeneration} PawnGeneration={context.PawnGeneration}");
    }
    private static bool Admin(CCSPlayerController? player) => player is null || AdminManager.PlayerHasPermissions(player, "@css/root");
    private void Events(CCSPlayerController? player, CommandInfo command)
    {
        if (!Admin(player)) { command.ReplyToCommand("Requires server console or @css/root."); return; }
        var count = 32;
        if (command.ArgCount > 2 || (command.ArgCount == 2 && !int.TryParse(command.GetArg(1), out count)) || count is < 1 or > 64)
        { command.ReplyToCommand("Usage: css_zepve_events [count: 1..64]"); return; }
        foreach (var entry in _api!.ReadFlightRecorder(count))
            command.ReplyToCommand($"#{entry.Sequence} {entry.Kind} Map={entry.State.MapEpoch} Round={entry.State.RoundEpoch} Slot={entry.Slot} Connection={entry.ConnectionGeneration} Pawn={entry.PawnGeneration} {entry.Detail}");
    }
    private void Probe(CCSPlayerController? player, CommandInfo command)
    {
        if (!Admin(player)) { command.ReplyToCommand("Requires server console or @css/root."); return; }
        var delay = 5f;
        if (command.ArgCount is < 2 or > 3 || !int.TryParse(command.GetArg(1), out var slot)
            || (command.ArgCount == 3 && !float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out delay))
            || !float.IsFinite(delay) || delay is < 0.1f or > 60f)
        { command.ReplyToCommand("Usage: css_zepve_probe <slot> [seconds: 0.1..60]"); return; }
        if (_probes!.PendingCount >= 16 || !_api!.TryCapture(slot, out var token))
        { command.ReplyToCommand("Probe unavailable: no live pawn/round or 16 probes pending."); return; }
        void Result(string result, string reason) => Logger.LogInformation("Core delayed observation probe: Slot={Slot} Result={Result} Reason={Reason}", slot, result, reason);
        if (!_probes.TrySchedulePlayer(token, delay, _ => Result("ACCEPT", "current identity"), out _, canceled: reason => Result("REJECT", reason)))
        { command.ReplyToCommand("Probe unavailable: invalid lifetime or scheduler full."); return; }
        command.ReplyToCommand($"Observation probe queued for Slot={slot}; invalidation may reject it immediately.");
    }
}
