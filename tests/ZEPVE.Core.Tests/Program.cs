using ZEPVE.Abstractions;
using ZEPVE.Core;

var scenarios = new (string Name, Action<Fixture> Run)[]
{
    ("current callback re-resolves source", f =>
    {
        var token = f.Capture();
        var reads = f.Source.Reads;
        Check(f.Registry.TryResolve(token, out var current, out _) && current!.Slot == 2, "current identity rejected");
        Check(f.Source.Reads > reads, "callback trusted cached context");
    }),
    ("disconnect rejects despite still-valid native controller", f =>
    {
        var token = f.Capture();
        f.Registry.Disconnect(2);
        f.Reject(token);
        f.Registry.Reconcile();
        Check(!f.Registry.Players.Single().Connected && f.Registry.Humans.Count == 0, "disconnect resurrected from native state");
    }),
    ("slot reuse with identical native handles gets new connection", f =>
    {
        var token = f.Capture();
        f.Registry.Disconnect(2);
        f.Registry.Connect(2);
        f.Reject(token);
        Check(f.Capture().ConnectionGeneration > token.ConnectionGeneration, "connection reused");
    }),
    ("connect event replaces lifetime without prior disconnect", f =>
    {
        var token = f.Capture();
        f.Registry.Connect(2);
        f.Reject(token);
    }),
    ("missed disconnect detected by reconciliation", f =>
    {
        var token = f.Capture();
        f.Source.Players.Clear();
        f.Registry.Reconcile();
        f.Reject(token);
        Check(!f.Registry.Players.Single().Connected, "missing player stayed connected");
    }),
    ("missed replacement detected at callback execution", f =>
    {
        var token = f.Capture();
        f.Source.Players[2] = f.Source.Players[2] with { ControllerHandle = 102, UserId = 9 };
        f.Reject(token); // No event or reconciliation before this call.
    }),
    ("userid changes even if controller handle is reused", f =>
    {
        var token = f.Capture();
        f.Source.Players[2] = f.Source.Players[2] with { UserId = 9 };
        f.Reject(token);
    }),
    ("handle serial change rejects same entity index", f =>
    {
        var token = f.Capture();
        f.Source.Players[2] = f.Source.Players[2] with { ControllerHandle = 101 + (1u << 15) };
        f.Reject(token);
    }),
    ("invalid controller at execution rejects", f =>
    {
        var token = f.Capture();
        f.Source.Players.Remove(2);
        f.Reject(token);
    }),
    ("pawn replacement without an event rejects", f =>
    {
        var token = f.Capture();
        f.Source.Players[2] = f.Source.Players[2] with { PawnHandle = 202 };
        f.Reject(token);
        Check(f.Capture().ConnectionGeneration == token.ConnectionGeneration, "pawn replacement replaced connection");
    }),
    ("invalid/unbound pawn rejects", f =>
    {
        var token = f.Capture();
        f.Source.Players[2] = f.Source.Players[2] with { PawnHandle = null, Alive = false };
        f.Reject(token);
        Check(!f.Registry.TryCapture(2, out _), "captured invalid pawn");
    }),
    ("death event immediately invalidates", f =>
    {
        var token = f.Capture();
        f.Registry.ObserveLifeEvent(2, 101, 7, true);
        Check(!f.Registry.Players.Single().Alive, "death not observed");
        f.Reject(token);
    }),
    ("missed death detected from live state", f =>
    {
        var token = f.Capture();
        f.Source.Players[2] = f.Source.Players[2] with { Alive = false };
        f.Reject(token);
    }),
    ("respawn with same pawn handle rejects previous life", f =>
    {
        var token = f.Capture();
        f.Registry.ObserveLifeEvent(2, 101, 7, false);
        f.Reject(token);
        Check(f.Capture().PawnGeneration > token.PawnGeneration, "spawn reused lifetime");
    }),
    ("death then respawn remains a new pawn generation", f =>
    {
        var token = f.Capture();
        f.Source.Players[2] = f.Source.Players[2] with { Alive = false };
        f.Registry.ObserveLifeEvent(2, 101, 7, true);
        f.Source.Players[2] = f.Source.Players[2] with { Alive = true };
        f.Registry.ObserveLifeEvent(2, 101, 7, false);
        f.Reject(token);
        Check(f.Registry.TryResolve(f.Capture(), out _, out _), "new life rejected");
    }),
    ("old-controller life event cannot invalidate new occupant", f =>
    {
        f.Source.Players[2] = f.Source.Players[2] with { ControllerHandle = 102, UserId = 8 };
        var token = f.Capture();
        f.Registry.ObserveLifeEvent(2, 101, 7, true);
        Check(f.Registry.TryResolve(token, out _, out _), "stale event affected replacement");
    }),
    ("round end closes capture and rejects immediately", f =>
    {
        var token = f.Capture();
        f.Registry.InvalidateRound();
        f.Reject(token);
        Check(!f.Registry.TryCapture(2, out _), "captured after round end");
    }),
    ("restart and new round reject old callbacks", f =>
    {
        var token = f.Capture();
        f.Registry.InvalidateRound();
        f.Registry.BeginRound();
        f.Reject(token);
        Check(f.Capture().RoundEpoch > token.RoundEpoch, "round epoch reused");
    }),
    ("new round rejects even without round end", f =>
    {
        var token = f.Capture();
        f.Registry.BeginRound();
        f.Reject(token);
    }),
    ("map end clears registry and closes capture", f =>
    {
        var token = f.Capture();
        f.Registry.EndMap();
        f.Reject(token);
        Check(f.Registry.Players.Count == 0 && !f.Registry.TryCapture(2, out _), "map end retained usable context");
    }),
    ("map start with same entities rejects old map", f =>
    {
        var token = f.Capture();
        f.Registry.EndMap();
        f.Registry.BeginMap();
        f.Registry.Reconcile();
        f.Reject(token);
        Check(f.Capture().ConnectionGeneration > token.ConnectionGeneration, "generation reset on map");
    }),
    ("unload rejects and cannot reactivate old registry", f =>
    {
        var token = f.Capture();
        f.Registry.Unload();
        f.Registry.BeginMap();
        f.Registry.BeginRound();
        f.Registry.Reconcile();
        f.Reject(token);
        Check(!f.Registry.TryCapture(2, out _), "unloaded registry recaptured");
    }),
    ("hot reload rejects previous plugin even with matching epochs/generations", f =>
    {
        var token = f.Capture();
        var replacement = new PlayerRegistry(f.Source);
        replacement.BeginMap();
        replacement.Reconcile();
        Check(!replacement.TryResolve(token, out _, out var reason) && reason == "plugin lifetime", "reload accepted old token");
        Check(replacement.TryCapture(2, out _), "hot-load bootstrap missing");
    }),
    ("late load discovers existing human and zombie", f =>
    {
        f.Source.Players[3] = new(3, 103, 8, 203, PlayerRole.ZombieBot, true);
        f.Registry.Reconcile();
        Check(f.Registry.Players.Count == 2 && f.Registry.Humans.Count == 1 && f.Registry.ZombieBots.Count == 1, "bootstrap views wrong");
    }),
    ("views share one immutable registry context", f =>
    {
        Check(ReferenceEquals(f.Registry.Humans.Single(), f.Registry.Players.Single()), "view copied identity");
        var previous = f.Registry.Humans.Single();
        f.Source.Players[2] = f.Source.Players[2] with { Role = PlayerRole.ZombieBot };
        f.Registry.Reconcile();
        Check(f.Registry.Humans.Count == 0 && f.Registry.ZombieBots.Count == 1, "role indexes diverged");
        Check(previous.Role == PlayerRole.Human && ReferenceEquals(f.Registry.ZombieBots.Single(), f.Registry.Players.Single()), "immutable snapshot changed");
    }),
    ("callback receives current role rather than captured role", f =>
    {
        var token = f.Capture();
        f.Source.Players[2] = f.Source.Players[2] with { Role = PlayerRole.Other };
        Check(f.Registry.TryResolve(token, out var current, out _) && current!.Role == PlayerRole.Other, "callback used obsolete role");
    }),
    ("default/foreign token rejects", f => f.Reject(default)),
    ("invalid slots cannot be captured or indexed", f =>
    {
        f.Source.Players[64] = new(64, 1, 1, 1, PlayerRole.Human, true);
        f.Registry.Reconcile();
        Check(!f.Registry.TryCapture(-1, out _) && !f.Registry.TryCapture(64, out _) && f.Registry.Players.Count == 1, "out of range identity");
    }),
    ("unchanged reconciliation does not invalidate valid work", f =>
    {
        var token = f.Capture();
        for (var i = 0; i < 100; i++) f.Registry.Reconcile();
        Check(f.Registry.TryResolve(token, out _, out _), "stable observations churned generations");
    }),
    ("repeated slot reuse stays bounded and never revives tokens", f =>
    {
        var tokens = new List<PlayerLifetime>();
        for (var i = 0; i < 100; i++)
        {
            tokens.Add(f.Capture());
            f.Registry.Disconnect(2);
            f.Registry.Connect(2);
        }
        foreach (var token in tokens) f.Reject(token);
        Check(f.Registry.Players.Count == 1, "tombstones grew per connection");
    })
};

