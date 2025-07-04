using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Gravity;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Shared.Damage.DamageSelector;

public sealed class DamagePartAccuracySystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamagePartAccuracyComponent, ProjectileShooterHitAttemptEvent>(OnProjectileHit);
        SubscribeLocalEvent<DamagePartAccuracyComponent, HitScanShooterHitAttemptEvent>(OnHitscanHit);
    }

    private void OnProjectileHit(EntityUid uid, DamagePartAccuracyComponent component, ref ProjectileShooterHitAttemptEvent args)
    {
        args.Cancelled = Prob(uid, args.Target, component);
    }

    private void OnHitscanHit(EntityUid uid, DamagePartAccuracyComponent component, ref HitScanShooterHitAttemptEvent args)
    {
        args.Cancelled = Prob(uid, args.Target, component);
    }

    private bool Prob(EntityUid shooter, EntityUid target, DamagePartAccuracyComponent component)
    {
        if (!TryComp<BodyComponent>(target, out var bodyComp))
            return false;

        if (!TryComp<DamagePartSelectorComponent>(shooter, out var selectorComp))
            return false;

        var parts = _body.GetBodyChildren(target, bodyComp);

        foreach (var (_, partComp) in parts)
        {
            if (partComp.PartType != selectorComp.SelectedPart.Type)
                continue;

            if (partComp.Symmetry != selectorComp.SelectedPart.Side)
                continue;

            if (partComp.MissProb == 1f)
                return true;

            if (partComp.MissProb == 0f)
                return false;

            return !_random.Prob(GetProb(shooter, partComp.MissProb, component));
        }

        return false;
    }


    private float GetProb(EntityUid shooter, float partProb, DamagePartAccuracyComponent component)
    {
        if (_gravity.IsWeightless(shooter))
            return component.MinProb;

        if (!TryComp<PhysicsComponent>(shooter, out var shooterPhysics))
            return component.MaxProb;

        var prob = MathHelper.Lerp(component.MinProb, component.MaxProb,
        partProb * (1 - Math.Clamp((shooterPhysics.LinearVelocity.Length() - component.MaxProbVelocity) / (component.MinProbVelocity - component.MaxProbVelocity), 0, 1)));

        Log.Debug(prob.ToString());
        return prob;
    }
}
