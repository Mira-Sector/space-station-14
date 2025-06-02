using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Intents.Events;

[Serializable, NetSerializable]
public sealed class IntentChangeMessage : BoundUserInterfaceMessage
{
    public ProtoId<IntentPrototype> Intent;

    public IntentChangeMessage(ProtoId<IntentPrototype> intent)
    {
        Intent = intent;
    }
}
