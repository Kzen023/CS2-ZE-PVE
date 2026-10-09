using ZEPVE.Abstractions;

namespace ZEPVE.Core;

internal sealed class CoreRuntime(PlayerRegistry registry, LifecycleWorkScheduler scheduler, PveRoundController policy) : ICoreLifecycle
{
    public LifecycleState State => registry.State;
    public PveRoundStatus Round => policy.Status;
    public bool GameplayAuthorityActive => policy.AuthorityActive;
    public IReadOnlyCollection<IPlayerContext> Players => registry.Players;
    public IReadOnlyCollection<IPlayerContext> Humans => registry.Humans;
    public IReadOnlyCollection<IPlayerContext> ZombieBots => registry.ZombieBots;
    public bool TryCapture(int slot, out PlayerLifetime token, PlayerValidity validity = PlayerValidity.LivePawn)
        => registry.TryCapture(slot, out token, validity);
    public bool TryResolve(PlayerLifetime token, out IPlayerContext? player, out string rejection, PlayerValidity validity = PlayerValidity.LivePawn)
    {
        var result = registry.TryResolve(token, out var resolved, out rejection, validity);
        player = resolved;
        return result;
    }
    public ServerLifetime CaptureServer() => registry.CaptureServer();
    public bool ValidateServer(ServerLifetime token, ServerValidity validity, out string rejection) => registry.ValidateServer(token, validity, out rejection);
    public ICoreWorkScope CreateWorkScope() => scheduler.CreateScope();
    public IReadOnlyList<LifecycleEntry> ReadFlightRecorder(int maximum = 64) => registry.Recorder.Read(Math.Min(maximum, 256));
}
