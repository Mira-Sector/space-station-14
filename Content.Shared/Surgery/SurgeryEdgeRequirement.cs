using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
[Serializable, NetSerializable]
public abstract partial class SurgeryEdgeRequirement
{
    public abstract SurgeryEdgeState RequirementMet(EntityUid body, EntityUid limb, EntityUid user, EntityUid? tool);

    public abstract bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge ,EntityUid body, EntityUid limb, EntityUid user, EntityUid? tool, [NotNullWhen(true)] out DoAfterId? doAfterId);

    public abstract bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged);
}

[Serializable, NetSerializable]
public enum SurgeryEdgeState
{
    Failed,
    Passed,
    DoAfter
}

[Serializable, NetSerializable]
public partial class SurgeryDoAfterEvent : DoAfterEvent
{
    public SurgeryEdge TargetEdge { get; private set; }

    public SurgeryDoAfterEvent(SurgeryEdge targetEdge)
    {
        TargetEdge = targetEdge;
    }

    public override DoAfterEvent Clone() => this;
}
