using ZEPVE.Abstractions;
using ZEPVE.BotAI;
using ZEPVE.Core;

static class BotAiTests
{
    public static (int Passed, int Total) Run()
    {
        var cases = new List<(string, Action<BotAiFixture>)>
        {
            ("new target authority independent of Enemy", f => { f.Observer.Value = default; f.Ai.Evaluate(); Check(f.Ai.BindingCount == 10); }),
            ("stable evaluation does not churn versions", f => { var b = f.Binding(); for (var i=0;i<100;i++) f.Ai.Evaluate(); Check(f.Binding() == b); }),
            ("getter refreshes native source", f => { var b=f.Binding(); var reads=f.Core.Source.Reads; Check(f.Ai.ValidateBinding(b,out _)); Check(f.Core.Source.Reads>reads); }),
            ("target disconnect rejects before evaluation", f => { var b=f.Binding(); f.Core.Registry.Disconnect(b.Target.Slot); Check(!f.Ai.ValidateBinding(b,out _)); }),
            ("target slot reuse cannot revive binding", f => { var b=f.Binding(); f.Core.Registry.Disconnect(2); f.Core.Registry.Connect(2); Check(!f.Ai.ValidateBinding(b,out _)); f.Ai.Evaluate(); Check(f.Binding().BindingVersion>b.BindingVersion); }),
            ("target death rejects", f => { var b=f.Binding(); f.Core.Source.Players[2]=f.Core.Source.Players[2] with {Alive=false}; Check(!f.Ai.ValidateBinding(b,out _)); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0); }),
            ("target same pawn respawn event invalidates", f => { var b=f.Binding(); f.Core.Registry.ObserveLifeEvent(2,101,7,true); f.Core.Registry.ObserveLifeEvent(2,101,7,false); f.Rebind(b); }),
            ("target pawn replacement rebinds", f => { var b=f.Binding(); f.Core.Source.Players[2]=f.Core.Source.Players[2] with {PawnHandle=901}; f.Rebind(b); }),
            ("bot death removes binding", f => { var b=f.Binding(); f.Core.Source.Players[10]=f.Core.Source.Players[10] with {Alive=false}; Check(!f.Ai.ValidateBinding(b,out _)); f.Ai.Evaluate(); Check(f.Ai.BindingCount==9); }),
            ("bot same slot respawn invalidates", f => { var b=f.Binding(); var p=f.Core.Source.Players[10]; f.Core.Registry.ObserveLifeEvent(10,p.ControllerHandle,p.UserId,true); f.Core.Registry.ObserveLifeEvent(10,p.ControllerHandle,p.UserId,false); f.Rebind(b); }),
            ("bot pawn replacement invalidates", f => { var b=f.Binding(); f.Core.Source.Players[10]=f.Core.Source.Players[10] with {PawnHandle=999}; f.Rebind(b); }),
            ("bot connection replacement invalidates", f => { var b=f.Binding(); f.Core.Source.Players[10]=f.Core.Source.Players[10] with {ControllerHandle=333}; f.Rebind(b); }),
            ("role change without lifetime change rejects", f => { var b=f.Binding(); f.Core.Source.Players[2]=f.Core.Source.Players[2] with {Role=PlayerRole.Other}; Check(!f.Ai.ValidateBinding(b,out var r)&&r=="role/permission"); }),
            ("bot CT role cannot retain binding", f => { var b=f.Binding(); f.Core.Source.Players[10]=f.Core.Source.Players[10] with {Role=PlayerRole.Other}; Check(!f.Ai.ValidateBinding(b,out _)); }),
            ("closed phase clears and rejects", f => { var b=f.Binding(); f.Core.Registry.InvalidateRound(); f.Core.Policy.EndRound(); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0&&!f.Ai.ValidateBinding(b,out _)); }),
            ("round restart rejects old binding", f => { var b=f.Binding(); f.Core.BeginRound(); Check(!f.Ai.ValidateBinding(b,out _)); f.Core.Clock.Advance(18); f.Ai.Evaluate(); Check(f.Binding().BindingVersion>b.BindingVersion); }),
            ("map change rejects", f => { var b=f.Binding(); f.Core.Registry.EndMap(); Check(!f.Ai.ValidateBinding(b,out _)); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0); }),
            ("Core unload invalidates", f => { var b=f.Binding(); f.Core.Registry.Unload(); Check(!f.Ai.ValidateBinding(b,out _)); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0); }),
            ("missing Core fails closed", f => { var b=f.Binding(); f.Provider=null; f.Ai.Evaluate(); Check(f.Ai.BindingCount==0&&!f.Ai.ValidateBinding(b,out _)); }),
            ("bridge loss revokes permission", f => { var b=f.Binding(); f.Core.Detach(); Check(!f.Ai.ValidateBinding(b,out _)); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0); }),
            ("manual late load Unbound has no assignments", f => { f.Core.Policy.BeginMap("de_mirage"); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0&&f.Ai.Gate=="Unbound"); }),
            ("preparation has no assignments", f => { f.Core.BeginRound(); f.Core.Clock.Advance(0.5); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0); }),
            ("AwaitingRelease has no assignments", f => { f.Core.BeginRound(); f.Core.Clock.Advance(2); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0); }),
            ("BotAI unload and new module reject old GUID", f => { var b=f.Binding(); f.Ai.Dispose(); using var next=new BotAiService(()=>f.Provider,f.Observer,()=>f.Core.Clock.Now); next.Evaluate(); Check(!next.ValidateBinding(b,out var r)&&r=="BotAI module lifetime"); }),
            ("forged binding version rejected", f => { var b=f.Binding(); Check(!f.Ai.ValidateBinding(b with {BindingVersion=b.BindingVersion+1},out _)); }),
            ("joining human balances with minimal churn", f => { var before=f.Bindings(); f.Human(3); f.Ai.Evaluate(); var after=f.Bindings(); Check(after.Count(b=>b.Target.Slot==3)==5); Check(before.Count(b=>after.Contains(b))==5); }),
            ("leaving human rebinds to remaining population", f => { f.Human(3); f.Ai.Evaluate(); f.Core.Registry.Disconnect(2); f.Ai.Evaluate(); Check(f.Bindings().All(b=>b.Target.Slot==3)); }),
            ("partial bot invalidation preserves balance", f => { f.Human(3); f.Ai.Evaluate(); f.Core.Source.Players[11]=f.Core.Source.Players[11] with {PawnHandle=900}; f.Ai.Evaluate(); f.Balanced(2); }),
            ("no eligible humans clears", f => { f.Core.Source.Players.Remove(2); f.Ai.Evaluate(); Check(f.Ai.BindingCount==0); }),
            ("unavailable observation leaves target intact", f => { var b=f.Binding(); f.Observer.Value=default; f.Ai.Evaluate(); Check(f.Binding()==b); }),
            ("enemy observation never selects target", f => { var b=f.Binding(); f.Observer.Value=new(true,1234,false,true,true,true,false,true,"foreign enemy"); f.Ai.Evaluate(); Check(f.Binding()==b); }),
            ("reacquire coalesces without extending deadline", f => { var b=f.Binding(); Check(f.Ai.TryGetStatus(b.Bot,out var s)); f.Core.Clock.Advance(1); Check(f.Ai.RequestReacquire(b,ReacquireReason.RecoveryTeleport,out _)); f.Ai.TryGetStatus(b.Bot,out var n); Check(n.ReacquireUntil==s.ReacquireUntil); }),
            ("reacquire times out and stops", f => { f.Core.Clock.Advance(3); f.Ai.Evaluate(); f.Ai.TryGetStatus(f.Binding().Bot,out var s); Check(s.Reacquire==ReacquirePhase.TimedOut); }),
            ("reacquire success requires assigned enemy signal", f => { f.Observer.Value=new(true,100,true,true,false,false,false,true,"match"); f.Ai.Evaluate(); f.Ai.TryGetStatus(f.Binding().Bot,out var s); Check(s.Reacquire==ReacquirePhase.Succeeded); }),
            ("foreign visible enemy is not reacquire success", f => { f.Observer.Value=new(true,100,false,true,true,true,false,true,"foreign"); f.Core.Clock.Advance(3); f.Ai.Evaluate(); f.Ai.TryGetStatus(f.Binding().Bot,out var s); Check(s.Reacquire==ReacquirePhase.TimedOut); }),
            ("cooldown limits observation windows", f => { f.Core.Clock.Advance(4); f.Ai.Evaluate(); Check(!f.Ai.RequestReacquire(f.Binding(),ReacquireReason.Diagnostic,out var r)&&r=="cooldown"); }),
            ("automatic lost perception reacquire is bounded", f => { f.Core.Clock.Advance(3); f.Ai.Evaluate(); f.Core.Clock.Advance(7); f.Ai.Evaluate(); f.Ai.TryGetStatus(f.Binding().Bot,out var s); Check(s.Reacquire==ReacquirePhase.Observing&&s.ReacquireReason==ReacquireReason.PerceptionLost); }),
            ("invalid reacquire reason rejected", f => Check(!f.Ai.RequestReacquire(f.Binding(),(ReacquireReason)999,out _))),
            ("stale rebind request rejected", f => { var b=f.Binding(); f.Human(3); f.Ai.Evaluate(); var changed=f.Bindings().First(n=>n.Target.Slot==3); var old=new BotTargetBinding(b.ModuleLifetime,changed.Bot,b.Target,b.BindingVersion); Check(!f.Ai.RequestReacquire(old,ReacquireReason.RecoveryTeleport,out _)); }),
            ("unchanged delayed binding accepts", f => { bool? result=null; Check(f.Ai.ScheduleProbe(f.Binding(),1,(ok,_)=>result=ok)); f.Core.Clock.Advance(1); Check(result==true&&f.Ai.PendingWork==0); }),
            ("delayed target disconnect rejects at execution", f => { bool? result=null; Check(f.Ai.ScheduleProbe(f.Binding(),1,(ok,_)=>result=ok)); f.Core.Registry.Disconnect(2); f.Core.Clock.Advance(1); Check(result==false); }),
            ("rebound target cancels old queued work", f => { var b=f.Bindings().Last(); bool? result=null; Check(f.Ai.ScheduleProbe(b,5,(ok,_)=>result=ok)); var timer=f.Core.Clock.Last!; f.Human(3); f.Ai.Evaluate(); timer.ForceFire(); Check(result==false); }),
            ("probe rejects role/permission changes", f => { bool? result=null; Check(f.Ai.ScheduleProbe(f.Binding(),1,(ok,_)=>result=ok)); f.Core.Source.Players[2]=f.Core.Source.Players[2] with {Role=PlayerRole.Other}; f.Core.Clock.Advance(1); Check(result==false); }),
            ("module unload cancels already dispatched work", f => { bool? result=null; Check(f.Ai.ScheduleProbe(f.Binding(),1,(ok,_)=>result=ok)); var timer=f.Core.Clock.Last!; f.Ai.Dispose(); timer.ForceFire(); Check(result==false); }),
            ("probe cap enforced", f => { for(var i=0;i<16;i++) Check(f.Ai.ScheduleProbe(f.Binding(),30,(_,_)=>{})); Check(!f.Ai.ScheduleProbe(f.Binding(),30,(_,_)=>{})); }),
            ("bounded recorder and details", f => { for(var i=0;i<10000;i++)f.Ai.Recorder.Add(i,10,1,"test",new string('x',300)); Check(f.Ai.Recorder.Count==256&&f.Ai.Recorder.Dropped>9700); Check(f.Ai.Recorder.Read(1000).Count==64&&f.Ai.Recorder.Read().All(e=>e.Detail.Length<=160)); }),
            ("no Core quota team or respawn interference", f => { var commands=f.Core.Game.Commands.Count; var moves=f.Core.Game.Moves.Count; for(var i=0;i<100;i++)f.Ai.Evaluate(); Check(commands==f.Core.Game.Commands.Count&&moves==f.Core.Game.Moves.Count); }),
            ("duplicate target provider refused", f => { BotAiRuntime.Publish(f.Ai); try { try { BotAiRuntime.Publish(f.Ai); throw new Exception("accepted duplicate"); } catch(InvalidOperationException){} } finally {BotAiRuntime.Withdraw(f.Ai);} }),
            ("native writer surface absent", _ => SourceBoundary())
        };
        for(var humans=1;humans<=6;humans++)
        {
            var count=humans;
            cases.Add(($"distribution 18 bots / {count} humans",f=>{ for(var slot=3;slot<count+2;slot++)f.Human(slot); for(var slot=20;slot<28;slot++)f.Bot(slot); f.Ai.Evaluate(); f.Balanced(count); Check(f.Ai.BindingCount==18); }));
        }
        var passed=0;
        foreach(var (name,test) in cases)
        {
            try { using var f=new BotAiFixture(); test(f); Check(f.Core.Errors.Count==0); passed++; Console.WriteLine("PASS BotAI "+name); }
            catch(Exception e) {Console.Error.WriteLine("FAIL BotAI "+name+": "+e.Message);}
        }
        return (passed,cases.Count);
    }
    static void Check(bool condition) { if(!condition)throw new InvalidOperationException("BotAI assertion failed"); }
    static void SourceBoundary()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!File.Exists(Path.Combine(root.FullName,"MIGRATION_AUTHORITY.md")))root=root.Parent;
        Check(root is not null);
        var plugin=File.ReadAllText(Path.Combine(root!.FullName,"src/ZEPVE.BotAI/BotAiPlugin.cs"));
        Check(!plugin.Contains("=> Server.CurrentTime")&&!plugin.Contains("Server.CurrentTime <")&&plugin.Contains("Stopwatch.GetElapsedTime"));
        foreach(var file in Directory.GetFiles(Path.Combine(root!.FullName,"src/ZEPVE.BotAI"),"*.cs"))
        {
            var code=File.ReadAllText(file);
            foreach(var forbidden in new[]{"Server.ExecuteCommand(",".Teleport(",".Respawn(",".ChangeTeam(",".SwitchTeam(","PlayerButtons.","BotControllerApi", "Schema.Set", "Enemy.Raw =", "IsEnemyVisible =", "AllowActive ="})
                Check(!code.Contains(forbidden));
        }
    }
}

