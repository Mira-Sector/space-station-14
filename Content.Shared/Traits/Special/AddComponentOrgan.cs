using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.Traits.Special;

[UsedImplicitly]
public sealed partial class AddComponentOrgan : TraitSpecial
{
    /// <summary>
    /// If set add it to the the organ if it exists
    /// If unset picks a random one
    /// </summary>
    [DataField]
    public ProtoId<OrganPrototype>? Organ;

    [DataField(required: true)]
    public ComponentRegistry Components;

    public override void TraitAdded(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<EntityManager>();
        var bodySys = entMan.System<SharedBodySystem>();

        var organs = bodySys.GetBodyOrgans(mob).ToDictionary();

        if (!organs.Any())
            return;

        if (Organ == null)
        {
            var random = IoCManager.Resolve<IRobustRandom>();

            var organ = random.Pick(organs.Keys);
            entMan.AddComponents(organ, Components);
            return;
        }

        foreach (var (organUid, organComp) in organs)
        {
            if (organComp.OrganType != Organ.Value)
                continue;

            entMan.AddComponents(organUid, Components);
            // may have multiple of the same type so dont exit early
        }
    }
}
