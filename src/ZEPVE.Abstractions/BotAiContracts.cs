namespace ZEPVE.Abstractions;

/// <summary>Both identities are issued by Core. Module lifetime/version also invalidate target work.</summary>
public readonly record struct BotTargetBinding(Guid ModuleLifetime, PlayerLifetime Bot,
    PlayerLifetime Target, long BindingVersion);
public enum ReacquireReason { TargetBound, PerceptionLost, RecoveryTeleport, NavigationRequest, Diagnostic }
public enum ReacquirePhase { Idle, Observing, Succeeded, TimedOut }
public readonly record struct CombatObservation(bool Available, uint? EnemyHandle, bool AssignedEnemy,
    bool EnemyVisible, bool AimingAtEnemy, bool Attacking, bool Sleeping, bool AllowActive, string Detail);
public readonly record struct BotAiStatus(BotTargetBinding Binding, string SelectionReason,
    ReacquirePhase Reacquire, ReacquireReason ReacquireReason, double ReacquireUntil,
    CombatObservation Observation, string LastAssist, string LastCombatEvent);
public readonly record struct BotAiEvent(long Sequence, double Time, int? BotSlot, long BindingVersion,
    string Kind, string Detail);

/// <summary>Server-thread only. A target is pursuit policy, not a movement command or Valve Enemy.</summary>
public interface IBotAi
{
    Guid ModuleLifetime { get; }
    bool TryGetAssignedTarget(PlayerLifetime bot, out BotTargetBinding binding);
    bool ValidateBinding(BotTargetBinding binding, out string rejection);
    bool RequestReacquire(BotTargetBinding binding, ReacquireReason reason, out string result);
    bool TryGetStatus(PlayerLifetime bot, out BotAiStatus status);
}

/// <summary>One BotAI provider; owns no player identity. Consumers must validate every stored binding.</summary>
public static class BotAiRuntime
{
    public static IBotAi? Current { get; private set; }
    internal static void Publish(IBotAi provider)
    {
        if (Current is not null) throw new InvalidOperationException("A BotAI provider is already active.");
        Current = provider;
    }
    internal static void Withdraw(IBotAi provider)
    {
        if (ReferenceEquals(Current, provider)) Current = null;
    }
}
