using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using ZEPVE.Abstractions;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
namespace ZEPVE.BotAI;

public sealed class BotAiPlugin : BasePlugin
{
    public override string ModuleName => "ZEPVE BotAI";
    public override string ModuleVersion => "0.2.0-d";
    public override string ModuleAuthor => "ZEPVE";
    public override string ModuleDescription => "AssignedTarget authority; bounded observation-only reacquisition; no movement/native writes.";
    private BotAiService? _service;
    private Timer? _timer;
    private bool _dirty = true;
    private double _nextEvaluation;
    private readonly List<Action> _unsubscribe = new();

    public override void Load(bool hotReload)
    {
        if (BotAiRuntime.Current is not null) throw new InvalidOperationException("Duplicate BotAI provider refused.");
        _service = new(() => SuiteRuntime.Current, new NativeCombatObserver(), () => Server.CurrentTime);
        BotAiRuntime.Publish(_service);
        try
        {
            Command("css_zepve_ai", "BotAI gate, binding and bounded recorder summary.", Summary);
            Command("css_zepve_bot", "Bot target/perception status: <slot>.", Bot);
            Command("css_zepve_ai_events", "Read bounded BotAI recorder: [count].", Events);
            Command("css_zepve_ai_probe", "Validate delayed binding: <slot> [seconds].", Probe);
            Command("css_zepve_reacquire", "Request bounded observation: <slot>.", Reacquire);
            Listen<Listeners.OnMapStart>(_ => _dirty = true);
            Listen<Listeners.OnMapEnd>(() => { _dirty = true; _service.Evaluate(); });
            Listen<Listeners.OnClientDisconnect>(_ => _dirty = true);
            Handle<EventPlayerSpawn>((_, _) => Dirty());
            Handle<EventPlayerDeath>((_, _) => Dirty());
            Handle<EventPlayerTeam>((_, _) => Dirty());
            Handle<EventRoundStart>((_, _) => Dirty());
            Handle<EventRoundEnd>((_, _) => Dirty());
            Handle<EventPlayerHurt>((e, _) =>
            {
                if (e.Attacker is { IsValid: true, IsBot: true } attacker)
                    _service.CombatEvent(attacker.Slot, "hurt attacker observed");
                if (e.Userid is { IsValid: true, IsBot: true } victim)
                    _service.CombatEvent(victim.Slot, "hurt victim observed");
                return HookResult.Continue;
            });
            // Consumer-owned timer survives map boundaries, reads current Core each time; unload kills it.
            _timer = new(0.25f, Tick, TimerFlags.REPEAT);
            _service.Evaluate();
            Logger.LogInformation("BotAI loaded: ModuleLifetime={Lifetime} hotReload={HotReload} NativeWrites=0 MovementWrites=0", _service.ModuleLifetime, hotReload);
        }
        catch { Unload(false); throw; }
    }
    private HookResult Dirty() { _dirty = true; return HookResult.Continue; }
    private void Tick()
    {
        if (_service is null || (!_dirty && Server.CurrentTime < _nextEvaluation)) return;
        _dirty = false; _nextEvaluation = Server.CurrentTime + 0.5;
        _service.Evaluate();
    }
    private void Command(string name, string description, CommandInfo.CommandCallback callback)
    { AddCommand(name, description, callback); _unsubscribe.Add(() => RemoveCommand(name, callback)); }
    private void Listen<T>(T listener) where T : Delegate
    { RegisterListener(listener); _unsubscribe.Add(() => RemoveListener(listener)); }
    private void Handle<T>(GameEventHandler<T> handler) where T : GameEvent
    { RegisterEventHandler(handler, HookMode.Post); _unsubscribe.Add(() => DeregisterEventHandler(handler, HookMode.Post)); }
    private static bool Admin(CCSPlayerController? p, CommandInfo c)
    {
        if (p is null || AdminManager.PlayerHasPermissions(p, "@css/root")) return true;
        c.ReplyToCommand("Requires server console or @css/root."); return false;
    }
    private bool Binding(CommandInfo c, out BotTargetBinding binding)
    {
        binding = default;
        _service!.Evaluate();
        if (c.ArgCount < 2 || !int.TryParse(c.GetArg(1), out var slot) || slot is < 0 or >= 64)
        { c.ReplyToCommand("Expected Bot slot 0..63."); return false; }
        var core = SuiteRuntime.Current;
        if (core is null || !core.TryCapture(slot, out var token) || !_service.TryGetAssignedTarget(token, out binding))
        { c.ReplyToCommand("No current binding; requires Released, live ZombieBot and live CT human."); return false; }
        return true;
    }
    private void Summary(CCSPlayerController? p, CommandInfo c)
    {
        if (!Admin(p, c)) return;
        _service!.Evaluate();
        c.ReplyToCommand($"[ZEPVE BotAI] ModuleLifetime={_service.ModuleLifetime} Gate={_service.Gate} Bindings={_service.BindingCount} PendingWork={_service.PendingWork} FlightRecords={_service.Recorder.Count}/256 Dropped={_service.Recorder.Dropped} Awareness=ObserveOnly NativeWrites=0 MovementWrites=0");
    }
    private void Bot(CCSPlayerController? p, CommandInfo c)
    {
        if (!Admin(p, c) || c.ArgCount != 2 || !Binding(c, out var binding)) return;
        if (!_service!.TryGetStatus(binding.Bot, out var s)) return;
        c.ReplyToCommand($"Bot={binding.Bot.Slot} AssignedTarget={binding.Target.Slot} BindingVersion={binding.BindingVersion} SelectionReason={s.SelectionReason} BotConnection={binding.Bot.ConnectionGeneration} BotPawn={binding.Bot.PawnGeneration} TargetConnection={binding.Target.ConnectionGeneration} TargetPawn={binding.Target.PawnGeneration}");
        c.ReplyToCommand($"Reacquire={s.Reacquire} Reason={s.ReacquireReason} Until={s.ReacquireUntil:F2} LastAssist={s.LastAssist} Combat={s.LastCombatEvent}");
        var o = s.Observation;
        c.ReplyToCommand($"Engine Available={o.Available} EnemyHandle={o.EnemyHandle} AssignedEnemy={o.AssignedEnemy} Visible={o.EnemyVisible} Aiming={o.AimingAtEnemy} Attacking={o.Attacking} Sleeping={o.Sleeping} AllowActive={o.AllowActive} Detail={o.Detail}");
    }
    private void Events(CCSPlayerController? p, CommandInfo c)
    {
        if (!Admin(p, c)) return;
        var count = 32;
        if (c.ArgCount > 2 || (c.ArgCount == 2 && !int.TryParse(c.GetArg(1), out count)) || count is < 1 or > 64)
        { c.ReplyToCommand("Usage: css_zepve_ai_events [count: 1..64]"); return; }
        foreach (var e in _service!.Recorder.Read(count))
            c.ReplyToCommand($"#{e.Sequence} Time={e.Time:F2} Bot={e.BotSlot} Binding={e.BindingVersion} {e.Kind}: {e.Detail}");
    }
    private void Probe(CCSPlayerController? p, CommandInfo c)
    {
        if (!Admin(p, c)) return;
        var delay = 5f;
        if (c.ArgCount is < 2 or > 3 || (c.ArgCount == 3 && !float.TryParse(c.GetArg(2), NumberStyles.Float,
            CultureInfo.InvariantCulture, out delay)) || !float.IsFinite(delay) || delay is < 0.1f or > 60)
        { c.ReplyToCommand("Usage: css_zepve_ai_probe <slot> [seconds: 0.1..60]"); return; }
        if (!Binding(c, out var binding)) return;
        if (!_service!.ScheduleProbe(binding, delay, (ok, reason) => Logger.LogInformation(
            "BotAI probe Bot={Slot} Binding={Version} Result={Result} Reason={Reason}", binding.Bot.Slot,
            binding.BindingVersion, ok ? "ACCEPT" : "REJECT", reason)))
        { c.ReplyToCommand("Probe unavailable: scope invalid or 16 probes pending."); return; }
        c.ReplyToCommand($"Binding probe queued Bot={binding.Bot.Slot} BindingVersion={binding.BindingVersion}.");
    }
    private void Reacquire(CCSPlayerController? p, CommandInfo c)
    {
        if (!Admin(p, c) || c.ArgCount != 2 || !Binding(c, out var binding)) return;
        var accepted = _service!.RequestReacquire(binding, ReacquireReason.Diagnostic, out var result);
        c.ReplyToCommand($"Reacquire request Accepted={accepted} Result={result}");
    }
    public override void Unload(bool hotReload)
    {
        _timer?.Kill(); _timer = null;
        foreach (var unsubscribe in _unsubscribe) unsubscribe(); _unsubscribe.Clear();
        if (_service is null) return;
        BotAiRuntime.Withdraw(_service); _service.Dispose(); _service = null;
        Logger.LogInformation("BotAI unloaded: owned scopes/timer/hooks/bindings disposed.");
    }
}
