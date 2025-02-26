using Content.Shared.Body.Part;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class AddOrgan : SurgerySpecial
{
    public override void NodeReached(EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (used == null || limb == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var containerSys = entMan.System<SharedContainerSystem>();
        var bodySys = entMan.System<SharedBodySystem>();

        if (!entMan.TryGetComponent<OrganComponent>(used, out var organComp))
            return;

        if (!entMan.TryGetComponent<BodyPartComponent>(limb, out var partComp))
            return;

        foreach (var (containerId, organSlot) in partComp.Organs)
        {
            if (organSlot.OrganType != organComp.OrganType)
                continue;

            if (!containerSys.TryGetContainer(limb.Value, SharedBodySystem.GetOrganContainerId(containerId), out var container))
                continue;

            if (container.ContainedEntities.Count >= 1)
                continue;

            containerSys.InsertOrDrop(used.Value, container);
            break;
        }
    }

    public override void NodeLeft(EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
    }
}
