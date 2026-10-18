using System.Diagnostics;
using ZEPVE.Abstractions;
using ZEPVE.Core;

static class AuthorityTests
{
    public static (int Passed, int Total) Run()
    {
        var tests = new (string Name, Action<AuthorityFixture> Test)[]
        {
            ("scheduled player work re-reads native source at execution", f =>
            {
                var invoked = 0; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 2, _ => invoked++, out _);
                var reads = f.Source.Reads; f.Clock.Advance(2);
                Check(invoked == 1 && f.Source.Reads > reads, "cached identity used");
            }),
            ("disconnect eagerly cancels scheduled work and late native callback", f =>
            {
                var invoked = 0; var reason = ""; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 5, _ => invoked++, out _, canceled: value => reason = value);
                var timer = f.Clock.Last;
                f.Registry.Disconnect(2); timer!.ForceFire(); f.Clock.Advance(5);
                Check(invoked == 0 && owner.PendingCount == 0 && reason.Contains("disconnect"), "stale work survived");
            }),
            ("slot reuse never revives queued work", f =>
            {
                var invoked = 0; using var owner = f.Api.CreateWorkScope();
                var token = f.Capture(); owner.TrySchedulePlayer(token, 3, _ => invoked++, out _);
                f.Registry.Disconnect(2); f.Registry.Connect(2); f.Clock.Advance(3);
                Check(invoked == 0 && f.Capture().ConnectionGeneration > token.ConnectionGeneration, "slot reused identity");
            }),
            ("missed pawn event is caught by execution-time refresh", f =>
            {
                var invoked = 0; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 3, _ => invoked++, out _);
                f.Source.Players[2] = f.Source.Players[2] with { PawnHandle = 999 };
                f.Clock.Advance(3); Check(invoked == 0, "replaced pawn action executed");
            }),
            ("same-slot same-handle respawn cancels old work", f =>
            {
                var invoked = 0; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 3, _ => invoked++, out _);
                f.Registry.ObserveLifeEvent(2, 101, 7, true);
                f.Registry.ObserveLifeEvent(2, 101, 7, false); f.Clock.Advance(3);
                Check(invoked == 0, "old pawn work ran on respawn");
            }),
            ("round restart cancels work eagerly", f =>
            {
                var invoked = 0; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 4, _ => invoked++, out _);
                f.Registry.InvalidateRound(); f.Registry.BeginRound(); f.Clock.Advance(4);
                Check(invoked == 0 && owner.PendingCount == 0, "old round action ran");
            }),
            ("map change cancels both player and map-scoped work", f =>
            {
                var invoked = 0; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 4, _ => invoked++, out _);
                owner.TryScheduleServer(f.Api.CaptureServer(), 4, () => invoked++, out _, ServerValidity.Map);
                f.Registry.EndMap(); f.Registry.BeginMap(); f.Clock.Advance(4);
                Check(invoked == 0 && owner.PendingCount == 0, "old map work ran");
            }),
            ("unload rejects cached API and cancels pending work", f =>
            {
                var invoked = 0; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 4, _ => invoked++, out _);
                f.Registry.Unload(); f.Clock.Advance(4);
                Check(!f.Api.State.Loaded && !f.Api.TryCapture(2, out _) && invoked == 0, "unloaded provider usable");
            }),
            ("consumer unload cancels only its jobs", f =>
            {
                var a = 0; var b = 0; var owner = f.Api.CreateWorkScope(); using var other = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 2, _ => a++, out _);
                other.TrySchedulePlayer(f.Capture(), 2, _ => b++, out _);
                owner.Dispose(); f.Clock.Advance(2); Check(a == 0 && b == 1, "owner isolation failed");
            }),
            ("explicit cancel is idempotent", f =>
            {
                var invoked = 0; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 1, _ => invoked++, out var job);
                job!.Dispose(); job.Dispose(); f.Clock.Advance(1); Check(invoked == 0, "canceled job ran");
            }),
            ("scheduler disposal blocks rescheduling from cancel callbacks", f =>
            {
                using var owner = f.Api.CreateWorkScope(); var token = f.Capture(); var accepted = true;
                owner.TrySchedulePlayer(token, 5, _ => { }, out _, canceled: reason => accepted = owner.TrySchedulePlayer(token, 5, _ => { }, out _));
                f.Scheduler.Dispose(); Check(!accepted && f.Scheduler.PendingCount == 0, "dispose leaked rescheduled work");
            }),
            ("scheduler capacity is bounded and reclaimed", f =>
            {
                f.Policy.Dispose(); using var owner = f.Api.CreateWorkScope(); var token = f.Capture();
                for (var i = 0; i < 256; i++) Check(owner.TrySchedulePlayer(token, 60, _ => { }, out _), "capacity too small");
                Check(!owner.TrySchedulePlayer(token, 60, _ => { }, out _) && f.Scheduler.PendingCount == 256, "unbounded scheduler");
                owner.Dispose(); using var next = f.Api.CreateWorkScope();
                Check(f.Scheduler.PendingCount == 0 && next.TrySchedulePlayer(token, 1, _ => { }, out _), "capacity leaked");
            }),
            ("invalid delay rejected without scheduling", f =>
            {
                using var owner = f.Api.CreateWorkScope(); var token = f.Capture();
                Check(!owner.TrySchedulePlayer(token, float.NaN, _ => { }, out _)
                    && !owner.TrySchedulePlayer(token, -1, _ => { }, out _), "invalid timer accepted");
            }),
            ("current role reaches the callback", f =>
            {
                PlayerRole? role = null; using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 2, p => role = p.Role, out _);
                f.Source.Players[2] = f.Source.Players[2] with { Role = PlayerRole.Other };
                f.Registry.Reconcile(); f.Clock.Advance(2); Check(role == PlayerRole.Other, "stale role returned");
            }),
            ("controller-only token permits dead controller but tracks pawn generation", f =>
            {
                f.Source.Players[2] = f.Source.Players[2] with { Alive = false, PawnHandle = null };
                Check(f.Api.TryCapture(2, out var token, PlayerValidity.Connected) && !f.Api.TryCapture(2, out _), "validity levels wrong");
                Check(f.Api.TryResolve(token, out _, out _, PlayerValidity.Connected), "dead controller rejected");
                f.Source.Players[2] = f.Source.Players[2] with { PawnHandle = 201 };
                Check(!f.Api.TryResolve(token, out _, out _, PlayerValidity.Connected), "pawn generation ignored");
            }),
            ("map-only work survives round restart; round work does not", f =>
            {
                var map = 0; var round = 0; using var owner = f.Api.CreateWorkScope(); var token = f.Api.CaptureServer();
                owner.TryScheduleServer(token, 2, () => map++, out _, ServerValidity.Map);
                owner.TryScheduleServer(token, 2, () => round++, out _);
                f.Registry.InvalidateRound(); f.Registry.BeginRound(); f.Clock.Advance(2);
                Check(map == 1 && round == 0, "scope boundaries wrong");
            }),
            ("callback exceptions release jobs", f =>
            {
                using var owner = f.Api.CreateWorkScope();
                owner.TrySchedulePlayer(f.Capture(), 1, _ => throw new Exception("test failure"), out _);
                f.Clock.Advance(1); Check(f.Errors.Count == 1 && owner.PendingCount == 0, "exception leaked job");
            }),
            ("recorder stays bounded under 10000 events", f =>
            {
                var recorder = new LifecycleFlightRecorder(32);
                for (var i = 0; i < 10000; i++) recorder.Add(LifecycleEventKind.Policy, f.Api.State, new string('x', 1000));
                var entries = recorder.Read(1000);
                Check(recorder.Count == 32 && recorder.Dropped == 9968 && entries.Count == 32, "unbounded recorder");
                Check(entries[0].Sequence == 9969 && entries[^1].Sequence == 10000 && entries.All(e => e.Detail.Length == 160), "ring order/detail bound wrong");
            }),
            ("recorder snapshots are immutable", f =>
            {
                var snapshot = f.Api.ReadFlightRecorder();
                Throws<NotSupportedException>(() => ((IList<LifecycleEntry>)snapshot)[0] = default);
            }),
            ("recorder records all requested lifecycle categories", f =>
            {
                var token = f.Capture(); f.Source.Players[2] = f.Source.Players[2] with { Role = PlayerRole.Other };
                f.Registry.Reconcile(); f.Registry.ObserveLifeEvent(2, 101, 7, false);
                f.Registry.Disconnect(2); f.Registry.BeginRound(); f.Registry.BeginMap(); f.Api.TryResolve(token, out _, out _);
                f.Registry.Unload(); var kinds = f.Api.ReadFlightRecorder(256).Select(e => e.Kind).ToHashSet();
                foreach (var kind in new[] { LifecycleEventKind.Connect, LifecycleEventKind.Disconnect, LifecycleEventKind.RoleChange,
                    LifecycleEventKind.PawnGeneration, LifecycleEventKind.RoundEpoch, LifecycleEventKind.MapEpoch,
                    LifecycleEventKind.StaleTokenRejected, LifecycleEventKind.PluginLifetime }) Check(kinds.Contains(kind), "missing " + kind);
            }),
            ("read-only contract views share registry contexts", f => Check(ReferenceEquals(f.Api.Humans.Single(), f.Registry.Players.Single()), "second identity source")),
            ("duplicate Core provider refused", f => Throws<InvalidOperationException>(() => SuiteRuntime.Publish(new CoreRuntime(f.Registry, f.Scheduler, f.Policy)))),
            ("duplicate legacy bridge refused", f => Throws<InvalidOperationException>(() => SuiteRuntime.AttachLegacy(new FakeBridge()))),
            ("retired provider cannot be republished", f =>
            {
                f.Registry.Unload(); SuiteRuntime.Withdraw(f.Api);
                Throws<InvalidOperationException>(() => SuiteRuntime.Publish(f.Api));
            }),
            ("invalid validity enum fails closed", f =>
            {
                Check(!f.Api.TryCapture(2, out _, (PlayerValidity)99), "invalid player requirement accepted");
                Check(!f.Api.ValidateServer(f.Api.CaptureServer(), (ServerValidity)99, out _), "invalid server scope accepted");
            }),
            ("obsolete spawn event is rejected before bridge forwarding", f =>
            {
                f.Source.Players[2] = f.Source.Players[2] with { ControllerHandle = 999, UserId = 99 };
                Check(!f.Registry.ObserveLifeEvent(2, 101, 7, false), "obsolete spawn accepted");
                Check(f.Registry.ObserveLifeEvent(2, 999, 99, false), "current spawn rejected");
            }),
            ("Core without adapted legacy issues no gameplay commands", f =>
            {
                f.Detach(); f.Registry.BeginRound(); f.Policy.BeginRound(); f.Clock.Advance(30);
                Check(f.Game.Commands.Count == 0 && !f.Api.GameplayAuthorityActive && f.Api.Round.Phase == PvePhase.Unbound, "parallel/unguarded writer");
            }),
            ("bridge removal cancels gameplay work without fallback", f =>
            {
                f.BeginRound(); f.Game.Commands.Clear(); f.Detach(); f.Clock.Advance(30);
                Check(f.Game.Commands.Count == 0 && f.Game.Moves.Count == 0, "writer survived bridge removal");
            }),
            ("legacy static profiles preserve exact quota and respawn policy", f =>
            {
                var expected = new[] { (1, "solo", 10, 7f), (2, "duo", 10, 5f), (3, "coop", 6, 4f),
                    (6, "coop", 12, 4f), (7, "group", 14, 3f), (20, "group", 18, 3f) };
                foreach (var (humans, name, quota, delay) in expected)
                { var p = PveRoundController.PickProfile(humans, f.Bridge.Settings); Check(p.Name == name && p.BotQuota == quota && p.RespawnDelay == delay, "profile parity changed"); }
            }),
            ("round timing preserves 1s preparation plus infection delay", f =>
            {
                f.BeginRound(); Check(f.Game.Commands.Count == 2 && f.Bridge.Countdowns.Single() == 16, "countdown changed");
                f.Clock.Advance(0.99); Check(!f.Game.Commands.Any(c => c.Command == "bot_quota 0"), "preparation early");
                f.Clock.Advance(0.01); Check(f.Api.Round.Phase == PvePhase.AwaitingRelease && f.Api.Round.RespawnDelay == 7, "profile not applied at 1s");
                f.Clock.Advance(15.99); Check(!f.Game.Commands.Any(c => c.Command == "bot_join_team T"), "release early");
                f.Clock.Advance(0.01); Check(f.Api.Round.InfectionReleased && f.Game.Commands.Last().Command == "bot_kick", "release not at 17s");
                f.Clock.Advance(0.19); Check(!f.Game.Commands.Any(c => c.Command == "bot_quota 10"), "quota early");
                f.Clock.Advance(0.011); Check(f.Game.Commands.Any(c => c.Command == "bot_quota 10"), "quota missing");
                f.Clock.Advance(0.8); Check(f.Game.Moves.Single().Team == PlayerTeam.Terrorist, "team transition missing");
            }),
            ("golden solo command vector matches baseline", f =>
            {
                var expected = new[] {
                    "mp_buytime 9999", "mp_buy_anywhere 1", "bot_join_team CT", "bot_join_after_player 0", "bot_auto_vacate 0",
                    "bot_difficulty 3", "bot_coop_idle_max_vision_distance 4096", "bot_chatter off", "bot_defer_to_human_goals 0",
                    "bot_defer_to_human_items 0", "bot_allow_rogues 1", "bot_stop 0", "bot_path_require_reachable_goal 0",
                    "bot_quota_mode normal", "bot_quota 0", "zr_enable 1", "zr_napalm_enable 1", "zr_infect_spawn_type 1",
                    "zr_infect_spawn_warning 1", "zr_default_winner_team 3", "zr_infect_min_count_req 2", "zr_infect_spawn_mz_min_count 1",
                    "zr_infect_spawn_mz_ratio 16", "zr_infect_spawn_time_min 16", "zr_infect_spawn_time_max 16",
                    "zr_respawn_delay 7.0", "zr_knockback_scale 4.8", "zr_napalm_burn_duration 4.0" };
                Check(PveRoundController.ProfileCommands(f.Bridge.Settings, PveRoundController.PickProfile(1, f.Bridge.Settings)).SequenceEqual(expected), "baseline command values/order changed");
            }),
            ("count policy includes T humans and excludes Bots/HLTV/spectators", f =>
            {
                f.Source.Players[3] = new(3, 103, 3, 203, PlayerRole.Other, true, PlayerTeam.Terrorist);
                f.Source.Players[4] = new(4, 104, 4, 204, PlayerRole.Other, true, PlayerTeam.Terrorist);
                f.Source.Players[5] = new(5, 105, 5, 205, PlayerRole.ZombieBot, true, PlayerTeam.Terrorist, true);
                f.Source.Players[6] = new(6, 106, 6, 206, PlayerRole.Other, true, PlayerTeam.CounterTerrorist, false, true);
                f.Source.Players[7] = new(7, 107, 7, 207, PlayerRole.Other, true, PlayerTeam.Spectator);
                f.BeginRound(); f.Clock.Advance(1); Check(f.Api.Round.Profile == "coop" && f.Api.Round.BotQuota == 6, "human count semantics changed");
            }),
            ("disabled profile retains countdown/map timing but no quota writer", f =>
            {
                f.Bridge.Settings = f.Bridge.Settings with { Enabled = false }; f.Policy.BeginMap("de_dust2");
                f.BeginRound(); f.Clock.Advance(30);
                Check(f.Bridge.Countdowns.Single() == 16 && f.Game.Commands.Any(c => c.Command == "mp_roundtime 2.5"), "disabled baseline map/countdown changed");
                Check(!f.Game.Commands.Any(c => c.Command.StartsWith("bot_")), "disabled quota changed");
            }),
            ("round end cancels release/quota/team work", f =>
            {
                f.BeginRound(); f.Clock.Advance(1); f.Registry.InvalidateRound(); f.Policy.EndRound();
                var count = f.Game.Commands.Count; f.Clock.Advance(30);
                Check(f.Game.Commands.Count == count && f.Game.Moves.Count == 0 && !f.Api.Round.InfectionReleased, "ended round still wrote");
            }),
            ("reentrant round end during kick aborts continuation", f =>
            {
                f.Game.OnCommand = command => { if (command == "bot_kick") { f.Registry.InvalidateRound(); f.Policy.EndRound(); } };
                f.BeginRound(); f.Clock.Advance(30);
                Check(!f.Game.Commands.Any(c => c.Command == "bot_join_team T") && f.Api.Round.Phase == PvePhase.Closed, "reentrant old release survived");
            }),
            ("atomic hot reload preserves release deadline and no duplicate preparation", f =>
            {
                f.BeginRound(); f.Clock.Advance(5); var plan = f.Policy.ExportHotResume()!;
                var old = f.Capture(); f.Registry.Unload(); f.Policy.Dispose();
                var registry = new PlayerRegistry(f.Source); registry.BeginMap(); registry.Reconcile();
                using var scheduler = new LifecycleWorkScheduler(registry, f.Clock, f.Errors.Add);
                using var policy = new PveRoundController(registry, scheduler, f.Game, () => f.Clock.Now);
                policy.BeginMap("de_dust2"); policy.ResumeHot(plan);
                Check(!registry.TryResolve(old, out _, out _), "reload accepted old token");
                f.Clock.Advance(11.99); Check(!f.Game.Commands.Any(c => c.Command == "bot_join_team T"), "resumed release early");
                f.Clock.Advance(0.01); Check(f.Game.Commands.Count(c => c.Command == "bot_join_team T") == 1 && f.Game.Commands.Count(c => c.Command == "mp_autoteambalance 0") == 1 && f.Bridge.Countdowns.Count == 1, "hot reload replayed/reset policy");
            }),
            ("late bootstrap discovers players without inventing infection deadline", f =>
            {
                f.Clock.Advance(30); Check(f.Api.Humans.Count == 1 && f.Api.Round.Phase == PvePhase.Unbound, "late phase invented");
                Check(!f.Game.Commands.Any(c => c.Command.StartsWith("bot_")), "late load reset Bots");
            }),
            ("hot reload before preparation retains apply deadline", f =>
            {
                f.BeginRound(); f.Clock.Advance(0.4); var plan = f.Policy.ExportHotResume()!;
                f.Registry.Unload(); f.Policy.Dispose(); var registry = new PlayerRegistry(f.Source); registry.BeginMap(); registry.Reconcile();
                using var scheduler = new LifecycleWorkScheduler(registry, f.Clock, f.Errors.Add);
                using var policy = new PveRoundController(registry, scheduler, f.Game, () => f.Clock.Now);
                policy.BeginMap("de_dust2"); policy.ResumeHot(plan); f.Clock.Advance(0.6);
                Check(Math.Abs(f.Game.Commands.Single(c => c.Command == "mp_autoteambalance 0").Time - 1) < 0.000001
                    && f.Bridge.Countdowns.Count == 1, "preparation reset/doubled");
            }),
            ("hot reload after release retains only pending quota/team work", f =>
            {
                f.BeginRound(); f.Clock.Advance(17.1); var plan = f.Policy.ExportHotResume()!;
                f.Registry.Unload(); f.Policy.Dispose(); var registry = new PlayerRegistry(f.Source); registry.BeginMap(); registry.Reconcile();
                using var scheduler = new LifecycleWorkScheduler(registry, f.Clock, f.Errors.Add);
                using var policy = new PveRoundController(registry, scheduler, f.Game, () => f.Clock.Now);
                policy.BeginMap("de_dust2"); policy.ResumeHot(plan); f.Clock.Advance(1);
                Check(f.Game.Commands.Count(c => c.Command == "bot_join_team T") == 1
                    && f.Game.Commands.Count(c => c.Command == "bot_quota 10") == 1 && f.Game.Moves.Count == 1, "completed actions replayed");
            }),
            ("hot reload preserves disabled map-setting deadline", f =>
            {
                f.Bridge.Settings = f.Bridge.Settings with { Enabled = false }; f.Policy.BeginMap("de_dust2"); f.BeginRound();
                f.Clock.Advance(0.4); var plan = f.Policy.ExportHotResume()!; f.Registry.Unload(); f.Policy.Dispose();
                var registry = new PlayerRegistry(f.Source); registry.BeginMap(); registry.Reconcile();
                using var scheduler = new LifecycleWorkScheduler(registry, f.Clock, f.Errors.Add);
                using var policy = new PveRoundController(registry, scheduler, f.Game, () => f.Clock.Now);
                policy.BeginMap("de_dust2"); policy.ResumeHot(plan); f.Clock.Advance(0.6);
                Check(f.Game.Commands.Count(c => c.Command == "mp_roundtime 2.5") == 1
                    && Math.Abs(f.Game.Commands.Single(c => c.Command == "mp_roundtime 2.5").Time - 1) < 0.000001, "map deadline lost/reset");
            }),
            ("invalid lifecycle settings fail closed", f =>
            {
                f.Bridge.Settings = f.Bridge.Settings with { InfectionDelay = float.NaN };
                Throws<InvalidOperationException>(f.BeginRound); Check(f.Game.Commands.Count == 0, "invalid policy wrote");
            }),
            ("legacy writer disable and recovery/config parity source audit", _ => AuditLegacyBoundary())
        };
        var passed = 0;
        foreach (var (name, test) in tests)
        {
            try { using var fixture = new AuthorityFixture(); test(fixture); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception error) { Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
        }
        Console.WriteLine($"{passed}/{tests.Length} authority/scheduler/recorder/migration model checks passed; runtime NOT TESTED");
        return (passed, tests.Length);
    }
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    static void AuditLegacyBoundary()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "MIGRATION_AUTHORITY.md"))) root = root.Parent;
        Check(root is not null, "suite checkout not found");
        var path = "legacy/CounterStrikeSharp/plugins/Kzen-ZRPVE/ZrPvePlugin.cs";
        var current = File.ReadAllText(Path.Combine(root!.FullName, path)).Replace("\r\n", "\n");
        foreach (var removed in new[] { "ApplyProfile(", "ReleaseInfectionBots(", "MoveExistingBots(", "_roundToken", "private int _targetBotQuota", "AddTimer(", "RegisterListener<Listeners.OnMapStart>", "OnRoundStart(EventRoundStart" })
            Check(!current.Contains(removed), "legacy executor still present: " + removed);
        var executions = current.Split('\n').Where(line => line.Contains("Server.ExecuteCommand"));
        Check(executions.Count() == 1 && executions.Single().Contains("css_kzen_countdown"), "legacy gameplay writer remains");
        var start = new ProcessStartInfo("git") { WorkingDirectory = root.FullName, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("show"); start.ArgumentList.Add("89b5c0c0722d630ad5b9ce75b1234414d7f873d3:" + path);
        using var git = Process.Start(start)!; var baseline = git.StandardOutput.ReadToEnd().Replace("\r\n", "\n"); git.WaitForExit();
        Check(git.ExitCode == 0, "accepted baseline Git object unavailable");
        foreach (var removed in new[] { "RecordHumanPath(", "CheckZombieBots(", "TryRecoverZombieBot(", "TryFindRecoveryPoint(", "TryStartEscort(", "UpdateEscorts(", "QueueBotRecovery(", ".Teleport(" })
            Check(!current.Contains(removed), "legacy Navigation writer remains: " + removed);
        Check(current.Contains("MovementWriterDisabled => true"), "matched adapter has no explicit movement disable capability");
        Check(Method(baseline, "private static List<CCSPlayerPawn> GetLiveHumanPawns()") == Method(current, "private static List<CCSPlayerPawn> GetLiveHumanPawns()"), "retained HUD human view changed");
        var baselineConfig = baseline[baseline.IndexOf("    private sealed class ZrPveConfig")..baseline.IndexOf("    private sealed record PathPoint")];
        var currentConfig = current[current.IndexOf("    private sealed class ZrPveConfig")..current.IndexOf("    private sealed record PathPoint")];
        Check(baselineConfig == currentConfig, "legacy config parser/defaults changed");
    }
    static string Method(string text, string signature)
    {
        var start = text.IndexOf(signature, StringComparison.Ordinal); Check(start >= 0, "method missing: " + signature);
        var cursor = text.IndexOf('{', start); var depth = 0;
        do { if (text[cursor] == '{') depth++; else if (text[cursor] == '}') depth--; cursor++; } while (depth > 0);
        return text[start..cursor];
    }
}

