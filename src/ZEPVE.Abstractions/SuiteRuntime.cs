namespace ZEPVE.Abstractions;

/// <summary>One shared provider reference, not another registry. Install this assembly under CSS shared/.</summary>
public static class SuiteRuntime
{
    public static ICoreLifecycle? Current { get; private set; }
    internal static ILegacyPveBridge? Legacy { get; private set; }
    internal static event Action? LegacyChanged;
    internal static PveResumePlan? ResumePlan { get; set; }

    internal static void Publish(ICoreLifecycle provider)
    {
        if (!provider.State.Loaded) throw new InvalidOperationException("An unloaded provider cannot become authoritative.");
        if (Current is { State.Loaded: true }) throw new InvalidOperationException("A Core lifecycle provider is already active.");
        Current = provider;
    }
    internal static void Withdraw(ICoreLifecycle provider)
    {
        if (ReferenceEquals(Current, provider)) Current = null;
    }
    internal static void AttachLegacy(ILegacyPveBridge bridge)
    {
        if (Legacy is not null) throw new InvalidOperationException("An adapted legacy bridge is already active.");
        Legacy = bridge;
        LegacyChanged?.Invoke();
    }
    internal static void DetachLegacy(ILegacyPveBridge bridge)
    {
        if (!ReferenceEquals(Legacy, bridge)) return;
        Legacy = null;
        LegacyChanged?.Invoke();
    }
}

// Temporary migration boundary. Only Core and the adapted legacy assembly can access it.
internal interface ILegacyPveBridge
{
    LegacyPveSettings Settings { get; }
    void ShowInfectionCountdown(int seconds);
    void ObserveSpawn(int slot);
    void ObserveMapStart(string mapName, bool resetData);
    void ObserveRoundStart();
    void SuspendMapServices();
}

internal sealed record LegacyPveSettings(
    bool Enabled, int SoloBotQuota, int DuoBotQuota, int CoopMinBotQuota, int CoopMaxBotQuota,
    int GroupMinBotQuota, int GroupMaxBotQuota, float InfectionDelay, float ZeRoundTimeMinutes,
    float NonZeRoundTimeMinutes, int BuyTimeSeconds, bool BuyAnywhere, float BotAddDelay,
    float ReleaseMoveDelay, int BotDifficulty, float BotIdleVisionDistance, bool ZrNapalmEnable,
    int ZrInfectSpawnType, bool ZrInfectSpawnWarning, int ZrDefaultWinnerTeam);
internal sealed record PveProfile(string Name, int MinPlayersToInfect, int MotherZombieMinCount,
    int MotherZombieRatio, float RespawnDelay, float KnockbackScale, float NapalmBurnDuration,
    int BotQuota, float InfectionDelay);
internal sealed record PveResumePlan(string MapName, LegacyPveSettings Settings, PveProfile? Profile,
    PveRoundStatus Status, double? ApplyDue, double? ReleaseDue, double? QuotaDue, double? MoveDue,
    double? MapSettingsDue = null);
