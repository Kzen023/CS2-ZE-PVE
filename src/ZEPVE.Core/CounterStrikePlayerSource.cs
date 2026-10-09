using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using ZEPVE.Abstractions;

namespace ZEPVE.Core;

internal sealed class CounterStrikePlayerSource : IPlayerObservationSource
{
    public IEnumerable<int> GetSlots()
    {
        foreach (var player in Utilities.GetPlayers())
            if (player.IsValid) yield return player.Slot;
    }

    public PlayerObservation? Read(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        if (player is not { IsValid: true } || player.Connected != PlayerConnectedState.Connected) return null;
        var role = player.IsHLTV ? PlayerRole.Other
            : !player.IsBot && player.Team == CsTeam.CounterTerrorist ? PlayerRole.Human
            : player.IsBot && player.Team == CsTeam.Terrorist ? PlayerRole.ZombieBot
            : PlayerRole.Other;
        var pawn = player.PlayerPawn.Value;
        // Use complete serial-bearing handles, not entity indexes or memory pointers.
        var pawnValid = pawn is { IsValid: true } && pawn.Controller.Raw == player.EntityHandle.Raw;
        return new(slot, player.EntityHandle.Raw, player.UserId,
            pawnValid ? player.PlayerPawn.Raw : null, role,
            pawnValid && pawn!.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.PawnIsAlive);
    }
}
