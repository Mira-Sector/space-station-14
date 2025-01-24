using Content.Shared.DoAfter;
using Content.Shared.Surgery.Systems;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class ToolRequirement : SurgeryEdgeRequirement
{
    [DataField]
    public List<string> Qualities = new();

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1f);

    public override SurgeryEdgeState RequirementMet(EntityUid body, EntityUid limb, EntityUid user, EntityUid? tool)
    {
        if (tool == null)
            return SurgeryEdgeState.Failed;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var toolSystem = entMan.System<SharedToolSystem>();

        if (toolSystem.HasAllQualities(tool.Value, Qualities))
            return SurgeryEdgeState.DoAfter;

        return SurgeryEdgeState.Failed;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid body, EntityUid limb, EntityUid user, EntityUid? tool, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;

        if (tool == null)
            return false;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var toolSystem = entMan.System<SharedToolSystem>();

        var surgerySystem = entMan.System<SurgerySystem>();

        return toolSystem.UseTool(tool.Value, user, limb, Delay, Qualities, new SurgeryDoAfterEvent(targetEdge), out doAfterId);
    }
}
