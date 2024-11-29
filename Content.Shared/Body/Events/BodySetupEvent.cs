using Content.Shared.Body.Components;

namespace Content.Shared.Body.Events;

public sealed class BodySetupEvent : EntityEventArgs
{
    public BodyComponent Body;

    public BodySetupEvent(BodyComponent body)
    {
        Body = body;
    }
}
