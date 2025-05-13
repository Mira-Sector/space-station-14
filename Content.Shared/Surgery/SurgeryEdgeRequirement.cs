using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class SurgeryEdgeRequirement
{
    public abstract SurgeryEdgeState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui);

    public abstract bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge ,EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId);

    public abstract bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged);
}

[Serializable, NetSerializable]
public enum SurgeryEdgeState
{
    Failed, //cant be run
    Passed, //passed and something has been done
    DoAfter, //passed and we are waiting for a doafter to finish
    UserInterface //passed and we are waiting the ui to do something
}

[Serializable, NetSerializable]
public sealed partial class SurgeryDoAfterEvent : DoAfterEvent
{
    public SurgeryEdge TargetEdge { get; private set; }

    public BodyPart BodyPart { get; private set; }

    public SurgeryDoAfterEvent(SurgeryEdge targetEdge, BodyPart bodyPart)
    {
        TargetEdge = targetEdge;
        BodyPart = bodyPart;
    }

    public override DoAfterEvent Clone() => this;
}
