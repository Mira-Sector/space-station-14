using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Surgery.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class LimbRequirement : SurgeryEdgeRequirement
{
    private static readonly SpriteSpecifier.Rsi Icon = new(new("/Textures/Interface/surgery_icons.rsi"), "limb");

    public override string Name(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart)
    {
        return Loc.GetString("surgery-requirement-limb-name");
    }

    public override string Description(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart)
    {
        return Loc.GetString("surgery-requirement-limb-desc", ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
    }

    public override SpriteSpecifier? GetIcon(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart)
    {
        return Icon;
    }

    public override SurgeryInteractionState RequirementMet(EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart? bodyPart, out Enum? ui, bool test = false)
    {
        ui = null;

        var entMan = IoCManager.Resolve<IEntityManager>();

        if (limb != null)
        {
            if (!test)
            {
                var handSys = entMan.System<SharedHandsSystem>();
                handSys.PickupOrDrop(user, limb.Value);
            }
            return SurgeryInteractionState.Passed;
        }

        if (tool is not { } used || body == null)
            return SurgeryInteractionState.Failed;

        var containerSys = entMan.System<SharedContainerSystem>();
        var bodySys = entMan.System<SharedBodySystem>();

        if (!entMan.TryGetComponent<BodyPartComponent>(used, out var bodyPartComp))
            return SurgeryInteractionState.Failed;

        foreach (var (_, container) in bodySys.GetBodyContainers(body.Value))
        {
            // must be empty
            if (container.ContainedEntities.Count > 0)
                continue;

            if (bodyPart?.Type != bodyPartComp.PartType || bodyPart.Side != bodyPartComp.Symmetry)
                continue;

            if (test || containerSys.Insert(used, container))
                return SurgeryInteractionState.Passed;
        }

        return SurgeryInteractionState.Failed;
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
