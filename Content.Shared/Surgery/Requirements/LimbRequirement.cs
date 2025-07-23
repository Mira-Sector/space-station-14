using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Surgery.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class LimbRequirement : SurgeryEdgeRequirement
{
    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-requirement-limb-desc", ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
    }

    public override SurgeryEdgeState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        var entMan = IoCManager.Resolve<IEntityManager>();

        if (limb != null)
        {
            var handSys = entMan.System<SharedHandsSystem>();
            handSys.PickupOrDrop(user, limb.Value);
            return SurgeryEdgeState.Passed;
        }

        if (tool is not { } used || body == null)
            return SurgeryEdgeState.Failed;

        var containerSys = entMan.System<SharedContainerSystem>();
        var bodySys = entMan.System<SharedBodySystem>();

        if (!entMan.TryGetComponent<BodyPartComponent>(used, out var bodyPartComp))
            return SurgeryEdgeState.Failed;

        foreach (var (_, container) in bodySys.GetBodyContainers(body.Value))
        {
            // must be empty
            if (container.ContainedEntities.Count > 0)
                continue;

            if (bodyPart.Type != bodyPartComp.PartType || bodyPart.Side != bodyPartComp.Symmetry)
                continue;

            if (containerSys.Insert(used, container))
                return SurgeryEdgeState.Passed;
        }

        return SurgeryEdgeState.Failed;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;
        return false;
    }

    public override bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged)
    {
        merged = null;

        if (other is not LimbRequirement)
            return false;

        merged = new LimbRequirement();
        return true;
    }
}
