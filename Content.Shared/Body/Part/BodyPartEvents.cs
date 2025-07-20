using Content.Shared.Body.Components;

namespace Content.Shared.Body.Part;

[ByRefEvent]
public readonly record struct BodyPartAddedEvent(string Slot, Entity<BodyPartComponent> Part);

[ByRefEvent]
public readonly record struct BodyPartRemovedEvent(string Slot, Entity<BodyPartComponent> Part);

[ByRefEvent]
public readonly record struct BodyPartAddedToBodyEvent(EntityUid Body);

[ByRefEvent]
public readonly record struct BodyPartRemovedFromBodyEvent(EntityUid Body);

[ByRefEvent]
public record struct LimbBodyRelayedEvent<TEvent>(TEvent Args, EntityUid Limb)
{
    public TEvent Args = Args;
    public readonly EntityUid Limb = Limb;
}

[ByRefEvent]
public record struct BodyLimbRelayedEvent<TEvent>(TEvent Args, EntityUid Body)
{
    public TEvent Args = Args;
    public readonly EntityUid Body = Body;
}

[ByRefEvent]
public record struct BodyOrganRelayedEvent<TEvent>(TEvent Args, EntityUid Body)
{
    public TEvent Args = Args;
    public readonly EntityUid Body = Body;
}

[ByRefEvent]
public record struct LimbOrganRelayedEvent<TEvent>(TEvent Args, EntityUid Limb)
{
    public TEvent Args = Args;
    public readonly EntityUid Limb = Limb;
}

[ByRefEvent]
public record struct OrganLimbRelayedEvent<TEvent>(TEvent Args, EntityUid Organ)
{
    public TEvent Args = Args;
    public readonly EntityUid Organ = Organ;
}

[ByRefEvent]
public record struct OrganBodyRelayedEvent<TEvent>(TEvent Args, EntityUid Organ)
{
    public TEvent Args = Args;
    public readonly EntityUid Organ = Organ;
}

public sealed class LimbStateChangedEvent : EntityEventArgs
{
    public EntityUid Body;
    public WoundState OldState;
    public WoundState NewState;

    public LimbStateChangedEvent(EntityUid body, WoundState oldState, WoundState newState)
    {
        Body = body;
        OldState = oldState;
        NewState = newState;
    }
}
