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
