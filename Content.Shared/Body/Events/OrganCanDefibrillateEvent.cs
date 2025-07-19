namespace Content.Shared.Body.Events;

[ByRefEvent]
public sealed partial class OrganCanDefibrillateEvent(EntityUid body) : CancellableEntityEventArgs
{
    public readonly EntityUid Body = body;
    public LocId? Reason;
}
