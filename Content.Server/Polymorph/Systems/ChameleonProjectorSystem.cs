using Content.Server.Footprints.Components;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;

namespace Content.Server.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonDisguiseComponent, ChameleonDisguisedEvent>(OnDisguise);
    }

    private void OnDisguise(EntityUid uid, ChameleonDisguiseComponent component, ChameleonDisguisedEvent args)
    {
        if (TryComp<LeavesFootprintsComponent>(args.Disguise, out var entFootprintComp))
        {
            var disguiseFootprintComp = EnsureComp<LeavesFootprintsComponent>(uid);
            disguiseFootprintComp.MaxFootsteps = entFootprintComp.MaxFootsteps;
            disguiseFootprintComp.Distance = entFootprintComp.Distance;
            disguiseFootprintComp.FootprintPrototype = entFootprintComp.FootprintPrototype;
            disguiseFootprintComp.FootprintPrototypeAlternative = entFootprintComp.FootprintPrototypeAlternative;
        }
    }
}
