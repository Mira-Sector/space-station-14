namespace Content.Shared.Modules.Events;

[ByRefEvent]
public struct ModuleRelayedEvent<T>(T args, EntityUid moduleOwner)
{
    public T Args = args;
    public readonly EntityUid ModuleOwner = moduleOwner;
}
