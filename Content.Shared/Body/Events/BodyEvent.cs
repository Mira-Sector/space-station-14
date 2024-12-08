using Content.Shared.Body.Components;

namespace Content.Shared.Body.Events;

public sealed class BodyChangedEvent : EntityEventArgs
{
    public BodyComponent Body;

    public BodyChangedEvent(BodyComponent body)
    {
        Body = body;
    }
}

public sealed class BodyInitEvent : EntityEventArgs
{
    public BodyComponent Body;

    public BodyInitEvent(BodyComponent body)
    {
        Body = body;
    }
}
