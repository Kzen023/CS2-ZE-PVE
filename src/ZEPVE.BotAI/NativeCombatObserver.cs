using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using ZEPVE.Abstractions;
namespace ZEPVE.BotAI;

/// <summary>Schema reads only. No offsets, native detours, BotController calls or field mutation.</summary>
internal sealed class NativeCombatObserver : ICombatObserver
{
    public CombatObservation Read(BotTargetBinding binding)
    {
        var core = SuiteRuntime.Current;
        if (core is null || !core.TryResolve(binding.Bot, out _, out _)
            || !core.TryResolve(binding.Target, out _, out _)) return Unavailable("Core identity invalid");
        try
        {
            var controller = Utilities.GetPlayerFromSlot(binding.Bot.Slot);
            var targetController = Utilities.GetPlayerFromSlot(binding.Target.Slot);
            var pawn = controller?.PlayerPawn.Value;
            var target = targetController?.PlayerPawn.Value;
            if (controller is not { IsValid: true } || targetController is not { IsValid: true }
                || pawn is not { IsValid: true } || target is not { IsValid: true }
                || pawn.Controller.Raw != controller.EntityHandle.Raw || target.Controller.Raw != targetController.EntityHandle.Raw)
                return Unavailable("current pawn/controller unavailable");
            var bot = pawn.Bot;
            if (bot is null || bot.Handle == IntPtr.Zero) return Unavailable("CCSBot unavailable");
            var enemy = bot.Enemy.Raw;
            return new(true, enemy, enemy == target.EntityHandle.Raw, bot.IsEnemyVisible,
                bot.IsAimingAtEnemy, bot.IsAttacking, bot.IsSleeping, bot.AllowActive, "CSS schema read; transient engine perception");
        }
        catch (Exception error)
        {
            // Unsupported schema observations degrade independently of authoritative targeting.
            return Unavailable($"schema observation unavailable: {error.GetType().Name}");
        }
    }
    private static CombatObservation Unavailable(string detail) => new(false, null, false, false, false, false, false, false, detail);
}
