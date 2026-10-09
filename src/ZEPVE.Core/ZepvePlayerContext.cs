using ZEPVE.Abstractions;

namespace ZEPVE.Core;

/// <summary>Immutable observation. Re-resolve through PlayerRegistry before delayed use.</summary>
public sealed class ZepvePlayerContext : IPlayerContext
{
    public int Slot { get; }
    public PlayerRole Role { get; }
    public PlayerTeam Team { get; }
    public bool IsBot { get; }
    public bool IsHLTV { get; }
    public bool Connected { get; }
    public bool Alive { get; }
    public long ConnectionGeneration { get; }
    public long PawnGeneration { get; }
    internal uint ControllerHandle { get; }
    internal int? UserId { get; }
    internal uint? PawnHandle { get; }

    internal ZepvePlayerContext(int slot, PlayerRole role, bool connected, bool alive,
        long connectionGeneration, long pawnGeneration, uint controllerHandle, int? userId, uint? pawnHandle,
        PlayerTeam team = PlayerTeam.CounterTerrorist, bool isBot = false, bool isHLTV = false)
    {
        Slot = slot;
        Role = role;
        Team = team;
        IsBot = isBot;
        IsHLTV = isHLTV;
        Connected = connected;
        Alive = alive;
        ConnectionGeneration = connectionGeneration;
        PawnGeneration = pawnGeneration;
        ControllerHandle = controllerHandle;
        UserId = userId;
        PawnHandle = pawnHandle;
    }
}
