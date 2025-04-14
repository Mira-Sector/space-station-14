using Robust.Shared.Serialization;

namespace Content.Shared.Intents;

[Serializable, NetSerializable]
public enum Intent
{
    Help,
    Disarm,
    Grab,
    Harm
}
