using Content.Server.Body.Damage.Components;
using Content.Server.Medical;
using Content.Shared.Body.Damage.Events;
using Content.Shared.Body.Damage.Systems;
using Robust.Shared.Random;

namespace Content.Server.Body.Damage.Systems;

public sealed partial class VomitOnBodyDamageSystem : BaseOnBodyDamageSystem<VomitOnBodyDamageComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VomitOnBodyDamageComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VomitOnBodyDamageComponent, BodyDamageChangedEvent>(OnBodyDamage, after: [typeof(BodyDamageThresholdsSystem)]);
        SubscribeLocalEvent<VomitOnBodyDamageComponent, StomachDigestedEvent>(OnDigested);
    }

    private void OnInit(Entity<VomitOnBodyDamageComponent> ent, ref ComponentInit args)
    {
        if (CanDoEffect(ent))
            ent.Comp.CurrentProb = ent.Comp.MinProb;
        else
            ent.Comp.CurrentProb = 0f;
    }

    private void OnBodyDamage(Entity<VomitOnBodyDamageComponent> ent, ref BodyDamageChangedEvent args)
    {
        if (!CanDoEffect(ent))
        {
            ent.Comp.CurrentProb = 0f;
            return;
        }

        if (ent.Comp.ScaleProbToDamage != null)
        {
            var probDelta = ent.Comp.MaxProb - ent.Comp.MinProb;
            var damageDelta = args.NewDamage - ent.Comp.MinDamage;
            var percentage = (float)(damageDelta / ent.Comp.ScaleProbToDamage.Value);
            var prob = probDelta * percentage + ent.Comp.MinProb;
            ent.Comp.CurrentProb = prob;
        }

        if (ent.Comp.TriggeredOnDigestion)
            return;

        if (!_random.Prob(ent.Comp.CurrentProb))
            return;

        _vomit.VomitOrgan(ent.Owner);
    }

    private void OnDigested(Entity<VomitOnBodyDamageComponent> ent, ref StomachDigestedEvent args)
    {
        if (!ent.Comp.TriggeredOnDigestion)
            return;

        if (!CanDoEffect(ent))
            return;

        if (!_random.Prob(ent.Comp.CurrentProb))
            return;

        _vomit.VomitOrgan(ent.Owner);
    }
}
