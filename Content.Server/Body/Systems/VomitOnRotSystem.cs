using Content.Server.Body.Components;
using Content.Server.Medical;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Robust.Shared.Random;

namespace Content.Server.Body.Systems;

public sealed class VomitOnRotSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VomitOnRotComponent, RotUpdateEvent>(OnRotUpdate);
    }

    private void OnRotUpdate(Entity<VomitOnRotComponent> ent, ref RotUpdateEvent args)
    {
        ent.Comp.CurrentChance = ent.Comp.HealthyChance + args.RotProgress * (ent.Comp.DamagedChance - ent.Comp.HealthyChance);
    }

    private void OnDigested(Entity<VomitOnRotComponent> ent, ref StomachDigestedEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organComp))
            return;

        if (!TryComp<StomachComponent>(ent, out var stomachComp))
            return;

        if (_random.Prob(ent.Comp.CurrentChance))
            _vomit.VomitOrgan((ent, organComp, stomachComp));
    }
}