var failures = 0;
foreach (var (name, run) in scenarios)
{
    try { run(new Fixture()); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {name}: {error.Message}"); }
}
Console.WriteLine($"{scenarios.Length - failures}/{scenarios.Length} scenarios passed (deterministic model tests; CS2 runtime NOT TESTED)");
return failures == 0 ? 0 : 1;

static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }

sealed class Fixture
{
    public FakeSource Source { get; } = new();
    public PlayerRegistry Registry { get; }
    public Fixture()
    {
        Source.Players[2] = new(2, 101, 7, 201, PlayerRole.Human, true);
        Registry = new(Source);
        Registry.BeginMap();
        Registry.Reconcile();
    }
    public PlayerLifetime Capture()
    {
        if (!Registry.TryCapture(2, out var token)) throw new InvalidOperationException("capture failed");
        return token;
    }
    public void Reject(PlayerLifetime token)
    {
        if (Registry.TryResolve(token, out _, out _)) throw new InvalidOperationException("stale callback accepted");
    }
}

sealed class FakeSource : IPlayerObservationSource
{
    public Dictionary<int, PlayerObservation> Players { get; } = new();
    public int Reads { get; private set; }
    public IEnumerable<int> GetSlots() => Players.Keys;
    public PlayerObservation? Read(int slot)
    {
        Reads++;
        return Players.TryGetValue(slot, out var observation) ? observation : null;
    }
}
