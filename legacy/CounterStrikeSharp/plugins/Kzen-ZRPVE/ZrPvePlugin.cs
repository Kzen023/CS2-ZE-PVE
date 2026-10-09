using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;

namespace KzenZrPve;

public sealed class ZrPvePlugin : BasePlugin
{
    public override string ModuleName => "Kzen ZR PvE";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Maxsun + Codex";
    public override string ModuleDescription => "PvE adapter for CS2Fixes Zombie:Reborn on ZE maps.";

    private const string Prefix = "[ZR-PvE]";
    private bool _enabled = true;
    private ZrPveConfig _config = new();
    private string _currentProfile = "off";
    private int _targetBotQuota;
    private bool _infectionReleased;
    private int _roundToken;
    private long _pathSequence;
    private readonly List<PathPoint> _pathHistory = new();
    private readonly Dictionary<int, BotWatchState> _botWatch = new();
    private readonly Dictionary<int, EscortState> _escorts = new();
    private readonly HashSet<int> _pendingRecoveries = new();
    private readonly HashSet<int> _cancelledRecoveries = new();
    private readonly Dictionary<int, string> _botStatuses = new();
    private readonly Dictionary<long, float> _reservedRecoveryPoints = new();

    public override void Load(bool hotReload)
    {
        _config = LoadConfig();
        _enabled = _config.Enabled;
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    private void OnMapStart(string mapName)
    {
        StopAllEscorts();
        _currentProfile = "off";
        _targetBotQuota = 0;
        _infectionReleased = false;
        _pathHistory.Clear();
        _botWatch.Clear();
        _escorts.Clear();
        _pendingRecoveries.Clear();
        _cancelledRecoveries.Clear();
        _botStatuses.Clear();
        _reservedRecoveryPoints.Clear();
        _pathSequence = 0;
        _roundToken++;
        AddTimer(Math.Max(0.1f, _config.RecordInterval), RecordHumanPath, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        AddTimer(Math.Max(0.25f, _config.WatchdogInterval), CheckZombieBots, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        AddTimer(Math.Max(0.5f, _config.StatusHudInterval), ShowBotStatusHud, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        AddTimer(1.0f, ApplyMapRoundTime, TimerFlags.STOP_ON_MAPCHANGE);
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _infectionReleased = false;
        StopAllEscorts();
        _pathHistory.Clear();
        _botWatch.Clear();
        _pendingRecoveries.Clear();
        _cancelledRecoveries.Clear();
        _botStatuses.Clear();
        _reservedRecoveryPoints.Clear();
        _pathSequence = 0;
        _roundToken++;
        StartInfectionCountdownHud();
        QueueApply("round start", hardResetBots: true);
        return HookResult.Continue;
    }

    private void StartInfectionCountdownHud()
    {
        var profile = PickProfile(CountHumanPlayers(), _config);
        var seconds = Math.Max(3, (int)MathF.Ceiling(profile.InfectionDelay));
        Server.ExecuteCommand($"zr_infect_spawn_time_min {seconds}");
        Server.ExecuteCommand($"zr_infect_spawn_time_max {seconds}");
        Server.ExecuteCommand($"css_kzen_countdown {seconds} 首轮感染");
        Server.PrintToConsole($"{Prefix} Infection countdown synchronized: {seconds}s.");
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (_infectionReleased && IsZombieBot(player))
        {
            SetBotStatus(player!, "复活等待回位");
            QueueBotRecovery(player!.Slot, "respawn", _config.RespawnRecoveryDelay);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        if (victim is { IsValid: true, IsBot: true })
        {
            StopEscort(victim.Slot);
            _botWatch.Remove(victim.Slot);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var attacker = @event.Attacker;
        if (IsZombieBot(victim) && attacker is { IsValid: true, IsBot: false, IsHLTV: false, Team: CsTeam.CounterTerrorist })
        {
            var pawn = victim!.PlayerPawn.Value;
            if (pawn is { IsValid: true, AbsOrigin: not null })
            {
                _botWatch[victim.Slot] = new BotWatchState(
                    CopyVector(pawn.AbsOrigin),
                    0.0f,
                    Server.CurrentTime + _config.RecoveryRetryDelay,
                    Server.CurrentTime);
            }
        }

        return HookResult.Continue;
    }

    private void QueueApply(string reason, bool hardResetBots = false)
    {
        AddTimer(1.0f, () => ApplyProfile(reason, hardResetBots), TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void ApplyProfile(string reason, bool hardResetBots)
    {
        if (!_enabled)
        {
            return;
        }

        if (!IsRealPlayableMap())
        {
            return;
        }

        var humans = CountHumanPlayers();
        var profile = PickProfile(humans, _config);
        _targetBotQuota = profile.BotQuota;

        Server.ExecuteCommand("mp_autoteambalance 0");
        Server.ExecuteCommand("mp_limitteams 0");
        Server.ExecuteCommand("mp_humanteam CT");
        Server.ExecuteCommand("mp_warmuptime 0");
        Server.ExecuteCommand("mp_warmuptime_all_players_connected 0");
        Server.ExecuteCommand("mp_warmup_pausetimer 0");
        Server.ExecuteCommand("mp_freezetime 0");
        ApplyMapRoundTime();
        Server.ExecuteCommand($"mp_buytime {_config.BuyTimeSeconds}");
        Server.ExecuteCommand($"mp_buy_anywhere {BoolToInt(_config.BuyAnywhere)}");
        Server.ExecuteCommand($"bot_join_team {(_infectionReleased ? "T" : "CT")}");
        Server.ExecuteCommand("bot_join_after_player 0");
        Server.ExecuteCommand("bot_auto_vacate 0");
        Server.ExecuteCommand($"bot_difficulty {_config.BotDifficulty}");
        Server.ExecuteCommand($"bot_coop_idle_max_vision_distance {_config.BotIdleVisionDistance:0}");
        Server.ExecuteCommand("bot_chatter off");
        Server.ExecuteCommand("bot_defer_to_human_goals 0");
        Server.ExecuteCommand("bot_defer_to_human_items 0");
        Server.ExecuteCommand("bot_allow_rogues 1");
        Server.ExecuteCommand("bot_stop 0");
        Server.ExecuteCommand("bot_path_require_reachable_goal 0");
        Server.ExecuteCommand("bot_quota_mode normal");
        Server.ExecuteCommand("bot_quota 0");

        Server.ExecuteCommand("zr_enable 1");
        Server.ExecuteCommand($"zr_napalm_enable {BoolToInt(_config.ZrNapalmEnable)}");
        Server.ExecuteCommand($"zr_infect_spawn_type {_config.ZrInfectSpawnType}");
        Server.ExecuteCommand($"zr_infect_spawn_warning {BoolToInt(_config.ZrInfectSpawnWarning)}");
        Server.ExecuteCommand($"zr_default_winner_team {_config.ZrDefaultWinnerTeam}");

        Server.ExecuteCommand($"zr_infect_min_count_req {profile.MinPlayersToInfect}");
        Server.ExecuteCommand($"zr_infect_spawn_mz_min_count {profile.MotherZombieMinCount}");
        Server.ExecuteCommand($"zr_infect_spawn_mz_ratio {profile.MotherZombieRatio}");
        Server.ExecuteCommand($"zr_infect_spawn_time_min {Math.Max(3, (int)MathF.Ceiling(profile.InfectionDelay))}");
        Server.ExecuteCommand($"zr_infect_spawn_time_max {Math.Max(3, (int)MathF.Ceiling(profile.InfectionDelay))}");
        Server.ExecuteCommand($"zr_respawn_delay {profile.RespawnDelay:0.0}");
        Server.ExecuteCommand($"zr_knockback_scale {profile.KnockbackScale:0.0}");
        Server.ExecuteCommand($"zr_napalm_burn_duration {profile.NapalmBurnDuration:0.0}");

        _currentProfile = profile.Name;

        if (hardResetBots)
        {
            _infectionReleased = false;
            Server.ExecuteCommand("bot_kick");
            var token = _roundToken;
            AddTimer(profile.InfectionDelay, () => ReleaseInfectionBots(token), TimerFlags.STOP_ON_MAPCHANGE);
        }
        else if (_infectionReleased)
        {
            MoveExistingBots(CsTeam.Terrorist);
        }
        else
        {
            MoveExistingBots(CsTeam.CounterTerrorist);
        }

        Server.PrintToConsole($"{Prefix} Applied {profile.Name} profile on {Server.MapName} for {humans} human player(s), bots={profile.BotQuota}, phase={(_infectionReleased ? "zombie" : "prepare")}: {reason}");
    }

    private void ReleaseInfectionBots(int roundToken)
    {
        if (!IsRealPlayableMap() || roundToken != _roundToken)
        {
            return;
        }

        _infectionReleased = true;
        Server.ExecuteCommand("bot_join_team T");
        Server.ExecuteCommand("bot_kick");
        AddTimer(_config.BotAddDelay, () =>
        {
            Server.ExecuteCommand("bot_quota_mode normal");
            Server.ExecuteCommand($"bot_quota {_targetBotQuota}");
        }, TimerFlags.STOP_ON_MAPCHANGE);
        AddTimer(_config.ReleaseMoveDelay, () => MoveExistingBots(CsTeam.Terrorist, botsOnly: true), TimerFlags.STOP_ON_MAPCHANGE);
        Server.PrintToConsole($"{Prefix} Infection phase released; maintaining {_targetBotQuota} zombie/T bots.");
    }

    private static bool IsRealPlayableMap()
    {
        var mapName = Server.MapName ?? string.Empty;
        return !string.IsNullOrWhiteSpace(mapName) && !mapName.Equals("<empty>", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyMapRoundTime()
    {
        var mapName = Server.MapName ?? string.Empty;
        var isZeMap = mapName.StartsWith("ze_", StringComparison.OrdinalIgnoreCase);
        var minutes = isZeMap
            ? _config.ZeRoundTimeMinutes
            : _config.NonZeRoundTimeMinutes;
        var value = minutes.ToString("0.##", CultureInfo.InvariantCulture);

        Server.ExecuteCommand($"mp_roundtime {value}");
        Server.ExecuteCommand($"mp_roundtime_defuse {value}");
        Server.ExecuteCommand($"mp_roundtime_hostage {value}");
        Server.PrintToConsole($"{Prefix} Map timer applied: {mapName} => {value} minute(s) ({(isZeMap ? "ZE" : "non-ZE")}).");
    }

    private static void AddBots(int count, CsTeam team)
    {
        var command = team == CsTeam.Terrorist ? "bot_add_t" : "bot_add_ct";

        for (var i = 0; i < count; i++)
        {
            Server.ExecuteCommand(command);
        }
    }

    private static void MoveExistingBots(CsTeam team, bool botsOnly = true)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsHLTV)
            {
                continue;
            }

            if (botsOnly && !player.IsBot)
            {
                continue;
            }

            if (player.Team != team)
            {
                player.ChangeTeam(team);
                player.SwitchTeam(team);
            }
        }
    }

    private void RecordHumanPath()
    {
        if (!_enabled || !IsRealPlayableMap())
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot || player.IsHLTV || player.Team != CsTeam.CounterTerrorist)
            {
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn is not { IsValid: true, LifeState: (byte)LifeState_t.LIFE_ALIVE } ||
                pawn.AbsOrigin == null ||
                (pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) == 0)
            {
                continue;
            }

            var origin = pawn.AbsOrigin;
            if (_pathHistory.Count > 0 && GetDistance(origin, _pathHistory[^1].Position) < _config.RecordDistance)
            {
                continue;
            }

            _pathHistory.Add(new PathPoint(++_pathSequence, CopyVector(origin), Server.CurrentTime));
            if (_pathHistory.Count > _config.MaxPathPoints)
            {
                _pathHistory.RemoveAt(0);
            }
        }
    }

    private void CheckZombieBots()
    {
        if (!_infectionReleased || !IsRealPlayableMap())
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsZombieBot(player))
            {
                continue;
            }

            var pawn = player!.PlayerPawn.Value;
            if (pawn is not { IsValid: true, LifeState: (byte)LifeState_t.LIFE_ALIVE } || pawn.AbsOrigin == null)
            {
                _botWatch.Remove(player.Slot);
                SetBotStatus(player, "等待复活");
                continue;
            }

            var origin = pawn.AbsOrigin;
            if (!_botWatch.TryGetValue(player.Slot, out var state))
            {
                _botWatch[player.Slot] = new BotWatchState(CopyVector(origin), 0.0f, 0.0f, Server.CurrentTime);
                SetBotStatus(player, "正在搜索玩家");
                continue;
            }

            if (GetDistance(origin, state.LastPosition) >= _config.StuckDistance)
            {
                state.LastPosition = CopyVector(origin);
                state.StuckSeconds = 0.0f;
                SetBotStatus(player, "正在搜索玩家");
            }
            else
            {
                state.StuckSeconds += _config.WatchdogInterval;
                SetBotStatus(player, $"疑似卡住 {state.StuckSeconds:0}/{_config.ForceRecoveryTime:0} 秒");
            }

            if (Server.CurrentTime - state.LastHumanHitTime >= _config.NoDamageRecoveryTime &&
                Server.CurrentTime >= state.NextRecoveryTime)
            {
                SetBotStatus(player, "长时间未受击，准备回位");
                QueueBotRecovery(player.Slot, "no_damage", 0.0f);
                state.LastHumanHitTime = Server.CurrentTime;
                state.NextRecoveryTime = Server.CurrentTime + _config.RecoveryRetryDelay;
                state.StuckSeconds = 0.0f;
                continue;
            }

            if (state.StuckSeconds >= _config.ForceRecoveryTime && Server.CurrentTime >= state.NextRecoveryTime)
            {
                SetBotStatus(player, "卡住，准备安全回位");
                QueueBotRecovery(player.Slot, "stuck", 0.0f);
                state.NextRecoveryTime = Server.CurrentTime + _config.RecoveryRetryDelay;
                state.StuckSeconds = 0.0f;
            }
        }
    }

    private void QueueBotRecovery(int slot, string reason, float delay, int attempt = 0)
    {
        if (!_pendingRecoveries.Add(slot))
        {
            return;
        }

        AddTimer(Math.Max(0.0f, delay), () =>
        {
            _pendingRecoveries.Remove(slot);
            TryRecoverZombieBot(slot, reason, attempt);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void QueueInitialRecoveries()
    {
        if (!_infectionReleased)
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (IsZombieBot(player))
            {
                SetBotStatus(player!, "开局等待回位");
                QueueBotRecovery(player!.Slot, "initial", 0.0f);
            }
        }
    }

    private void TryRecoverZombieBot(int slot, string reason, int attempt)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        if (!IsZombieBot(player))
        {
            _botStatuses.Remove(slot);
            return;
        }

        var pawn = player!.PlayerPawn.Value;
        if (pawn is not { IsValid: true, LifeState: (byte)LifeState_t.LIFE_ALIVE } || pawn.AbsOrigin == null)
        {
            SetBotStatus(player, "等待复活");
            return;
        }

        if (!TryFindRecoveryPoint(out var recoveryPoint))
        {
            SetBotStatus(player, "回位失败：没有有效路径");
            ShowDebugHud($"recovery skipped: no route point ({reason})");
            return;
        }

        if (!TryStartEscort(player, pawn, recoveryPoint, reason))
        {
            SetBotStatus(player, "回位取消：落点不安全");
            return;
        }

        _reservedRecoveryPoints[recoveryPoint.Sequence] = Server.CurrentTime + _config.RecoveryPointReservationTime;

        _botWatch[slot] = new BotWatchState(
            CopyVector(pawn.AbsOrigin!),
            0.0f,
            Server.CurrentTime + _config.RecoveryCooldown,
            Server.CurrentTime);
        SetBotStatus(player, "已安全回位，搜索玩家");
    }

    private bool TryFindRecoveryPoint(out PathPoint recoveryPoint)
    {
        recoveryPoint = default!;
        if (_pathHistory.Count == 0)
        {
            return false;
        }

        var humans = GetLiveHumanPawns();
        if (humans.Count == 0)
        {
            return false;
        }

        foreach (var sequence in _reservedRecoveryPoints.Where(entry => entry.Value <= Server.CurrentTime).Select(entry => entry.Key).ToArray())
        {
            _reservedRecoveryPoints.Remove(sequence);
        }

        var closestDistance = float.MaxValue;
        for (var i = _pathHistory.Count - 1; i >= 0; i--)
        {
            if (Server.CurrentTime - _pathHistory[i].Timestamp > _config.RecoveryPathMaxAge)
            {
                break;
            }

            var nearestHumanDistance = float.MaxValue;
            foreach (var human in humans)
            {
                var origin = human.AbsOrigin;
                if (origin != null)
                {
                    nearestHumanDistance = Math.Min(nearestHumanDistance, GetDistance(origin, _pathHistory[i].Position));
                }
            }

            if (nearestHumanDistance >= _config.RecoveryMinBehind &&
                nearestHumanDistance <= _config.RecoveryMaxBehind &&
                nearestHumanDistance < closestDistance)
            {
                if (_reservedRecoveryPoints.Keys.Any(sequence =>
                    Math.Abs(sequence - _pathHistory[i].Sequence) < Math.Max(1, _config.RecoveryLaneNodeGap)))
                {
                    continue;
                }

                recoveryPoint = _pathHistory[i];
                closestDistance = nearestHumanDistance;
            }
        }

        if (closestDistance == float.MaxValue)
        {
            return false;
        }

        return true;
    }

    private bool TryStartEscort(CCSPlayerController player, CCSPlayerPawn pawn, PathPoint recoveryPoint, string reason)
    {
        // Do one safe recovery placement only. Breadcrumb stepping caused rapid teleport
        // traffic and could release bots directly beside a player.
        var pointDistance = GetNearestHumanDistance(recoveryPoint.Position, GetLiveHumanPawns());
        if (pointDistance < _config.RecoveryMinBehind || pointDistance > _config.RecoveryMaxBehind)
        {
            ShowDebugHud($"recovery skipped: lane point out of range ({player.PlayerName})");
            return false;
        }

        pawn.Teleport(CopyVector(recoveryPoint.Position), pawn.AbsRotation, new Vector(0.0f, 0.0f, 0.0f));
        ShowDebugHud($"recovery placed: {player.PlayerName} ({reason}) at {FormatVector(recoveryPoint.Position)}");
        return true;
    }

    private void UpdateEscorts()
    {
        if (_escorts.Count == 0)
        {
            return;
        }

        var humans = GetLiveHumanPawns();
        foreach (var entry in _escorts.ToArray())
        {
            var slot = entry.Key;
            var state = entry.Value;
            var player = Utilities.GetPlayerFromSlot(slot);
            var pawn = player?.PlayerPawn.Value;
            if (!IsZombieBot(player) || pawn is not { IsValid: true, LifeState: (byte)LifeState_t.LIFE_ALIVE } || pawn.AbsOrigin == null)
            {
                StopEscort(slot);
                continue;
            }

            var origin = pawn.AbsOrigin;
            var nearestHumanDistance = GetNearestHumanDistance(origin, humans);
            if (nearestHumanDistance <= _config.EscortReleaseDistance)
            {
                StopEscort(slot);
                SetBotRecoveryCooldown(slot);
                ShowDebugHud($"escort released: {player!.PlayerName} near human ({nearestHumanDistance:0})");
                continue;
            }

            if (!TryGetPathPoint(state.TargetSequence, out var target))
            {
                continue;
            }

            if (Server.CurrentTime < state.NextStepTime)
            {
                continue;
            }

            // Bot-Controller's analog movement conflicts with native bot avoidance on recent CS2 builds.
            // Advance through actual human ground positions instead, keeping the bot locked until it is nearby.
            pawn.Teleport(CopyVector(target.Position), pawn.AbsRotation, new Vector(0.0f, 0.0f, 0.0f));
            state.TargetSequence = target.Sequence + 1;
            state.LastProgressPosition = CopyVector(target.Position);
            state.NextStepTime = Server.CurrentTime + _config.EscortStepInterval;
        }
    }

    private bool TryGetPathPoint(long sequence, out PathPoint point)
    {
        point = default!;
        if (_pathHistory.Count == 0)
        {
            return false;
        }

        var index = _pathHistory.BinarySearch(new PathPoint(sequence, new Vector(0.0f, 0.0f, 0.0f), 0.0f), PathPointSequenceComparer.Instance);
        if (index < 0)
        {
            index = ~index;
        }

        if (index >= _pathHistory.Count)
        {
            return false;
        }

        point = _pathHistory[index];
        return true;
    }

    private void SetBotRecoveryCooldown(int slot)
    {
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn is { IsValid: true, AbsOrigin: not null })
        {
            _botWatch[slot] = new BotWatchState(
                CopyVector(pawn.AbsOrigin),
                0.0f,
                Server.CurrentTime + _config.RecoveryCooldown,
                Server.CurrentTime);
        }
    }

    private static float GetNearestHumanDistance(Vector origin, IReadOnlyList<CCSPlayerPawn> humans)
    {
        var distance = float.MaxValue;
        foreach (var human in humans)
        {
            var humanOrigin = human.AbsOrigin;
            if (humanOrigin != null)
            {
                distance = Math.Min(distance, GetDistance(origin, humanOrigin));
            }
        }

        return distance;
    }

    private void StopEscort(int slot)
    {
        if (!_escorts.Remove(slot))
        {
            return;
        }

    }

    private void StopAllEscorts()
    {
        foreach (var slot in _escorts.Keys.ToArray())
        {
            StopEscort(slot);
        }
    }

    private static List<CCSPlayerPawn> GetLiveHumanPawns()
    {
        var humans = new List<CCSPlayerPawn>();
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot || player.IsHLTV || player.Team != CsTeam.CounterTerrorist)
            {
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn is { IsValid: true, LifeState: (byte)LifeState_t.LIFE_ALIVE })
            {
                humans.Add(pawn);
            }
        }

        return humans;
    }

    private static Vector CopyVector(Vector source)
    {
        return new Vector(source.X, source.Y, source.Z);
    }

    private void SetBotStatus(CCSPlayerController player, string status)
    {
        if (player.IsValid && player.IsBot)
        {
            _botStatuses[player.Slot] = status;
        }
    }

    private void ShowBotStatusHud()
    {
        if (!_config.StatusHudEnabled || !_infectionReleased)
        {
            return;
        }

        var lines = new List<string> { "ZR-PvE 人机状态" };
        var humans = GetLiveHumanPawns();
        foreach (var bot in Utilities.GetPlayers().Where(IsZombieBot).Take(_config.StatusHudMaxBots))
        {
            var pawn = bot!.PlayerPawn.Value;
            var distance = pawn?.AbsOrigin == null ? "--" : $"{GetNearestHumanDistance(pawn.AbsOrigin, humans):0}";
            var status = _botStatuses.TryGetValue(bot.Slot, out var value) ? value : "正在搜索玩家";
            lines.Add($"{bot.PlayerName}: {status} | 距离 {distance}");
        }

        if (lines.Count == 1)
        {
            lines.Add("没有存活的僵尸人机");
        }

        var message = string.Join("<br>", lines);
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is { IsValid: true, IsBot: false, IsHLTV: false })
            {
                if (_config.StatusHudLargeText)
                {
                    player.PrintToCenterHtml($"<font size='28'>{message}</font>", Math.Max(1, (int)MathF.Ceiling(_config.StatusHudInterval)));
                }
                else
                {
                    player.PrintToCenter(StripHudBreaks(message));
                }
            }
        }
    }

    private void ShowDebugHud(string message)
    {
        if (!_config.DebugHudEnabled && !_config.DebugChatEnabled && !_config.DebugConsoleEnabled)
        {
            return;
        }

        if (_config.DebugConsoleEnabled)
        {
            Server.PrintToConsole($"{Prefix} {StripHudBreaks(message)}");
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot || player.IsHLTV)
            {
                continue;
            }

            if (_config.DebugHudEnabled)
            {
                player.PrintToCenterHtml(message, _config.DebugHudDuration);
            }

            if (_config.DebugChatEnabled)
            {
                player.PrintToChat($"{Prefix} {StripHudBreaks(message)}");
            }
        }
    }

    private static string StripHudBreaks(string message)
    {
        return message.Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatVector(Vector? vector)
    {
        if (vector == null)
        {
            return "null";
        }

        return $"{vector.X:0},{vector.Y:0},{vector.Z:0}";
    }

    private static float GetDistance(Vector a, Vector b)
    {
        var x = a.X - b.X;
        var y = a.Y - b.Y;
        var z = a.Z - b.Z;

        return MathF.Sqrt(x * x + y * y + z * z);
    }

    private static int BoolToInt(bool value)
    {
        return value ? 1 : 0;
    }

    private static ZrPveConfig LoadConfig()
    {
        var config = new ZrPveConfig();
        var path = GetConfigPath();

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, config.ToFileText());
            Server.PrintToConsole($"{Prefix} Created config: {path}");
            return config;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                line = line[..commentIndex].Trim();
            }

            var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            config.Apply(parts[0], parts[1]);
        }

        return config;
    }

    private static string GetConfigPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var cssharpIndex = baseDirectory.IndexOf("addons\\counterstrikesharp", StringComparison.OrdinalIgnoreCase);
        if (cssharpIndex < 0)
        {
            cssharpIndex = baseDirectory.IndexOf("addons/counterstrikesharp", StringComparison.OrdinalIgnoreCase);
        }

        var cssharpDirectory = cssharpIndex >= 0
            ? baseDirectory[..(cssharpIndex + "addons\\counterstrikesharp".Length)]
            : Path.Combine(baseDirectory, "..", "..", "csgo", "addons", "counterstrikesharp");

        return Path.GetFullPath(Path.Combine(cssharpDirectory, "configs", "plugins", "Kzen-ZRPVE", "zrpve.cfg"));
    }

    private static bool IsZombieBot(CCSPlayerController? player)
    {
        return player is { IsValid: true, IsBot: true, IsHLTV: false, Team: CsTeam.Terrorist };
    }

    private static int CountHumanPlayers()
    {
        var count = 0;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot || player.IsHLTV)
            {
                continue;
            }

            if (player.Team == CsTeam.CounterTerrorist || player.Team == CsTeam.Terrorist)
            {
                count++;
            }
        }

        return count;
    }

    private static PveProfile PickProfile(int humans, ZrPveConfig config)
    {
        if (humans <= 1)
        {
            return new PveProfile(
                Name: "solo",
                MinPlayersToInfect: 2,
                MotherZombieMinCount: 1,
                MotherZombieRatio: 16,
                RespawnDelay: 7.0f,
                KnockbackScale: 4.8f,
                NapalmBurnDuration: 4.0f,
                BotQuota: config.SoloBotQuota,
                InfectionDelay: config.InfectionDelay);
        }

        if (humans <= 2)
        {
            return new PveProfile(
                Name: "duo",
                MinPlayersToInfect: 2,
                MotherZombieMinCount: 1,
                MotherZombieRatio: 12,
                RespawnDelay: 5.0f,
                KnockbackScale: 4.0f,
                NapalmBurnDuration: 4.0f,
                BotQuota: config.DuoBotQuota,
                InfectionDelay: config.InfectionDelay);
        }

        if (humans <= 6)
        {
            return new PveProfile(
                Name: "coop",
                MinPlayersToInfect: 2,
                MotherZombieMinCount: 1,
                MotherZombieRatio: 8,
                RespawnDelay: 4.0f,
                KnockbackScale: 3.5f,
                NapalmBurnDuration: 3.5f,
                BotQuota: Math.Clamp(humans * 2, config.CoopMinBotQuota, config.CoopMaxBotQuota),
                InfectionDelay: config.InfectionDelay);
        }

        return new PveProfile(
            Name: "group",
            MinPlayersToInfect: 2,
            MotherZombieMinCount: 2,
            MotherZombieRatio: 7,
            RespawnDelay: 3.0f,
            KnockbackScale: 3.0f,
            NapalmBurnDuration: 3.0f,
            BotQuota: Math.Clamp(humans * 2, config.GroupMinBotQuota, config.GroupMaxBotQuota),
            InfectionDelay: config.InfectionDelay);
    }

    private sealed record PveProfile(
        string Name,
        int MinPlayersToInfect,
        int MotherZombieMinCount,
        int MotherZombieRatio,
        float RespawnDelay,
        float KnockbackScale,
        float NapalmBurnDuration,
        int BotQuota,
        float InfectionDelay);

    private sealed class ZrPveConfig
    {
        public bool Enabled { get; private set; } = true;
        public int SoloBotQuota { get; private set; } = 10;
        public int DuoBotQuota { get; private set; } = 10;
        public int CoopMinBotQuota { get; private set; } = 6;
        public int CoopMaxBotQuota { get; private set; } = 12;
        public int GroupMinBotQuota { get; private set; } = 12;
        public int GroupMaxBotQuota { get; private set; } = 18;
        public float InfectionDelay { get; private set; } = 16.0f;
        public float ZeRoundTimeMinutes { get; private set; } = 60.0f;
        public float NonZeRoundTimeMinutes { get; private set; } = 2.5f;
        public int BuyTimeSeconds { get; private set; } = 9999;
        public bool BuyAnywhere { get; private set; } = true;
        public float BotAddDelay { get; private set; } = 0.2f;
        public float ReleaseMoveDelay { get; private set; } = 1.0f;
        public float InitialRecoveryDelay { get; private set; } = 3.0f;
        public float RespawnRecoveryDelay { get; private set; } = 3.0f;
        public float RecordInterval { get; private set; } = 0.25f;
        public float RecordDistance { get; private set; } = 128.0f;
        public int MaxPathPoints { get; private set; } = 2048;
        public float WatchdogInterval { get; private set; } = 1.0f;
        public float StuckDistance { get; private set; } = 128.0f;
        public float ForceRecoveryTime { get; private set; } = 8.0f;
        public float NoDamageRecoveryTime { get; private set; } = 10.0f;
        public float RecoveryMinBehind { get; private set; } = 800.0f;
        public float RecoveryMaxBehind { get; private set; } = 1600.0f;
        public float RecoveryPathMaxAge { get; private set; } = 60.0f;
        public float RecoveryCooldown { get; private set; } = 4.0f;
        public float HitRecoveryCooldown { get; private set; } = 15.0f;
        public float RecoveryRetryDelay { get; private set; } = 2.0f;
        public float RecoveryPathRetryDelay { get; private set; } = 1.0f;
        public int RecoveryPathRetryCount { get; private set; } = 6;
        public float RecoveryPointReservationTime { get; private set; } = 5.0f;
        public int RecoveryLaneCount { get; private set; } = 8;
        public int RecoveryLaneNodeGap { get; private set; } = 3;
        public float EscortUpdateInterval { get; private set; } = 0.1f;
        public float EscortNodeRadius { get; private set; } = 100.0f;
        public float EscortReleaseDistance { get; private set; } = 800.0f;
        public float EscortProgressDistance { get; private set; } = 40.0f;
        public float EscortSkipTime { get; private set; } = 2.0f;
        public float EscortTeleportTime { get; private set; } = 4.0f;
        public float EscortGiveUpTime { get; private set; } = 10.0f;
        public float EscortForwardMove { get; private set; } = 450.0f;
        public float EscortStepInterval { get; private set; } = 0.30f;
        public int EscortLaneCount { get; private set; } = 3;
        public int EscortLaneNodeGap { get; private set; } = 2;
        public int BotDifficulty { get; private set; } = 3;
        public float BotIdleVisionDistance { get; private set; } = 4096.0f;
        public bool ZrNapalmEnable { get; private set; } = true;
        public int ZrInfectSpawnType { get; private set; } = 1;
        public bool ZrInfectSpawnWarning { get; private set; } = true;
        public int ZrDefaultWinnerTeam { get; private set; } = 3;
        public bool DebugHudEnabled { get; private set; } = false;
        public bool StatusHudEnabled { get; private set; }
        public bool StatusHudLargeText { get; private set; } = true;
        public float StatusHudInterval { get; private set; } = 1.0f;
        public int StatusHudMaxBots { get; private set; } = 8;
        public bool DebugChatEnabled { get; private set; } = false;
        public bool DebugConsoleEnabled { get; private set; } = true;
        public int DebugHudDuration { get; private set; } = 4;

        public void Apply(string key, string value)
        {
            switch (key.Trim().ToLowerInvariant())
            {
                case "enabled": Enabled = ParseBool(value, Enabled); break;
                case "solo_bot_quota": SoloBotQuota = ParseInt(value, SoloBotQuota); break;
                case "duo_bot_quota": DuoBotQuota = ParseInt(value, DuoBotQuota); break;
                case "coop_min_bot_quota": CoopMinBotQuota = ParseInt(value, CoopMinBotQuota); break;
                case "coop_max_bot_quota": CoopMaxBotQuota = ParseInt(value, CoopMaxBotQuota); break;
                case "group_min_bot_quota": GroupMinBotQuota = ParseInt(value, GroupMinBotQuota); break;
                case "group_max_bot_quota": GroupMaxBotQuota = ParseInt(value, GroupMaxBotQuota); break;
                case "infection_delay": InfectionDelay = ParseFloat(value, InfectionDelay); break;
                case "ze_roundtime_minutes": ZeRoundTimeMinutes = ParseFloat(value, ZeRoundTimeMinutes); break;
                case "non_ze_roundtime_minutes": NonZeRoundTimeMinutes = ParseFloat(value, NonZeRoundTimeMinutes); break;
                case "buytime_seconds": BuyTimeSeconds = ParseInt(value, BuyTimeSeconds); break;
                case "buy_anywhere": BuyAnywhere = ParseBool(value, BuyAnywhere); break;
                case "bot_add_delay": BotAddDelay = ParseFloat(value, BotAddDelay); break;
                case "release_move_delay": ReleaseMoveDelay = ParseFloat(value, ReleaseMoveDelay); break;
                case "initial_recovery_delay": InitialRecoveryDelay = ParseFloat(value, InitialRecoveryDelay); break;
                case "respawn_recovery_delay": RespawnRecoveryDelay = ParseFloat(value, RespawnRecoveryDelay); break;
                case "record_interval": RecordInterval = ParseFloat(value, RecordInterval); break;
                case "record_distance": RecordDistance = ParseFloat(value, RecordDistance); break;
                case "max_path_points": MaxPathPoints = ParseInt(value, MaxPathPoints); break;
                case "watchdog_interval": WatchdogInterval = ParseFloat(value, WatchdogInterval); break;
                case "stuck_distance": StuckDistance = ParseFloat(value, StuckDistance); break;
                case "force_recovery_time": ForceRecoveryTime = ParseFloat(value, ForceRecoveryTime); break;
                case "no_damage_recovery_time": NoDamageRecoveryTime = ParseFloat(value, NoDamageRecoveryTime); break;
                case "recovery_min_behind": RecoveryMinBehind = ParseFloat(value, RecoveryMinBehind); break;
                case "recovery_max_behind": RecoveryMaxBehind = ParseFloat(value, RecoveryMaxBehind); break;
                case "recovery_path_max_age": RecoveryPathMaxAge = ParseFloat(value, RecoveryPathMaxAge); break;
                case "recovery_cooldown": RecoveryCooldown = ParseFloat(value, RecoveryCooldown); break;
                case "hit_recovery_cooldown": HitRecoveryCooldown = ParseFloat(value, HitRecoveryCooldown); break;
                case "recovery_retry_delay": RecoveryRetryDelay = ParseFloat(value, RecoveryRetryDelay); break;
                case "recovery_path_retry_delay": RecoveryPathRetryDelay = ParseFloat(value, RecoveryPathRetryDelay); break;
                case "recovery_path_retry_count": RecoveryPathRetryCount = ParseInt(value, RecoveryPathRetryCount); break;
                case "recovery_point_reservation_time": RecoveryPointReservationTime = ParseFloat(value, RecoveryPointReservationTime); break;
                case "recovery_lane_count": RecoveryLaneCount = ParseInt(value, RecoveryLaneCount); break;
                case "recovery_lane_node_gap": RecoveryLaneNodeGap = ParseInt(value, RecoveryLaneNodeGap); break;
                case "escort_update_interval": EscortUpdateInterval = ParseFloat(value, EscortUpdateInterval); break;
                case "escort_node_radius": EscortNodeRadius = ParseFloat(value, EscortNodeRadius); break;
                case "escort_release_distance": EscortReleaseDistance = ParseFloat(value, EscortReleaseDistance); break;
                case "escort_progress_distance": EscortProgressDistance = ParseFloat(value, EscortProgressDistance); break;
                case "escort_skip_time": EscortSkipTime = ParseFloat(value, EscortSkipTime); break;
                case "escort_teleport_time": EscortTeleportTime = ParseFloat(value, EscortTeleportTime); break;
                case "escort_give_up_time": EscortGiveUpTime = ParseFloat(value, EscortGiveUpTime); break;
                case "escort_forward_move": EscortForwardMove = ParseFloat(value, EscortForwardMove); break;
                case "escort_step_interval": EscortStepInterval = ParseFloat(value, EscortStepInterval); break;
                case "escort_lane_count": EscortLaneCount = ParseInt(value, EscortLaneCount); break;
                case "escort_lane_node_gap": EscortLaneNodeGap = ParseInt(value, EscortLaneNodeGap); break;
                case "bot_difficulty": BotDifficulty = ParseInt(value, BotDifficulty); break;
                case "bot_idle_vision_distance": BotIdleVisionDistance = ParseFloat(value, BotIdleVisionDistance); break;
                case "zr_napalm_enable": ZrNapalmEnable = ParseBool(value, ZrNapalmEnable); break;
                case "zr_infect_spawn_type": ZrInfectSpawnType = ParseInt(value, ZrInfectSpawnType); break;
                case "zr_infect_spawn_warning": ZrInfectSpawnWarning = ParseBool(value, ZrInfectSpawnWarning); break;
                case "zr_default_winner_team": ZrDefaultWinnerTeam = ParseInt(value, ZrDefaultWinnerTeam); break;
                case "debug_hud_enabled": DebugHudEnabled = ParseBool(value, DebugHudEnabled); break;
                case "status_hud_enabled": StatusHudEnabled = ParseBool(value, StatusHudEnabled); break;
                case "status_hud_large_text": StatusHudLargeText = ParseBool(value, StatusHudLargeText); break;
                case "status_hud_interval": StatusHudInterval = ParseFloat(value, StatusHudInterval); break;
                case "status_hud_max_bots": StatusHudMaxBots = ParseInt(value, StatusHudMaxBots); break;
                case "debug_chat_enabled": DebugChatEnabled = ParseBool(value, DebugChatEnabled); break;
                case "debug_console_enabled": DebugConsoleEnabled = ParseBool(value, DebugConsoleEnabled); break;
                case "debug_hud_duration": DebugHudDuration = ParseInt(value, DebugHudDuration); break;
            }
        }

        public string ToFileText()
        {
            return """
// Kzen-ZRPVE config
// 0/1 or true/false are both accepted.

enabled = 1

// Bot count profiles. Solo is 1 human, duo is 2 humans.
solo_bot_quota = 10
duo_bot_quota = 10
coop_min_bot_quota = 6
coop_max_bot_quota = 12
group_min_bot_quota = 12
group_max_bot_quota = 18

// Round / infection timing.
infection_delay = 16.0
ze_roundtime_minutes = 60.0
non_ze_roundtime_minutes = 2.5
buytime_seconds = 9999
buy_anywhere = 1
bot_add_delay = 0.2
release_move_delay = 1.0
initial_recovery_delay = 3.0
respawn_recovery_delay = 3.0

// Breadcrumb path recording. Only grounded, live CT players are recorded.
record_interval = 0.25
record_distance = 128
max_path_points = 2048

// Bot stuck watchdog and one-time safe recovery placement behind the recorded route.
watchdog_interval = 1.0
stuck_distance = 128
force_recovery_time = 8.0
no_damage_recovery_time = 10.0
recovery_min_behind = 800
recovery_max_behind = 1600
recovery_path_max_age = 60.0
recovery_cooldown = 4.0
hit_recovery_cooldown = 15.0
recovery_retry_delay = 2.0
recovery_path_retry_delay = 1.0
recovery_path_retry_count = 6
recovery_point_reservation_time = 5.0
recovery_lane_count = 8
recovery_lane_node_gap = 3

// A recovery point must remain at least this far from a live human.
escort_release_distance = 800

// CS2 bot AI. Pathing is still limited by CS2 nav.
bot_difficulty = 3
bot_idle_vision_distance = 4096

// CS2Fixes Zombie:Reborn cvars applied by this plugin.
zr_napalm_enable = 1
zr_infect_spawn_type = 1
zr_infect_spawn_warning = 1
zr_default_winner_team = 3

// Debug output. HUD/chat can cause netchan lag; keep them off unless testing.
debug_hud_enabled = 0
status_hud_enabled = 0
status_hud_large_text = 1
status_hud_interval = 1.0
status_hud_max_bots = 8
debug_chat_enabled = 0
debug_console_enabled = 1
debug_hud_duration = 4

""";
        }

        private static bool ParseBool(string value, bool fallback)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "1" or "true" or "yes" or "on" => true,
                "0" or "false" or "no" or "off" => false,
                _ => fallback
            };
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            return float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }
    }

    private sealed record PathPoint(long Sequence, Vector Position, float Timestamp);

    private sealed class PathPointSequenceComparer : IComparer<PathPoint>
    {
        public static PathPointSequenceComparer Instance { get; } = new();

        public int Compare(PathPoint? left, PathPoint? right)
        {
            return (left?.Sequence ?? 0).CompareTo(right?.Sequence ?? 0);
        }
    }

    private sealed class BotWatchState(Vector lastPosition, float stuckSeconds, float nextRecoveryTime, float lastHumanHitTime)
    {
        public Vector LastPosition { get; set; } = lastPosition;
        public float StuckSeconds { get; set; } = stuckSeconds;
        public float NextRecoveryTime { get; set; } = nextRecoveryTime;
        public float LastHumanHitTime { get; set; } = lastHumanHitTime;
    }

    private sealed class EscortState(
        long targetSequence,
        long movementId,
        Vector lastProgressPosition,
        float nextStepTime,
        string reason)
    {
        public long TargetSequence { get; set; } = targetSequence;
        public long MovementId { get; } = movementId;
        public Vector LastProgressPosition { get; set; } = lastProgressPosition;
        public float NextStepTime { get; set; } = nextStepTime;
        public string Reason { get; } = reason;
    }
}

