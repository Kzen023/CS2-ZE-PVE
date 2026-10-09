using System.Collections;
using ZEPVE.Abstractions;

namespace ZEPVE.Core;

/// <summary>Single identity source. All operations run on the server thread.</summary>
public sealed class PlayerRegistry
{
    private readonly IPlayerObservationSource _source;
    private readonly Dictionary<int, ZepvePlayerContext> _players = new();
    private readonly HashSet<int> _disconnecting = new();
    private readonly HashSet<int> _seen = new();
    private long _connectionSequence;
    private long _pawnSequence;
    private bool _loaded = true;
    private bool _mapOpen;
    private bool _roundOpen;
    internal readonly LifecycleFlightRecorder Recorder = new();
    internal event Action? Changed;
    public LifecycleState State => new(PluginLifetime, MapEpoch, RoundEpoch, _loaded, _mapOpen, _roundOpen);

    public Guid PluginLifetime { get; } = Guid.NewGuid();
    public long MapEpoch { get; private set; }
    public long RoundEpoch { get; private set; }
    public IReadOnlyCollection<ZepvePlayerContext> Players { get; }
    public IReadOnlyCollection<ZepvePlayerContext> Humans { get; }
    public IReadOnlyCollection<ZepvePlayerContext> ZombieBots { get; }

    internal PlayerRegistry(IPlayerObservationSource source)
    {
        _source = source;
        Players = new RegistryView(this, null);
        Humans = new RegistryView(this, PlayerRole.Human);
        ZombieBots = new RegistryView(this, PlayerRole.ZombieBot);
        Record(LifecycleEventKind.PluginLifetime, "load");
    }

    internal void BeginMap()
    {
        if (!_loaded) return;
        MapEpoch++;
        RoundEpoch++;
        _mapOpen = _roundOpen = true;
        _players.Clear();
        _disconnecting.Clear();
        _seen.Clear();
        Record(LifecycleEventKind.MapEpoch, "map start/bootstrap");
        Record(LifecycleEventKind.RoundEpoch, "map initialization");
        Changed?.Invoke();
    }

    internal void EndMap()
    {
        MapEpoch++;
        InvalidateRound();
        _mapOpen = false;
        _players.Clear();
        _disconnecting.Clear();
        _seen.Clear();
        Record(LifecycleEventKind.MapEpoch, "map end");
        Changed?.Invoke();
    }

    internal void BeginRound()
    {
        if (!_loaded || !_mapOpen) return;
        RoundEpoch++;
        _roundOpen = true;
        Record(LifecycleEventKind.RoundEpoch, "round start");
        Changed?.Invoke();
    }

    internal void InvalidateRound()
    {
        RoundEpoch++;
        _roundOpen = false;
        Record(LifecycleEventKind.RoundEpoch, "round invalidation");
        Changed?.Invoke();
    }

    internal void Unload()
    {
        _loaded = false;
        Record(LifecycleEventKind.PluginLifetime, "unload");
        EndMap();
    }

    internal void Connect(int slot)
    {
        if (!ValidSlot(slot) || !_loaded || !_mapOpen) return;
        Disconnect(slot);
        _disconnecting.Remove(slot);
        Observe(slot);
    }

    internal void Disconnect(int slot)
    {
        if (!ValidSlot(slot)) return;
        _disconnecting.Add(slot); // Native controller can remain valid until disconnect completes.
        MarkAbsent(slot);
    }

    private void MarkAbsent(int slot)
    {
        if (!_players.TryGetValue(slot, out var old) || !old.Connected) return;
        var absent = new ZepvePlayerContext(slot, PlayerRole.Unknown, false, false,
            ++_connectionSequence, ++_pawnSequence, old.ControllerHandle, old.UserId, null,
            old.Team, old.IsBot, old.IsHLTV);
        _players[slot] = absent;
        Record(LifecycleEventKind.Disconnect, "absent/disconnect", absent);
        Record(LifecycleEventKind.PawnGeneration, "disconnect", absent);
        Changed?.Invoke();
    }

    internal ZepvePlayerContext? Observe(int slot)
    {
        if (!_loaded || !_mapOpen || !ValidSlot(slot) || _disconnecting.Contains(slot)) return null;
        var observed = _source.Read(slot);
        if (observed is not { } current || current.Slot != slot)
        {
            MarkAbsent(slot);
            return null;
        }
        _players.TryGetValue(slot, out var old);
        var newConnection = old is null || !old.Connected || old.ControllerHandle != current.ControllerHandle
            || old.UserId != current.UserId;
        var connection = newConnection ? ++_connectionSequence : old!.ConnectionGeneration;
        // A life-state transition invalidates work even when the engine reuses the same pawn handle.
        var newPawn = newConnection || old!.PawnHandle != current.PawnHandle || old.Alive != current.Alive;
        var pawn = newPawn ? ++_pawnSequence : old!.PawnGeneration;
        if (!newConnection && !newPawn && old!.Role == current.Role && old.Team == current.Team
            && old.IsBot == current.IsBot && old.IsHLTV == current.IsHLTV) return old;
        var context = new ZepvePlayerContext(slot, current.Role, true, current.Alive,
            connection, pawn, current.ControllerHandle, current.UserId, current.PawnHandle,
            current.Team, current.IsBot, current.IsHLTV);
        _players[slot] = context;
        if (newConnection) Record(LifecycleEventKind.Connect, "connection observed", context);
        if (old is not null && old.Role != context.Role) Record(LifecycleEventKind.RoleChange, $"{old.Role} -> {context.Role}", context);
        if (newPawn) Record(LifecycleEventKind.PawnGeneration, "pawn handle/life transition", context);
        Changed?.Invoke();
        return context;
    }

