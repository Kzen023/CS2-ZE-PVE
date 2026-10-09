using ZEPVE.Abstractions;

namespace ZEPVE.Core;

/// <summary>Immutable observation. Re-resolve through PlayerRegistry before delayed use.</summary>
public sealed class ZepvePlayerContext
{
    public int Slot { get; }
    public PlayerRole Role { get; }
    public bool Connected { get; }
    public bool Alive { get; }
    public long ConnectionGeneration { get; }
    public long PawnGeneration { get; }
    internal uint ControllerHandle { get; }
    internal int? UserId { get; }
    internal uint? PawnHandle { get; }

    internal ZepvePlayerContext(int slot, PlayerRole role, bool connected, bool alive,
        long connectionGeneration, long pawnGeneration, uint controllerHandle, int? userId, uint? pawnHandle)
    {
        Slot = slot;
        Role = role;
        Connected = connected;
        Alive = alive;
        ConnectionGeneration = connectionGeneration;
        PawnGeneration = pawnGeneration;
        ControllerHandle = controllerHandle;
        UserId = userId;
        PawnHandle = pawnHandle;
    }
}
