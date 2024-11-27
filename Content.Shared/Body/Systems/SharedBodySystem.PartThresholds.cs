using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializePartThresholds()
    {
        SubscribeLocalEvent<BodyPartThresholdsComponent, DamageChangedEvent>(OnDamaged);
    }

    private void OnDamaged(EntityUid uid, BodyPartThresholdsComponent component, DamageChangedEvent args)
    {
        if (!TryComp<BodyPartComponent>(uid, out var partComp) || partComp.Body is not {} body)
            return;

        if (!TryComp<BodyComponent>(body, out var bodyComp))
            return;

        CheckThresholds(uid, body, component, args.Damageable);
        _alerts.ShowAlert(body, bodyComp.Alert);
    }

    internal void CheckThresholds(EntityUid limb, EntityUid body, BodyPartThresholdsComponent thresholds, DamageableComponent damage)
    {
        foreach (var (limbThreshold, limbState) in thresholds.Thresholds)
        {
            if (limbThreshold < damage.TotalDamage)
                continue;

            if (limbState == thresholds.CurrentState)
                return;

            DoThreshold(limb, body, thresholds, limbState);
        }
    }

    internal void DoThreshold(EntityUid limb, EntityUid body, BodyPartThresholdsComponent thresholds, WoundState state)
    {
        var ev = new LimbStateChangedEvent(body, thresholds.CurrentState, state);
        RaiseLocalEvent(limb, ev);

        // TODO: do something on state change

        thresholds.CurrentState = state;
    }

    public bool TryGetLimbStateThreshold(EntityUid limb, WoundState state, out FixedPoint2 threshold, BodyPartThresholdsComponent? thresholds = null)
    {
        threshold = FixedPoint2.Zero;

        if (!Resolve(limb, ref thresholds, false))
            return false;

        foreach (var (limbThreshold, limbState) in thresholds.Thresholds)
        {
            if (limbState != state)
                continue;

            threshold = limbThreshold;
            return true;
        }

        return false;
    }
}
