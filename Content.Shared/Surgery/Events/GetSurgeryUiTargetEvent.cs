namespace Content.Shared.Surgery.Events;

[ByRefEvent]
public record struct GetSurgeryUiTargetEvent
{
    public EntityUid? Target;
}
