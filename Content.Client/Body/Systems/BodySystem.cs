using Content.Client.Alerts;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Client.Body.Systems;

public sealed class BodySystem : SharedBodySystem
{
    private const MobState DeadState = MobState.Dead;
    private const int SegmentCount = 7;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(EntityUid uid, BodyComponent component, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != component.Alert)
            return;

        if (!TryComp<MobThresholdsComponent>(uid, out var thresholdsComp))
            return;

        FixedPoint2? deadThreshold = null;

        foreach ((var threshold, var state) in thresholdsComp.Thresholds)
        {
            if (state != DeadState)
                continue;

            deadThreshold = threshold;
            break;
        }

        if (deadThreshold == null)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var parts = GetBodyChildren(uid, component);

        foreach ((var currentPart, var currentPartComp) in parts)
        {
            if (!TryComp<DamageableComponent>(currentPart, out var damageableComp))
                continue;

            var layer = BodyPartToLayer(currentPartComp.PartType, currentPartComp.Symmetry);

            if (layer == BodyPartLayer.None)
                continue;

            float offset;

            if (damageableComp.TotalDamage >= deadThreshold)
            {
                offset = SegmentCount;
            }
            else
            {
                // 1 indexed
                // we programming in lua or some shit??
                var percentage = (float) (damageableComp.TotalDamage / deadThreshold);
                offset = (SegmentCount * percentage);

                if (offset < 0)
                    offset = 1;
                else if (offset > SegmentCount - 1 && offset < SegmentCount)
                    offset = SegmentCount - 1; // reserve highest for dead only
                else
                    offset = (uint) Math.Ceiling(offset) + 1;
            }

            var state = $"{layer.ToString()}{offset}";
            sprite.LayerSetState(layer, state);
        }
    }
}
