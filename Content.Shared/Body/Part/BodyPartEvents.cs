using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Part;

[ByRefEvent]
public readonly record struct BodyPartAddedEvent(string Slot, Entity<BodyPartComponent> Part);

[ByRefEvent]
public readonly record struct BodyPartRemovedEvent(string Slot, Entity<BodyPartComponent> Part);


[ByRefEvent]
public record struct LimbBodyRelayedEvent<TEvent>(TEvent Args, EntityUid Limb)
{
    public readonly TEvent Args = Args;
    public readonly EntityUid Limb = Limb;
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
