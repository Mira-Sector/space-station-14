using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[Serializable, NetSerializable]
public enum SurgerySpecialInteractionPhase
{
    BeforeGraph,
    AfterGraph
}
