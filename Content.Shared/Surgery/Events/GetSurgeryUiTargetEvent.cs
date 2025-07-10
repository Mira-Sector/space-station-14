namespace Content.Shared.Surgery.Events;

[ByRefEvent]
public record struct GetSurgeryUiTarget
{
    public EntityUid? Target;
}
