using Robust.Shared.Serialization;

namespace Content.Shared.Coughing;

[Serializable, NetSerializable]
public sealed partial class CoughGetChangceEvent : CancellableEntityEventArgs
{
    public float Chance;

    public CoughGetChangceEvent(float chance)
    {
        Chance = chance;
    }
}
