using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace ZEPVE.Core;

public sealed class CorePlugin : BasePlugin
{
    public override string ModuleName => "ZEPVE Core Observer";
    public override string ModuleVersion => "0.2.0-b";
    public override string ModuleAuthor => "ZEPVE";
    public override string ModuleDescription => "Registry and lifecycle observation; legacy remains the only gameplay writer.";

    private PlayerRegistry _registry = null!;
    private Timer? _reconcileTimer;
    private readonly HashSet<Timer> _probes = new();
    private readonly List<Action> _unsubscribe = new();

    public override void Load(bool hotReload)
    {
        _registry = new(new CounterStrikePlayerSource());
        Listen<Listeners.OnMapStart>(_ => InitializeMap());
        Listen<Listeners.OnMapEnd>(EndMap);
        Listen<Listeners.OnClientConnected>(slot => _registry.Connect(slot));
        Listen<Listeners.OnClientPutInServer>(slot => _registry.Observe(slot));
        Listen<Listeners.OnClientDisconnect>(slot => _registry.Disconnect(slot));
        Handle<EventPlayerSpawn>((e, _) => ObserveLife(e.Userid, false));
        Handle<EventPlayerDeath>((e, _) => ObserveLife(e.Userid, true));
        Handle<EventPlayerTeam>((e, _) => { Observe(e.Userid); return HookResult.Continue; });
        Handle<EventRoundPrestart>((_, _) => { _registry.InvalidateRound(); return HookResult.Continue; });
        Handle<EventRoundStart>((_, _) =>
        {
            _registry.BeginRound();
            _registry.Reconcile();
            Logger.LogInformation("Core observed round start: MapEpoch={MapEpoch} RoundEpoch={RoundEpoch}", _registry.MapEpoch, _registry.RoundEpoch);
            return HookResult.Continue;
        });
        Handle<EventRoundEnd>((_, _) => { _registry.InvalidateRound(); return HookResult.Continue; });
        AddCommand("css_zepve_status", "Show Core registry/lifecycle observations.", Status);
        AddCommand("css_zepve_probe", "Observer-only delayed validity probe: <slot> [seconds].", Probe);
        // Late/hot load cannot wait for a map-start event that already happened.
        if (!string.IsNullOrWhiteSpace(Server.MapName)) InitializeMap();
        Logger.LogInformation("Core observer loaded (hotReload={HotReload}, PluginLifetime={Lifetime}); legacy remains the only gameplay writer; no authority handoff", hotReload, _registry.PluginLifetime);
    }

    private void InitializeMap()
    {
        StopTimers();
        _registry.BeginMap();
        _registry.Reconcile();
        _reconcileTimer = AddTimer(0.5f, _registry.Reconcile, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        Logger.LogInformation("Core observed map initialization: MapEpoch={MapEpoch} RoundEpoch={RoundEpoch}", _registry.MapEpoch, _registry.RoundEpoch);
    }

    private void EndMap()
    {
        _registry.EndMap();
        StopTimers();
    }

    public override void Unload(bool hotReload)
    {
        _registry.Unload(); // Reject tokens before tearing down callbacks.
        StopTimers();
        foreach (var unsubscribe in _unsubscribe) unsubscribe();
        _unsubscribe.Clear();
        RemoveCommand("css_zepve_status", Status);
        RemoveCommand("css_zepve_probe", Probe);
        Logger.LogInformation("Core observer unloaded (hotReload={HotReload})", hotReload);
    }

    private void Listen<T>(T listener) where T : Delegate
    {
        RegisterListener(listener);
        _unsubscribe.Add(() => RemoveListener(listener));
    }

    private void Handle<T>(GameEventHandler<T> handler) where T : GameEvent
    {
        RegisterEventHandler(handler, HookMode.Post);
        _unsubscribe.Add(() => DeregisterEventHandler(handler, HookMode.Post));
    }

    private void StopTimers()
    {
        _reconcileTimer?.Kill();
        _reconcileTimer = null;
        foreach (var timer in _probes) timer.Kill();
        _probes.Clear();
    }

    private void Observe(CCSPlayerController? player)
    {
        if (player is { IsValid: true }) _registry.Observe(player.Slot);
    }

    private HookResult ObserveLife(CCSPlayerController? player, bool death)
    {
        if (player is { IsValid: true })
            _registry.ObserveLifeEvent(player.Slot, player.EntityHandle.Raw, player.UserId, death);
        return HookResult.Continue;
    }

    private void Status(CCSPlayerController? player, CommandInfo command)
    {
        _registry.Reconcile();
        command.ReplyToCommand($"[ZEPVE Core observer] MapEpoch={_registry.MapEpoch} RoundEpoch={_registry.RoundEpoch} Humans={_registry.Humans.Count} ZombieBots={_registry.ZombieBots.Count}");
        foreach (var context in _registry.Players.OrderBy(p => p.Slot))
            command.ReplyToCommand($"Slot={context.Slot} Role={context.Role} Connected={context.Connected} Alive={context.Alive} ConnectionGeneration={context.ConnectionGeneration} PawnGeneration={context.PawnGeneration}");
    }

    private void Probe(CCSPlayerController? player, CommandInfo command)
    {
        if (player is not null && !AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            command.ReplyToCommand("[ZEPVE Core] css_zepve_probe requires @css/root or server console.");
            return;
        }
        var delay = 5f;
        if (command.ArgCount is < 2 or > 3 || !int.TryParse(command.GetArg(1), out var slot)
            || (command.ArgCount == 3 && !float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out delay))
            || !float.IsFinite(delay) || delay is < 0.1f or > 60f)
        {
            command.ReplyToCommand("Usage: css_zepve_probe <slot> [seconds: 0.1..60]");
            return;
        }
        if (_probes.Count >= 16 || !_registry.TryCapture(slot, out var lifetime))
        {
            command.ReplyToCommand("[ZEPVE Core] Probe unavailable: no live valid pawn/observation round, or 16 probes pending.");
            return;
        }
        Timer? timer = null;
        // Capture the owning registry instance as well as its token; old work cannot bind to a reload.
        var registry = _registry;
        timer = AddTimer(delay, () =>
        {
            if (timer is not null) _probes.Remove(timer);
            var accepted = registry.TryResolve(lifetime, out _, out var rejection);
            Logger.LogInformation("Core delayed observation probe: Slot={Slot} Result={Result} Reason={Reason}",
                lifetime.Slot, accepted ? "ACCEPT" : "REJECT", accepted ? "current identity" : rejection);
        }, TimerFlags.STOP_ON_MAPCHANGE);
        _probes.Add(timer);
        command.ReplyToCommand($"[ZEPVE Core] Observation probe queued for Slot={slot}; result goes to the server log.");
    }
}
