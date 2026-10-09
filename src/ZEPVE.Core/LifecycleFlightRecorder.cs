using ZEPVE.Abstractions;

namespace ZEPVE.Core;

internal sealed class LifecycleFlightRecorder
{
    private readonly LifecycleEntry[] _entries;
    private long _sequence;
    public int Count { get; private set; }
    public int Capacity => _entries.Length;
    public long Dropped => Math.Max(0, _sequence - Capacity);
    public LifecycleFlightRecorder(int capacity = 256)
    {
        if (capacity is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(capacity));
        _entries = new LifecycleEntry[capacity];
    }
    public void Add(LifecycleEventKind kind, LifecycleState state, string detail, ZepvePlayerContext? player = null)
    {
        var sequence = ++_sequence;
        _entries[(int)((sequence - 1) % Capacity)] = new(sequence, kind, state, player?.Slot,
            player?.ConnectionGeneration ?? 0, player?.PawnGeneration ?? 0,
            detail.Length > 160 ? detail[..160] : detail);
        Count = Math.Min(Count + 1, Capacity);
    }
    public IReadOnlyList<LifecycleEntry> Read(int maximum)
    {
        var count = Math.Clamp(maximum, 0, Count);
        var result = new LifecycleEntry[count];
        for (var i = 0; i < count; i++) result[i] = _entries[(int)((_sequence - count + i) % Capacity)];
        return Array.AsReadOnly(result);
    }
}