sealed class BotAiFixture : IDisposable
{
    public AuthorityFixture Core {get;}=new();
    public ICoreLifecycle? Provider;
    public FakeCombatObserver Observer {get;}=new();
    public BotAiService Ai {get;}
    public BotAiFixture()
    {
        Provider=Core.Api; for(var i=10;i<20;i++)Bot(i);
        Core.BeginRound(); Core.Clock.Advance(18);
        Ai=new(()=>Provider,Observer,()=>Core.Clock.Now); Ai.Evaluate();
    }
    public void Human(int slot){Core.Source.Players[slot]=new(slot,(uint)(100+slot),slot,(uint)(200+slot),PlayerRole.Human,true,PlayerTeam.CounterTerrorist,false,false);Core.Registry.Observe(slot);}
    public void Bot(int slot){Core.Source.Players[slot]=new(slot,(uint)(100+slot),slot,(uint)(200+slot),PlayerRole.ZombieBot,true,PlayerTeam.Terrorist,true,false);Core.Registry.Observe(slot);}
    public BotTargetBinding Binding()=>Bindings().First(b=>b.Bot.Slot==10);
    public BotTargetBinding[] Bindings()
    {
        var result=new List<BotTargetBinding>();
        for(var slot=0;slot<64;slot++)if(Core.Api.TryCapture(slot,out var token)&&Ai.TryGetAssignedTarget(token,out var b))result.Add(b);
        return result.ToArray();
    }
    public void Rebind(BotTargetBinding old)
    {
        if(Ai.ValidateBinding(old,out _))throw new Exception("old binding revived");
        Ai.Evaluate(); if(Binding().BindingVersion<=old.BindingVersion)throw new Exception("no new version");
    }
    public void Balanced(int humans)
    {
        var bindings=Bindings(); var counts=new int[humans];
        for(var i=0;i<humans;i++)counts[i]=bindings.Count(b=>b.Target.Slot==i+2);
        if(counts.Max()-counts.Min()>1)throw new Exception("distribution unbalanced: "+string.Join(',',counts));
    }
    public void Dispose(){Ai.Dispose();Core.Dispose();}
}
sealed class FakeCombatObserver : ICombatObserver
{
    public CombatObservation Value=new(true,null,false,false,false,false,false,true,"model");
    public CombatObservation Read(BotTargetBinding binding)=>Value;
}