    internal bool ObserveLifeEvent(int slot, uint controllerHandle, int? userId, bool death)
    {
        var current = Observe(slot);
        // Reject an event referring to a controller from a previous occupant of the slot.
        if (current is null || current.ControllerHandle != controllerHandle || current.UserId != userId) return false;
        _players[slot] = new(slot, current.Role, true, death ? false : current.Alive,
            current.ConnectionGeneration, ++_pawnSequence, current.ControllerHandle, current.UserId, current.PawnHandle,
            current.Team, current.IsBot, current.IsHLTV);
        Record(LifecycleEventKind.PawnGeneration, death ? "death event" : "spawn event", _players[slot]);
        Changed?.Invoke();
        return true;
    }

    internal void Reconcile()
    {
        if (!_loaded || !_mapOpen) return;
        _seen.Clear();
        foreach (var slot in _source.GetSlots())
        {
            if (!ValidSlot(slot)) continue;
            _seen.Add(slot);
            Observe(slot);
        }
        // Snapshot keys because MarkAbsent replaces immutable contexts in the dictionary.
        foreach (var slot in _players.Keys.ToArray())
            if (!_seen.Contains(slot)) MarkAbsent(slot);
    }

    public bool TryCapture(int slot, out PlayerLifetime lifetime, PlayerValidity validity = PlayerValidity.LivePawn)
    {
        lifetime = default;
        if (!_loaded || !_mapOpen || !_roundOpen) return false;
        var current = Observe(slot);
        if (current is null || !Meets(current, validity)) return false;
        lifetime = new(PluginLifetime, MapEpoch, RoundEpoch, slot,
            current.ConnectionGeneration, current.PawnGeneration);
        return true;
    }

    public bool TryResolve(PlayerLifetime lifetime, out ZepvePlayerContext? context, out string rejection,
        PlayerValidity validity = PlayerValidity.LivePawn) => Resolve(lifetime, out context, out rejection, validity, true);

    internal bool Resolve(PlayerLifetime lifetime, out ZepvePlayerContext? context, out string rejection,
        PlayerValidity validity, bool refresh)
    {
        context = null;
        rejection = "";
        if (!_loaded || lifetime.PluginLifetime != PluginLifetime) rejection = "plugin lifetime";
        else if (!_mapOpen || lifetime.MapEpoch != MapEpoch) rejection = "map epoch";
        else if (!_roundOpen || lifetime.RoundEpoch != RoundEpoch) rejection = "round epoch";
        else
        {
            // Always read the CURRENT native controller/pawn through the adapter at execution time.
            var current = refresh ? Observe(lifetime.Slot) : _players.GetValueOrDefault(lifetime.Slot);
            if (current is null || !current.Connected) rejection = "disconnected/invalid entity";
            else if (current.ConnectionGeneration != lifetime.ConnectionGeneration) rejection = "connection generation";
            else if (current.PawnGeneration != lifetime.PawnGeneration) rejection = "pawn generation";
            else if (_disconnecting.Contains(lifetime.Slot) || !Meets(current, validity)) rejection = "dead/invalid pawn";
            else context = current;
        }
        if (context is null) Record(LifecycleEventKind.StaleTokenRejected, rejection, _players.GetValueOrDefault(lifetime.Slot));
        return context is not null;
    }

    public ServerLifetime CaptureServer() => new(PluginLifetime, MapEpoch, RoundEpoch);
    public bool ValidateServer(ServerLifetime token, ServerValidity validity, out string rejection)
    {
        rejection = validity is not ServerValidity.Map and not ServerValidity.Round ? "invalid scope"
            : !_loaded || token.PluginLifetime != PluginLifetime ? "plugin lifetime"
            : !_mapOpen || token.MapEpoch != MapEpoch ? "map epoch"
            : validity == ServerValidity.Round && (!_roundOpen || token.RoundEpoch != RoundEpoch) ? "round epoch" : "";
        if (rejection.Length != 0) Record(LifecycleEventKind.StaleTokenRejected, rejection);
        return rejection.Length == 0;
    }
    private static bool Meets(ZepvePlayerContext player, PlayerValidity validity) => player.Connected && validity switch
    {
        PlayerValidity.Connected => true,
        PlayerValidity.BoundPawn => player.PawnHandle is not null,
        PlayerValidity.LivePawn => player.PawnHandle is not null && player.Alive,
        _ => false
    };
    internal void Record(LifecycleEventKind kind, string detail, ZepvePlayerContext? player = null) => Recorder.Add(kind, State, detail, player);

    private static bool ValidSlot(int slot) => slot is >= 0 and < 64;

    private sealed class RegistryView(PlayerRegistry owner, PlayerRole? role) : IReadOnlyCollection<ZepvePlayerContext>
    {
        private bool Includes(ZepvePlayerContext player) => role is null || (player.Connected && player.Role == role);
        public int Count
        {
            get
            {
                var count = 0;
                foreach (var player in owner._players.Values) if (Includes(player)) count++;
                return count;
            }
        }
        public IEnumerator<ZepvePlayerContext> GetEnumerator()
        {
            foreach (var player in owner._players.Values) if (Includes(player)) yield return player;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
