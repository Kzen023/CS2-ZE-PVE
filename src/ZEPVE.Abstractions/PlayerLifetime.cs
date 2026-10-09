namespace ZEPVE.Abstractions;

/// <summary>An opaque validity stamp; obtaining one never grants gameplay permission.</summary>
public readonly record struct PlayerLifetime(
    Guid PluginLifetime,
    long MapEpoch,
    long RoundEpoch,
    int Slot,
    long ConnectionGeneration,
    long PawnGeneration);
