using Robust.Shared.Serialization;

namespace Content.Shared.Intents;

[Serializable, NetSerializable]
public sealed class IntentChangeMessage : BoundUserInterfaceMessage
{
    public Intent Intent;

    public IntentChangeMessage(Intent intent)
    {
        Intent = intent;
    }
}
