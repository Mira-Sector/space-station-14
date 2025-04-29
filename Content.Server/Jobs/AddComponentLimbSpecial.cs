using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Jobs;

public sealed partial class AddComponentLimbSpecial : JobSpecial
{
    /// <summary>
    /// If set add it to the the body part if it exists
    /// If unset picks a random one
    /// </summary>
    [DataField]
    public BodyPart? BodyPart;

    [DataField(required: true)]
    public ComponentRegistry Components;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<EntityManager>();
        var bodySys = entMan.System<SharedBodySystem>();

        var parts = bodySys.GetBodyChildren(mob).ToDictionary();

        if (!parts.Any())
            return;

        if (BodyPart == null)
        {
            var random = IoCManager.Resolve<IRobustRandom>();

            var part = random.Pick(parts.Keys);
            entMan.AddComponents(part, Components);
            return;
        }

        foreach (var (partUid, partComp) in parts)
        {
            var bodyPart = new BodyPart(partComp.PartType, partComp.Symmetry);
            if (bodyPart != BodyPart)
                continue;

            entMan.AddComponents(partUid, Components);
            // may have multiple of the same type so dont exit early
        }
    }
}
