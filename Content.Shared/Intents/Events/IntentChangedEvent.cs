using Robust.Shared.Prototypes;

namespace Content.Shared.Intents.Events;

public sealed partial class IntentChangedEvent : EntityEventArgs
{
    public ProtoId<IntentPrototype> SelectedIntent;

    public ProtoId<IntentPrototype> OldIntent;

    public IntentChangedEvent(ProtoId<IntentPrototype> selectedIntent, ProtoId<IntentPrototype> oldIntent)
    {
        SelectedIntent = selectedIntent;
        OldIntent = oldIntent;
    }
}
