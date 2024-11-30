using Content.Server.Footprints.Components;
using Content.Server.Footprint.Systems;
using Content.Server.Polymorph.Components;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;

namespace Content.Server.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly FootprintSystem _footprint = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonDisguisedComponent, ChameleonProjectorDisguisedEvent>(OnDisguise);
        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentRemove>(OnRemove);
    }

    private void OnDisguise(EntityUid uid, ChameleonDisguisedComponent component, ref ChameleonProjectorDisguisedEvent args)
    {
        var messMaker = _footprint.GetMessMaker(args.Source);

        if (!TryComp<LeavesFootprintsComponent>(messMaker, out var messMakerFootprintComp))
            return;

        if (TryComp<LeavesFootprintsComponent>(uid, out var footprintComp))
        {
            component.FootprintMaxFootsteps = footprintComp.MaxFootsteps;
            component.FootprintDistance = footprintComp.Distance;
            component.FootprintPrototype = footprintComp.FootprintPrototype;
            component.FootprintPrototypeAlternative = footprintComp.FootprintPrototypeAlternative;
            Dirty(uid, component);
        }

        footprintComp = EnsureComp<LeavesFootprintsComponent>(uid);

        footprintComp.MaxFootsteps = messMakerFootprintComp.MaxFootsteps;
        footprintComp.Distance = messMakerFootprintComp.Distance;
        footprintComp.FootprintPrototype = messMakerFootprintComp.FootprintPrototype;
        footprintComp.FootprintPrototypeAlternative = messMakerFootprintComp.FootprintPrototypeAlternative;
    }

    private void OnRemove(EntityUid uid, ChameleonDisguisedComponent component, ComponentRemove args)
    {
        RemComp<LeavesFootprintsComponent>(uid);

        if (component.FootprintDistance != null && component.FootprintPrototype != null && component.FootprintPrototypeAlternative != null)
        {
            var footprintComp = EnsureComp<LeavesFootprintsComponent>(uid);
            footprintComp.MaxFootsteps = component.FootprintMaxFootsteps;
            footprintComp.Distance = component.FootprintDistance.Value;
            footprintComp.FootprintPrototype = component.FootprintPrototype;
            footprintComp.FootprintPrototypeAlternative = component.FootprintPrototypeAlternative;
            Dirty(uid, footprintComp);
        }
    }
}
