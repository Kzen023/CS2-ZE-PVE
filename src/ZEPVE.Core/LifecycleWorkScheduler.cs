using ZEPVE.Abstractions;

namespace ZEPVE.Core;

internal interface ITimerDispatcher { IDisposable Schedule(float seconds, Action action); }

/// <summary>Bounded server-thread scheduler. Cancellation is eager; execution always re-reads entities.</summary>
internal sealed class LifecycleWorkScheduler : IDisposable
{
    private const int MaximumJobs = 256;
    private readonly PlayerRegistry _registry;
    private readonly ITimerDispatcher _timers;
    private readonly Action<Exception> _error;
    private readonly Dictionary<long, Job> _pending = new();
    private long _sequence;
    private bool _disposed;
    private bool _sweeping;
    public int PendingCount => _pending.Count;
    public LifecycleWorkScheduler(PlayerRegistry registry, ITimerDispatcher timers, Action<Exception> error)
    {
        _registry = registry;
        _timers = timers;
        _error = error;
        registry.Changed += Sweep;
    }
    public ICoreWorkScope CreateScope()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LifecycleWorkScheduler));
        return new Scope(this);
    }
    private bool Schedule(Scope owner, float seconds, PlayerLifetime? player, ServerLifetime server,
        PlayerValidity playerValidity, ServerValidity serverValidity, Action<IPlayerContext>? playerAction,
        Action? serverAction, Action<string>? canceled, out IDisposable? lease)
    {
        lease = null;
        if (_disposed || owner.Disposed || _pending.Count >= MaximumJobs || !float.IsFinite(seconds) || seconds < 0) return false;
        var job = new Job(++_sequence, owner, player, server, playerValidity, serverValidity, playerAction, serverAction, canceled);
        if (!Validate(job, true, out _, out _)) return false;
        _pending.Add(job.Id, job);
        try { job.Timer = _timers.Schedule(seconds, () => Run(job)); }
        catch { _pending.Remove(job.Id); throw; }
        lease = job;
        return true;
    }
    private bool Validate(Job job, bool refresh, out ZepvePlayerContext? player, out string rejection)
    {
        player = null;
        if (job.Player is { } token) return _registry.Resolve(token, out player, out rejection, job.PlayerValidity, refresh);
        return _registry.ValidateServer(job.Server, job.ServerValidity, out rejection);
    }
    private void Run(Job job)
    {
        if (job.Finished || _disposed || !_pending.ContainsKey(job.Id)) return;
        if (!Validate(job, true, out var player, out var rejection)) { Cancel(job, rejection); return; }
        // Refresh can trigger cancellation synchronously. Never resurrect the canceled job.
        if (job.Finished) return;
        _pending.Remove(job.Id);
        job.Finished = true;
        try { if (job.PlayerAction is { } action) action(player!); else job.ServerAction?.Invoke(); }
        catch (Exception error) { _error(error); }
        finally { job.Clear(); }
    }
    private void Sweep()
    {
        if (_disposed || _sweeping) return;
        _sweeping = true;
        try
        {
            foreach (var job in _pending.Values.ToArray())
                if (!Validate(job, false, out _, out var rejection)) Cancel(job, rejection);
        }
        finally { _sweeping = false; }
    }
    private void Cancel(Job job, string reason)
    {
        if (job.Finished) return;
        job.Finished = true;
        _pending.Remove(job.Id);
        job.Timer?.Dispose();
        _registry.Record(LifecycleEventKind.WorkCanceled, reason);
        try { job.Canceled?.Invoke(reason); }
        catch (Exception error) { _error(error); }
        finally { job.Clear(); }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _registry.Changed -= Sweep;
        foreach (var job in _pending.Values.ToArray()) Cancel(job, "scheduler unload");
    }
    private sealed class Scope(LifecycleWorkScheduler scheduler) : ICoreWorkScope
    {
        internal bool Disposed;
        internal void Cancel(Job job) => scheduler.Cancel(job, "explicit cancellation");
        public int PendingCount => scheduler._pending.Values.Count(job => ReferenceEquals(job.Owner, this));
        public bool TrySchedulePlayer(PlayerLifetime token, float seconds, Action<IPlayerContext> action,
            out IDisposable? job, PlayerValidity validity = PlayerValidity.LivePawn, Action<string>? canceled = null)
            => scheduler.Schedule(this, seconds, token, default, validity, default, action, null, canceled, out job);
        public bool TryScheduleServer(ServerLifetime token, float seconds, Action action,
            out IDisposable? job, ServerValidity validity = ServerValidity.Round, Action<string>? canceled = null)
            => scheduler.Schedule(this, seconds, null, token, default, validity, null, action, canceled, out job);
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            foreach (var job in scheduler._pending.Values.Where(job => ReferenceEquals(job.Owner, this)).ToArray())
                scheduler.Cancel(job, "consumer unload");
        }
    }
    private sealed class Job(long id, Scope owner, PlayerLifetime? player, ServerLifetime server,
        PlayerValidity playerValidity, ServerValidity serverValidity, Action<IPlayerContext>? playerAction,
        Action? serverAction, Action<string>? canceled) : IDisposable
    {
        public long Id = id;
        public Scope Owner = owner;
        public PlayerLifetime? Player = player;
        public ServerLifetime Server = server;
        public PlayerValidity PlayerValidity = playerValidity;
        public ServerValidity ServerValidity = serverValidity;
        public Action<IPlayerContext>? PlayerAction = playerAction;
        public Action? ServerAction = serverAction;
        public Action<string>? Canceled = canceled;
        public IDisposable? Timer;
        public bool Finished;
        public void Clear() { PlayerAction = null; ServerAction = null; Canceled = null; Timer = null; }
        public void Dispose() => Owner.Cancel(this);
    }
}
