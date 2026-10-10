namespace ZEPVE.Abstractions;

public enum NavigationDriver { Native, Trail, Recovery }
public enum NavigationProgress { Progressing, Waiting, Blocked, RouteInvalid, TargetUnavailable }
public readonly record struct NavigationStatus(PlayerLifetime Bot,BotTargetBinding Binding,
    NavigationDriver Driver,NavigationProgress Progress,long RouteVersion,long TrailGeneration,long Cursor,
    string Detail,string Recovery,double NoProgressSeconds);
public readonly record struct NavigationEvent(long Sequence,double Time,int? BotSlot,string Kind,string Detail);
public interface INavigationDiagnostics
{
    Guid ModuleLifetime {get;}
    bool TryGetStatus(int slot,out NavigationStatus status);
    IReadOnlyList<NavigationEvent> ReadEvents(int maximum=32);
}
public static class NavigationRuntime
{
    public static INavigationDiagnostics? Current {get;private set;}
    internal static void Publish(INavigationDiagnostics provider)
    {
        if(!SuiteRuntime.LegacyMovementDisabled)throw new InvalidOperationException("Legacy movement/Recovery writer is still active or the matched adapter is absent.");
        if(Current is not null)throw new InvalidOperationException("A Navigation writer is already active.");
        Current=provider;
    }
    internal static void Withdraw(INavigationDiagnostics provider){if(ReferenceEquals(Current,provider))Current=null;}
}
