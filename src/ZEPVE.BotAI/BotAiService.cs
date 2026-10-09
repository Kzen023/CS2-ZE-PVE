using ZEPVE.Abstractions;
namespace ZEPVE.BotAI;

internal interface ICombatObserver { CombatObservation Read(BotTargetBinding binding); }

/// <summary>Bounded target state only. No cached native entities, player registry or movement executor.</summary>
internal sealed class BotAiService(Func<ICoreLifecycle?> coreSource, ICombatObserver observer,
    Func<double> clock) : IBotAi, IDisposable
{
    private readonly State?[] _bots = new State?[64];
    private readonly PlayerLifetime?[] _humans = new PlayerLifetime?[64];
    private readonly PlayerLifetime?[] _liveBots = new PlayerLifetime?[64];
    private readonly int[] _counts = new int[64];
    private long _version;
    private int _tieCursor;
    private bool _disposed;
    private string _gate = "initializing";
    private ICoreLifecycle? _provider;
    private ICoreWorkScope? _work;
    internal BotAiRecorder Recorder { get; } = new();
    public Guid ModuleLifetime { get; } = Guid.NewGuid();
    public int BindingCount { get { var n = 0; foreach (var b in _bots) if (b is not null) n++; return n; } }
    public int PendingWork => _work?.PendingCount ?? 0;
    public string Gate => _gate;

    private bool Allowed(ICoreLifecycle? core) => !_disposed && core is { State.Loaded: true,
        State.MapOpen: true, State.RoundOpen: true, GameplayAuthorityActive: true,
        Round.Phase: PvePhase.Released };
    private static bool ResolveRole(ICoreLifecycle core, PlayerLifetime token, PlayerRole role,
        out string rejection)
    {
        if (!core.TryResolve(token, out var current, out rejection)) return false;
        if (current is not { Connected: true, Alive: true } || current.Role != role)
        { rejection = "role/permission"; return false; }
        return true;
    }
    public bool ValidateBinding(BotTargetBinding binding, out string rejection)
    {
        var core = coreSource();
        rejection = "";
        if (_disposed || binding.ModuleLifetime != ModuleLifetime) rejection = "BotAI module lifetime";
        else if (!Allowed(core)) rejection = "Core gameplay permission";
        else if (binding.Bot.Slot is < 0 or >= 64 || _bots[binding.Bot.Slot] is not { } state
            || state.Binding != binding) rejection = "target binding version";
        else if (!ResolveRole(core!, binding.Bot, PlayerRole.ZombieBot, out rejection)) { }
        else if (!ResolveRole(core!, binding.Target, PlayerRole.Human, out rejection)) { }
        return rejection.Length == 0;
    }
    public bool TryGetAssignedTarget(PlayerLifetime bot, out BotTargetBinding binding)
    {
        binding = default;
        if (bot.Slot is < 0 or >= 64 || _bots[bot.Slot] is not { } state || state.Binding.Bot != bot) return false;
        if (!ValidateBinding(state.Binding, out _)) return false;
        binding = state.Binding;
        return true;
    }
    public bool TryGetStatus(PlayerLifetime bot, out BotAiStatus status)
    {
        status = default;
        if (!TryGetAssignedTarget(bot, out var binding)) return false;
        var state = _bots[bot.Slot]!;
        status = new(binding, state.SelectionReason, state.Reacquire, state.Reason, state.Until,
            state.Observation, "ObserveOnly; NativeWrites=0", state.LastCombat);
        return true;
    }
    internal void Evaluate()
    {
        if (_disposed) return;
        var core = coreSource();
        if (!ReferenceEquals(core, _provider))
        {
            Clear("Core provider changed");
            _work?.Dispose(); _work = null;
            _provider = core;
            if (core is { State.Loaded: true }) _work = core.CreateWorkScope();
        }
        var gate = Allowed(core) ? "Released" : core is null ? "Core unavailable" : core.Round.Phase.ToString();
        if (_gate != gate) { Record(null, 0, "Permission", gate); _gate = gate; }
        if (!Allowed(core)) { Clear("Core gameplay permission"); return; }
        Array.Clear(_humans); Array.Clear(_liveBots); Array.Clear(_counts);
        // Snapshot slots first: TryCapture may replace Core's immutable contexts while enumerating.
        foreach (var p in core!.Humans) if (p.Slot is >= 0 and < 64) _humans[p.Slot] = default(PlayerLifetime);
        foreach (var p in core.ZombieBots) if (p.Slot is >= 0 and < 64) _liveBots[p.Slot] = default(PlayerLifetime);
        for (var slot = 0; slot < 64; slot++)
        {
            if (_humans[slot].HasValue)
                _humans[slot] = core.TryCapture(slot, out var human) && ResolveRole(core, human, PlayerRole.Human, out _) ? human : null;
            if (_liveBots[slot].HasValue)
                _liveBots[slot] = core.TryCapture(slot, out var bot) && ResolveRole(core, bot, PlayerRole.ZombieBot, out _) ? bot : null;
        }
        for (var slot = 0; slot < 64; slot++)
        {
            var state = _bots[slot];
            if (state is null) continue;
            if (_liveBots[slot] != state.Binding.Bot || _humans[state.Binding.Target.Slot] != state.Binding.Target)
                Remove(slot, "bot/target lifecycle or role changed", false);
            else _counts[state.Binding.Target.Slot]++;
        }
        for (var slot = 0; slot < 64; slot++)
        {
            if (_liveBots[slot] is not { } bot || _bots[slot] is not null) continue;
            var target = LeastLoaded();
            if (target >= 0) Bind(bot, _humans[target]!.Value, "least assigned; rotating slot tie");
        }
        // Minimal churn on population changes: move only excess bindings until max-min <= 1.
        for (var moves = 0; moves < 64; moves++)
        {
            var least = LeastLoaded(); var most = MostLoaded();
            if (least < 0 || most < 0 || _counts[most] - _counts[least] <= 1) break;
            for (var slot = 63; slot >= 0; slot--)
                if (_bots[slot] is { } state && state.Binding.Target.Slot == most)
                { Bind(state.Binding.Bot, _humans[least]!.Value, "population balance; excess assignment"); break; }
        }
        for (var slot = 0; slot < 64; slot++) if (_bots[slot] is { } state) Observe(state);
    }
    private int LeastLoaded()
    {
        var best = -1;
        for (var i = 0; i < 64; i++)
        {
            var slot = (_tieCursor + i) % 64;
            if (_humans[slot].HasValue && (best < 0 || _counts[slot] < _counts[best])) best = slot;
        }
        return best;
    }
    private int MostLoaded()
    {
        var best = -1;
        for (var slot = 0; slot < 64; slot++)
            if (_humans[slot].HasValue && (best < 0 || _counts[slot] > _counts[best])) best = slot;
        return best;
    }
    private void Bind(PlayerLifetime bot, PlayerLifetime human, string reason)
    {
        Remove(bot.Slot, "target rebind");
        var state = new State(new(ModuleLifetime, bot, human, ++_version), reason);
        _bots[bot.Slot] = state;
        _counts[human.Slot]++;
        _tieCursor = (human.Slot + 1) % 64;
        Record(bot.Slot, state.Binding.BindingVersion, "AssignedTarget", $"Target={human.Slot}; {reason}");
        RequestReacquire(state.Binding, ReacquireReason.TargetBound, out _);
    }
    private void Remove(int slot, string reason, bool counted = true)
    {
        if (_bots[slot] is not { } state) return;
        _bots[slot] = null;
        if (counted) _counts[state.Binding.Target.Slot] = Math.Max(0, _counts[state.Binding.Target.Slot] - 1);
        foreach (var probe in state.Probes.ToArray()) probe.Dispose();
        Record(slot, state.Binding.BindingVersion, "Invalidated", reason);
    }
    private void Clear(string reason)
    { for (var slot = 0; slot < 64; slot++) Remove(slot, reason); }
    public bool RequestReacquire(BotTargetBinding binding, ReacquireReason reason, out string result)
    {
        if (!Enum.IsDefined(reason)) { result = "invalid reason"; return false; }
        if (!ValidateBinding(binding, out result)) return false;
        var state = _bots[binding.Bot.Slot]!;
        if (state.Reacquire == ReacquirePhase.Observing) { result = "coalesced; existing deadline retained"; return true; }
        if (clock() < state.NextAllowed) { result = "cooldown"; return false; }
        state.Reacquire = ReacquirePhase.Observing;
        state.Reason = reason; state.Until = clock() + 3; state.NextAllowed = clock() + 10;
        result = "ObserveOnly; NativeWrites=0";
        Record(binding.Bot.Slot, binding.BindingVersion, "ReacquireStart", reason.ToString());
        return true;
    }
    private void Observe(State state)
    {
        if (!ValidateBinding(state.Binding, out _)) return;
        var previous = state.Observation;
        var observation = observer.Read(state.Binding);
        state.Observation = observation;
        if (previous.Available != observation.Available || previous.AssignedEnemy != observation.AssignedEnemy
            || previous.EnemyVisible != observation.EnemyVisible || previous.Sleeping != observation.Sleeping
            || previous.Attacking != observation.Attacking)
            Record(state.Binding.Bot.Slot, state.Binding.BindingVersion, "Perception",
                $"Available={observation.Available} AssignedEnemy={observation.AssignedEnemy} Visible={observation.EnemyVisible} Attacking={observation.Attacking} Sleeping={observation.Sleeping}");
        // Success means observed assigned enemy + visibility/aim/attack; it does not prove causation.
        var recovered = observation.Available && observation.AssignedEnemy
            && (observation.EnemyVisible || observation.AimingAtEnemy || observation.Attacking);
        if (recovered) state.LostSince = null;
        else state.LostSince ??= clock();
        if (state.Reacquire == ReacquirePhase.Observing)
        {
            if (recovered || clock() >= state.Until)
            {
                state.Reacquire = recovered ? ReacquirePhase.Succeeded : ReacquirePhase.TimedOut;
                Record(state.Binding.Bot.Slot, state.Binding.BindingVersion, "ReacquireStop", state.Reacquire.ToString());
            }
        }
        else if (observation.Available && state.LostSince is { } lost && clock() - lost >= 2 && clock() >= state.NextAllowed)
            RequestReacquire(state.Binding, ReacquireReason.PerceptionLost, out _);
    }
    internal void CombatEvent(int botSlot, string detail)
    {
        if (botSlot is < 0 or >= 64 || _bots[botSlot] is not { } state || !ValidateBinding(state.Binding, out _)) return;
        state.LastCombat = detail;
        Record(botSlot, state.Binding.BindingVersion, "Combat", detail);
    }
    internal bool ScheduleProbe(BotTargetBinding binding, float delay, Action<bool, string> result)
    {
        if (_work is null || PendingWork >= 16 || !ValidateBinding(binding, out _)) return false;
        var state = _bots[binding.Bot.Slot]!;
        IDisposable? lease = null;
        void Complete(bool accepted, string reason)
        {
            if (lease is not null) state.Probes.Remove(lease);
            Record(binding.Bot.Slot, binding.BindingVersion, accepted ? "ProbeAccept" : "StaleBindingRejected", reason);
            result(accepted, reason);
        }
        if (!_work.TrySchedulePlayer(binding.Bot, delay, _ =>
            { var accepted = ValidateBinding(binding, out var reason); Complete(accepted, accepted ? "current binding" : reason); },
            out lease, canceled: reason => Complete(false, reason))) return false;
        state.Probes.Add(lease!);
        return true;
    }
    private void Record(int? slot, long version, string kind, string detail) => Recorder.Add(clock(), slot, version, kind, detail);
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Clear("BotAI unload"); _work?.Dispose(); _work = null; _provider = null;
    }
    private sealed class State(BotTargetBinding binding, string reason)
    {
        public BotTargetBinding Binding = binding;
        public string SelectionReason = reason;
        public ReacquirePhase Reacquire;
        public ReacquireReason Reason;
        public double Until, NextAllowed;
        public double? LostSince;
        public CombatObservation Observation;
        public string LastCombat = "none observed";
        public readonly List<IDisposable> Probes = new();
    }
}
