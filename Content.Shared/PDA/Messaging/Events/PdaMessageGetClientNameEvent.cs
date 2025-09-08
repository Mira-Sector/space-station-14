namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public record struct PdaMessageGetClientNameEvent
{
    public string? Name;
}
