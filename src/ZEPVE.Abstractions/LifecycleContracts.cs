namespace ZEPVE.Abstractions;

public enum PlayerTeam { None, Spectator, Terrorist, CounterTerrorist }
public enum PlayerValidity { Connected, BoundPawn, LivePawn }
public enum ServerValidity { Map, Round }
public readonly record struct ServerLifetime(Guid PluginLifetime, long MapEpoch, long RoundEpoch);
public readonly record struct LifecycleState(Guid PluginLifetime, long MapEpoch, long RoundEpoch,
    bool Loaded, bool MapOpen, bool RoundOpen);

public interface IPlayerContext
{
    int Slot { get; }
    PlayerRole Role { get; }
    PlayerTeam Team { get; }
    bool IsBot { get; }
    bool IsHLTV { get; }
    bool Connected { get; }
    bool Alive { get; }
    long ConnectionGeneration { get; }
    long PawnGeneration { get; }
}

public enum PvePhase { Unbound, Disabled, Preparing, AwaitingRelease, Released, Closed }
public readonly record struct PveRoundStatus(PvePhase Phase, string Profile, int BotQuota, float RespawnDelay)
{
    public bool InfectionReleased => Phase == PvePhase.Released;
    public string RespawnExecutor => "ZombieReborn / existing compatible runtime";
}

/// <summary>Read-only identity/validity authority. Server-thread access only; validity is not gameplay permission.</summary>
public interface ICoreLifecycle
{
    LifecycleState State { get; }
    PveRoundStatus Round { get; }
    bool GameplayAuthorityActive { get; }
    IReadOnlyCollection<IPlayerContext> Players { get; }
    IReadOnlyCollection<IPlayerContext> Humans { get; }
    IReadOnlyCollection<IPlayerContext> ZombieBots { get; }
    bool TryCapture(int slot, out PlayerLifetime token, PlayerValidity validity = PlayerValidity.LivePawn);
    bool TryResolve(PlayerLifetime token, out IPlayerContext? player, out string rejection,
        PlayerValidity validity = PlayerValidity.LivePawn);
    ServerLifetime CaptureServer();
    bool ValidateServer(ServerLifetime token, ServerValidity validity, out string rejection);
    ICoreWorkScope CreateWorkScope();
    IReadOnlyList<LifecycleEntry> ReadFlightRecorder(int maximum = 64);
}

/// <summary>Dispose when the consuming module unloads. Jobs also cancel on relevant Core invalidation.</summary>
public interface ICoreWorkScope : IDisposable
{
    int PendingCount { get; }
    bool TrySchedulePlayer(PlayerLifetime token, float seconds, Action<IPlayerContext> action,
        out IDisposable? job, PlayerValidity validity = PlayerValidity.LivePawn, Action<string>? canceled = null);
    bool TryScheduleServer(ServerLifetime token, float seconds, Action action,
        out IDisposable? job, ServerValidity validity = ServerValidity.Round, Action<string>? canceled = null);
}

public enum LifecycleEventKind
{
    PluginLifetime, MapEpoch, RoundEpoch, Connect, Disconnect, RoleChange, PawnGeneration,
    StaleTokenRejected, WorkCanceled, Authority, Policy
}
public readonly record struct LifecycleEntry(long Sequence, LifecycleEventKind Kind, LifecycleState State,
    int? Slot, long ConnectionGeneration, long PawnGeneration, string Detail);
