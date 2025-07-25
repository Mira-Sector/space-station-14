using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class AddLimb : SurgerySpecial
{
    public override void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        base.NodeReached(body, limb, user, used, bodyPart, out ui);

        if (used == null)
            return;

        if (body == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var containerSys = entMan.System<SharedContainerSystem>();
        var bodySys = entMan.System<SharedBodySystem>();

        if (!entMan.TryGetComponent<BodyPartComponent>(used, out var bodyPartComp))
            return;

        foreach (var (_, container) in bodySys.GetBodyContainers(body.Value))
        {
            // must be empty
            if (container.ContainedEntities.Count > 0)
                continue;

            if (bodyPart.Type != bodyPartComp.PartType || bodyPart.Side != bodyPartComp.Symmetry)
                continue;

            if (containerSys.Insert(used.Value, container))
                break;
        }
    }
}