sealed class AuthorityFixture : IDisposable
{
    public FakeSource Source { get; } = new();
    public FakeClock Clock { get; } = new();
    public FakeBridge Bridge { get; } = new();
    public List<Exception> Errors { get; } = new();
    public PlayerRegistry Registry { get; }
    public LifecycleWorkScheduler Scheduler { get; }
    public FakeGame Game { get; }
    public PveRoundController Policy { get; }
    public CoreRuntime Api { get; }
    public AuthorityFixture()
    {
        SuiteRuntime.AttachLegacy(Bridge);
        Source.Players[2] = new(2, 101, 7, 201, PlayerRole.Human, true);
        Registry = new(Source); Registry.BeginMap(); Registry.Reconcile();
        Scheduler = new(Registry, Clock, Errors.Add); Game = new(Clock);
        Policy = new(Registry, Scheduler, Game, () => Clock.Now); Api = new(Registry, Scheduler, Policy);
        SuiteRuntime.Publish(Api); Policy.BeginMap("de_dust2");
    }
    public PlayerLifetime Capture() { if (!Api.TryCapture(2, out var token)) throw new InvalidOperationException("capture failed"); return token; }
    public void BeginRound() { Registry.BeginRound(); Policy.BeginRound(); }
    public void Detach() { SuiteRuntime.DetachLegacy(Bridge); Policy.BridgeChanged(); }
    public void Dispose()
    {
        Registry.Unload(); Policy.Dispose(); Scheduler.Dispose(); SuiteRuntime.Withdraw(Api); SuiteRuntime.DetachLegacy(Bridge); SuiteRuntime.ResumePlan = null;
    }
}
sealed class FakeBridge : ILegacyPveBridge
{
    public LegacyPveSettings Settings { get; set; } = new(true, 10, 10, 6, 12, 12, 18, 16, 60, 2.5f, 9999, true, 0.2f, 1, 3, 4096, true, 1, true, 3);
    public List<int> Countdowns { get; } = new();
    public void ShowInfectionCountdown(int seconds) => Countdowns.Add(seconds);
    public void ObserveSpawn(int slot) { }
    public void ObserveMapStart(string mapName, bool resetData) { }
    public void ObserveRoundStart() { }
    public void SuspendMapServices() { }
}
sealed class FakeGame(FakeClock clock) : IPveGameAdapter
{
    public List<(double Time, string Command)> Commands { get; } = new();
    public List<(double Time, PlayerTeam Team)> Moves { get; } = new();
    public Action<string>? OnCommand;
    public void Execute(string command) { Commands.Add((clock.Now, command)); OnCommand?.Invoke(command); }
    public void MoveBots(PlayerTeam team, ServerLifetime token) => Moves.Add((clock.Now, team));
}
sealed class FakeClock : ITimerDispatcher
{
    private readonly List<FakeTimer> _timers = new();
    private int _sequence;
    public double Now { get; private set; }
    public FakeTimer? Last { get; private set; }
    public IDisposable Schedule(float seconds, Action action)
    {
        var timer = new FakeTimer(this, Now + seconds, ++_sequence, action); _timers.Add(timer); Last = timer; return timer;
    }
    public void Advance(double seconds)
    {
        var end = Now + seconds;
        while (_timers.OrderBy(t => t.Due).ThenBy(t => t.Sequence).FirstOrDefault(t => t.Due <= end + 0.0000001) is { } timer)
        { _timers.Remove(timer); Now = timer.Due; timer.ForceFire(); }
        Now = end;
    }
    public sealed class FakeTimer(FakeClock clock, double due, int sequence, Action action) : IDisposable
    {
        public double Due = due;
        public int Sequence = sequence;
        public void ForceFire() => action(); // Emulates an already-dispatched native callback after cancellation.
        public void Dispose() => clock._timers.Remove(this);
    }
}
