using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using ZEPVE.Abstractions;

namespace ZEPVE.Core;

internal sealed class NativeTimerDispatcher : ITimerDispatcher
{
    public IDisposable Schedule(float seconds, Action action) => new TimerLease(seconds, action);
    private sealed class TimerLease : IDisposable
    {
        private CounterStrikeSharp.API.Modules.Timers.Timer? _timer;
        public TimerLease(float seconds, Action action) => _timer = new(seconds, () => { _timer = null; action(); }, TimerFlags.STOP_ON_MAPCHANGE);
        public void Dispose() { _timer?.Kill(); _timer = null; }
    }
}

internal sealed class NativePveGameAdapter(PlayerRegistry registry) : IPveGameAdapter
{
    public void Execute(string command) => Server.ExecuteCommand(command);
    public void MoveBots(PlayerTeam team, ServerLifetime lifetime)
    {
        // Rare team transitions only; snapshot prevents reentrant team-event mutation during enumeration.
        foreach (var entry in registry.Players.ToArray())
        {
            if (!registry.ValidateServer(lifetime, ServerValidity.Round, out _)) return;
            if (!registry.TryCapture(entry.Slot, out var token, PlayerValidity.Connected)
                || !registry.TryResolve(token, out var current, out _, PlayerValidity.Connected)
                || !current!.IsBot || current.IsHLTV || current.Team == team) continue;
            var player = ResolveController(current);
            if (player is null) continue;
            player.ChangeTeam((CsTeam)team);
            // Team change can replace the pawn; re-authorize the same connection's current controller.
            if (!registry.ValidateServer(lifetime, ServerValidity.Round, out _)
                || !registry.TryCapture(entry.Slot, out var next, PlayerValidity.Connected)
                || next.ConnectionGeneration != token.ConnectionGeneration
                || !registry.TryResolve(next, out var after, out _, PlayerValidity.Connected)) continue;
            ResolveController(after!)?.SwitchTeam((CsTeam)team);
        }
    }
    private static CCSPlayerController? ResolveController(ZepvePlayerContext context)
    {
        var current = Utilities.GetPlayerFromSlot(context.Slot);
        return current is { IsValid: true } && current.EntityHandle.Raw == context.ControllerHandle
            && current.UserId == context.UserId && current.Connected == PlayerConnectedState.Connected ? current : null;
    }
}
