using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Events;

[Serializable, NetSerializable]
public sealed partial class SurgeryEdgeRequirementDoAfterEvent : DoAfterEvent
{
    public SurgeryEdge TargetEdge { get; private set; }

    public BodyPart? BodyPart { get; private set; }

    public SurgeryEdgeRequirementDoAfterEvent(SurgeryEdge targetEdge, BodyPart? bodyPart)
    {
        TargetEdge = targetEdge;
        BodyPart = bodyPart;
    }

    public override DoAfterEvent Clone() => this;
}
