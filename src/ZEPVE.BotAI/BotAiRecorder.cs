using ZEPVE.Abstractions;
namespace ZEPVE.BotAI;

internal sealed class BotAiRecorder
{
    private readonly BotAiEvent[] _entries = new BotAiEvent[256];
    private long _sequence;
    public int Count { get; private set; }
    public long Dropped => _sequence - Count;
    public void Add(double time, int? slot, long version, string kind, string detail)
    {
        var sequence = ++_sequence;
        _entries[(int)((sequence - 1) % _entries.Length)] = new(sequence, time, slot, version, kind,
            detail.Length > 160 ? detail[..160] : detail);
        Count = Math.Min(Count + 1, _entries.Length);
    }
    public IReadOnlyList<BotAiEvent> Read(int maximum = 32)
    {
        var count = Math.Clamp(maximum, 0, Math.Min(Count, 64));
        var result = new BotAiEvent[count];
        for (var i = 0; i < count; i++)
        {
            var sequence = _sequence - count + i + 1;
            result[i] = _entries[(int)((sequence - 1) % _entries.Length)];
        }
        return result;
    }
}
