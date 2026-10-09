using ZEPVE.Abstractions;

namespace ZEPVE.Core;

// The engine adapter supplies observations, never generations or authoritative identities.
internal readonly record struct PlayerObservation(
    int Slot, uint ControllerHandle, int? UserId, uint? PawnHandle, PlayerRole Role, bool Alive);

internal interface IPlayerObservationSource
{
    IEnumerable<int> GetSlots();
    PlayerObservation? Read(int slot);
}
