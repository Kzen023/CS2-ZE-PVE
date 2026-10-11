using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using ZEPVE.Abstractions;

namespace KzenZrPve;

public sealed class ZrPvePlugin : BasePlugin
{
    public override string ModuleName => "Kzen ZR PvE";
    public override string ModuleVersion => "1.1.0-navigation-adapter";
    public override string ModuleAuthor => "Maxsun + Codex";
    public override string ModuleDescription => "PvE adapter for CS2Fixes Zombie:Reborn on ZE maps.";

    private const string Prefix = "[ZR-PvE]";
    private bool _enabled = true;
    private ZrPveConfig _config = new();
    private LegacyBridge? _bridge;
    private bool _infectionReleased => SuiteRuntime.Current is { State.Loaded: true, GameplayAuthorityActive: true } core && core.Round.InfectionReleased;
    private readonly List<CounterStrikeSharp.API.Modules.Timers.Timer> _mapTimers = new();
    private readonly Dictionary<int, string> _botStatuses = new();

    public override void Load(bool hotReload)
    {
        _config = LoadConfig();
        _enabled = _config.Enabled;
        _bridge = new(this);
        try { SuiteRuntime.AttachLegacy(_bridge); }
        catch { SuiteRuntime.DetachLegacy(_bridge); _bridge = null; StopMapTimers(); throw; }
    }

    private void OnMapStart(string mapName)
    {
        StopMapTimers();
        _botStatuses.Clear();
        StartMapServices();
    }

    private void StartMapServices()
    {
        StartMapTimer(Math.Max(0.5f, _config.StatusHudInterval), ShowBotStatusHud, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void ResetRound()
    {
        _botStatuses.Clear();
    }

    private void ShowInfectionCountdown(int seconds)
    {
        Server.ExecuteCommand($"css_kzen_countdown {seconds} 首轮感染");
        Server.PrintToConsole($"{Prefix} Infection countdown synchronized: {seconds}s.");
    }

    private void ObserveSpawn(int slot)
    {
        // Respawn execution remains external. No movement or recovery scheduler in this adapter.
        var player = Utilities.GetPlayerFromSlot(slot);
        if (_infectionReleased && IsZombieBot(player)) SetBotStatus(player!, "正在搜索玩家");
    }

    public override void Unload(bool hotReload)
    {
        if (_bridge is not null) SuiteRuntime.DetachLegacy(_bridge);
        _bridge = null;
        StopMapTimers();
    }

    private void StartMapTimer(float seconds, Action action, TimerFlags flags)
    {
        var core = SuiteRuntime.Current;
        if (core is not { State.Loaded: true }) return;
        var token = core.CaptureServer();
        _mapTimers.Add(new(seconds, () =>
        {
            if (core.ValidateServer(token, ServerValidity.Map, out _)) action();
        }, flags));
    }

    private void StopMapTimers()
    {
        foreach (var timer in _mapTimers) timer.Kill();
        _mapTimers.Clear();
    }

    private sealed class LegacyBridge(ZrPvePlugin plugin) : ILegacyPveBridge
    {
        public bool MovementWriterDisabled => true;
        public LegacyPveSettings Settings => new(
            plugin._config.Enabled, plugin._config.SoloBotQuota, plugin._config.DuoBotQuota,
            plugin._config.CoopMinBotQuota, plugin._config.CoopMaxBotQuota, plugin._config.GroupMinBotQuota,
            plugin._config.GroupMaxBotQuota, plugin._config.InfectionDelay, plugin._config.ZeRoundTimeMinutes,
            plugin._config.NonZeRoundTimeMinutes, plugin._config.BuyTimeSeconds, plugin._config.BuyAnywhere,
            plugin._config.BotAddDelay, plugin._config.ReleaseMoveDelay, plugin._config.BotDifficulty,
            plugin._config.BotIdleVisionDistance, plugin._config.ZrNapalmEnable, plugin._config.ZrInfectSpawnType,
            plugin._config.ZrInfectSpawnWarning, plugin._config.ZrDefaultWinnerTeam);
        public void ShowInfectionCountdown(int seconds) => plugin.ShowInfectionCountdown(seconds);
        public void ObserveSpawn(int slot) => plugin.ObserveSpawn(slot);
        public void ObserveMapStart(string mapName, bool resetData)
        {
            if (resetData) plugin.OnMapStart(mapName);
            else { plugin.StopMapTimers(); plugin.StartMapServices(); }
        }
        public void ObserveRoundStart() => plugin.ResetRound();
        public void SuspendMapServices() => plugin.StopMapTimers();
    }

    private static bool IsRealPlayableMap()
    {
        var mapName = Server.MapName ?? string.Empty;
        return !string.IsNullOrWhiteSpace(mapName) && !mapName.Equals("<empty>", StringComparison.OrdinalIgnoreCase);
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

