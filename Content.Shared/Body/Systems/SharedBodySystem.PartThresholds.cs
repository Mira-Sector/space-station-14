using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

    private void InitializePartThresholds()
    {
        SubscribeLocalEvent<BodyPartThresholdsComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<BodyPartThresholdsComponent, BeforeDamageChangedEvent>(OnBeforeDamaged);
    }

    private void OnBeforeDamaged(EntityUid uid, BodyPartThresholdsComponent component, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!args.Damage.AnyPositive() || component.CurrentState != WoundState.Dead)
            return;

        args.Cancelled = true;

        if (!TryComp<BodyPartComponent>(uid, out var partComp) || partComp.Body == null)
            return;

        if (args.Origin != null)
            _color.RaiseEffect(Color.BetterViolet, new List<EntityUid>() { partComp.Body.Value }, Filter.Pvs(partComp.Body.Value, entityManager: EntityManager));
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
        WoundState? highestPossibleState = null;
        FixedPoint2 highestPossibleThreshold = new();

        foreach (var (limbState, limbThreshold) in thresholds.Thresholds)
        {
            if (damage.TotalDamage < limbThreshold)
                continue;

            if (limbThreshold < highestPossibleThreshold)
                continue;

            highestPossibleState = limbState;
            highestPossibleThreshold = limbThreshold;
        }

        if (highestPossibleState == null)
            return;

        if (highestPossibleState.Value == thresholds.CurrentState)
            return;

        DoThreshold(limb, body, thresholds, highestPossibleState.Value);
    }

    internal void DoThreshold(EntityUid limb, EntityUid body, BodyPartThresholdsComponent thresholds, WoundState state)
    {
        var ev = new LimbStateChangedEvent(body, thresholds.CurrentState, state);
        RaiseLocalEvent(limb, ev);

        // TODO: do something on state change

        thresholds.CurrentState = state;
        Dirty(limb, thresholds);
    }
}
