using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Robust.Shared.Player;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

    private void InitializePartThresholds()
    {
        SubscribeLocalEvent<BodyPartThresholdsComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<BodyPartThresholdsComponent, BeforeDamageChangedEvent>(OnBeforeDamaged);
    }

    private void OnBeforeDamaged(Entity<BodyPartThresholdsComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!args.Damage.AnyPositive())
            return;

        if (ent.Comp.CurrentState == WoundState.Dead)
        {
            args.Cancelled = true;

            if (args.Origin == null)
                return;

            TryComp<BodyPartComponent>(ent, out var partComp);

            var flashUid = partComp?.Body ?? ent;
            _color.RaiseEffect(Color.BetterViolet, new List<EntityUid>() { flashUid }, Filter.Pvs(flashUid, entityManager: EntityManager));

            return;
        }

        if (!ent.Comp.Thresholds.TryGetValue(WoundState.Dead, out var deadThreshold))
            return;

        var damageable = EntityManager.GetComponent<DamageableComponent>(ent);

        var delta = deadThreshold - damageable.Damage.GetTotal();

        if (delta > args.Damage.GetTotal())
            return;

        // cap it
        // we take away from every damage group that deals damage that deals damage equally
        Dictionary<string, FixedPoint2> damagingGroups = new();

        foreach (var (group, damage) in args.Damage.DamageDict)
        {
            if (damage <= FixedPoint2.Zero)
                continue;

            damagingGroups.Add(group, damage);
        }

        var damageToRemovePerGroup = (args.Damage.GetTotal() - delta) / damagingGroups.Count;

        foreach (var (group, damage) in damagingGroups)
            args.Damage.DamageDict[group] -= damageToRemovePerGroup;
    }

    private void OnDamaged(Entity<BodyPartThresholdsComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp<BodyPartComponent>(ent, out var partComp) || partComp.Body is not {} body)
            return;

        if (!TryComp<BodyComponent>(body, out var bodyComp))
            return;

        CheckThresholds((ent.Owner, ent.Comp, args.Damageable), body);
        _alerts.ShowAlert(body, bodyComp.Alert);
    }

    internal void CheckThresholds(Entity<BodyPartThresholdsComponent, DamageableComponent> limb, EntityUid body)
    {
        WoundState? highestPossibleState = null;
        FixedPoint2 highestPossibleThreshold = new();

        foreach (var (limbState, limbThreshold) in limb.Comp1.Thresholds)
        {
            if (limb.Comp2.TotalDamage < limbThreshold)
                continue;

            if (limbThreshold < highestPossibleThreshold)
                continue;

            highestPossibleState = limbState;
            highestPossibleThreshold = limbThreshold;
        }

        if (highestPossibleState == null)
            return;

        if (highestPossibleState.Value == limb.Comp1.CurrentState)
            return;

        DoThreshold(limb, body, highestPossibleState.Value);
    }

    internal void DoThreshold(Entity<BodyPartThresholdsComponent> limb, EntityUid body, WoundState state)
    {
        var ev = new LimbStateChangedEvent(body, limb.Comp.CurrentState, state);
        RaiseLocalEvent(limb, ev);

        // TODO: do something on state change

        limb.Comp.CurrentState = state;
        Dirty(limb);
    }
}
