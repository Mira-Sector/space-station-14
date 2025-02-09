using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class RemoveOrgan : SurgerySpecial
{
    public override void NodeReached(EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (limb == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var bodySys = entMan.System<SharedBodySystem>();
        var containerSys = entMan.System<SharedContainerSystem>();
        var handSys = entMan.System<SharedHandsSystem>();

        if (!entMan.TryGetComponent<BodyPartComponent>(limb, out var bodyPartComp))
            return;

        // do we have enoigh hands to just pick them all up?
        if (bodyPartComp.Organs.Count <= handSys.EnumerateHands(user).Where(x => x.IsEmpty).Count())
        {
            foreach (var containerId in bodyPartComp.Organs.Keys)
            {
                if (!containerSys.TryGetContainer(limb.Value, SharedBodySystem.GetOrganContainerId(containerId), out var container))
                    continue;

                foreach (var organ in container.ContainedEntities)
                {
                    if (!entMan.TryGetComponent<OrganComponent>(organ, out var organComp) || organComp.Body != body)
                        continue;

                    handSys.PickupOrDrop(user, organ);
                }
            }

            return;
        }

        // organs are stored in multiple containers so collect them
        HashSet<EntityUid> organs = new();

        foreach (var containerId in bodyPartComp.Organs.Keys)
        {
            if (!containerSys.TryGetContainer(limb.Value, SharedBodySystem.GetOrganContainerId(containerId), out var container))
                continue;

            foreach (var organ in container.ContainedEntities)
            {
                if (!entMan.TryGetComponent<OrganComponent>(organ, out var organComp) || organComp.Body != body)
                    continue;

                organs.Add(organ);
            }
        }

        foreach (var organ in organs)
        {
            // add a ui/radial menu or smth idk
        }
    }

    public override void NodeLeft(EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        // should merge the add and remove into one
        // with bool if it should act on add/removed
    }
}
