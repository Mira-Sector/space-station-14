namespace Content.Shared.Surgery.Events;

[ByRefEvent]
public record struct GetSurgeryUiSourceEvent
{
    public EntityUid? Source;
}
