using Content.Server.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.Fluids.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Fluids.EntitySystems;

public sealed class ColorOnBloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColorOnBloodstreamComponent, DestructableSpawnedEvent>(OnSpawned);
    }

    private void OnSpawned(EntityUid uid, ColorOnBloodstreamComponent component, DestructableSpawnedEvent args)
    {
        if (!TryComp<BloodstreamComponent>(args.Owner, out var bloodstreamComp))
            return;

        if (!_prototypeManager.TryIndex(bloodstreamComp.BloodReagent, out var reagent))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearanceComp))
            return;

        _appearance.SetData(uid, BloodColor.Color, reagent.SubstanceColor, appearanceComp);
    }
}
