using System.Linq;
using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.Coughing;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class CoughOnBodyDamageSystem : BaseOnBodyDamageSystem<CoughOnBodyDamageComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoughOnBodyDamageComponent, BodyDamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<CoughOnBodyDamageComponent, CoughGetChanceEvent>(OnGetCoughChance);
    }

    private void OnDamageChanged(Entity<CoughOnBodyDamageComponent> ent, ref BodyDamageChangedEvent args)
    {
        if (!TryComp<BodyDamageThresholdsComponent>(ent.Owner, out var thresholdsComp))
            return;

        if (!CanDoEffect((ent.Owner, ent.Comp, thresholdsComp)))
            return;

        var maxThreshold = thresholdsComp.Thresholds.Last().Value;
        var delta = args.NewDamage - maxThreshold;

        var chance = delta * ent.Comp.MaxChance;

        if (chance < ent.Comp.MinChance)
            chance = ent.Comp.MinChance;

        ent.Comp.CurrentChance = (float)chance;
        Dirty(ent);
    }

    private void OnGetCoughChance(Entity<CoughOnBodyDamageComponent> ent, ref CoughGetChanceEvent args)
    {
        if (args.Cancelled)
            return;

        args.Chance += ent.Comp.CurrentChance;
    }
}
