using Content.Server.Body.Components;
using Content.Server.Ghost;
using Content.Server.Humanoid;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using System.Numerics;
using Content.Shared.Damage.Components;

namespace Content.Server.Body.Systems;

public sealed partial class BodySystem : SharedBodySystem
{
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    private const float LimbMultiplier = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRelays();

        SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnRelayMoveInput);
        SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BodyComponent, BodyInitEvent>(OnBodyInit);
    }

    private void OnRelayMoveInput(Entity<BodyComponent> ent, ref MoveInputEvent args)
    {
        // If they haven't actually moved then ignore it.
        if ((args.Entity.Comp.HeldMoveButtons &
             (MoveButtons.Down | MoveButtons.Left | MoveButtons.Up | MoveButtons.Right)) == 0x0)
        {
            return;
        }

        if (_mobState.IsDead(ent) && _mindSystem.TryGetMind(ent, out var mindId, out var mind))
        {
            mind.TimeOfDeath ??= _gameTiming.RealTime;
            _ghostSystem.OnGhostAttempt(mindId, canReturnGlobal: true, mind: mind);
        }
    }

    private void OnApplyMetabolicMultiplier(
        Entity<BodyComponent> ent,
        ref ApplyMetabolicMultiplierEvent args)
    {
        foreach (var organ in GetBodyOrgans(ent, ent))
        {
            RaiseLocalEvent(organ.Id, ref args);
        }
    }

    private void OnBodyInit(EntityUid uid, BodyComponent component, ref BodyInitEvent args)
    {
        if (TryComp<MobThresholdsComponent>(uid, out var thresholds))
        {
            var damage = _mobThreshold.GetThresholdForState(uid, MobState.Dead, thresholds);

            if (damage != FixedPoint2.Zero)
            {
                EnsureBodyThreshold(uid, component, damage);
                return;
            }
        }
    }

    public void EnsureBodyThreshold(EntityUid uid, BodyComponent body, FixedPoint2 threshold)
    {
        var parts = GetBodyDamageable(uid, body);
        Dictionary<EntityUid, (FixedPoint2 MaxDamage, float Scale)> deadThresholds = new();
        HashSet<EntityUid> vitalLimbs = new();
        var totalThreshold = FixedPoint2.Zero;

        foreach (var (part, _) in parts)
        {
            if (!TryComp<BodyPartComponent>(part, out var partComp))
                continue;

            if (!TryComp<BodyPartThresholdsComponent>(part, out var thresholdsComp))
                continue;

            if (!thresholdsComp.Thresholds.TryGetValue(WoundState.Dead, out var deadThreshold))
                continue;

            if (partComp.IsVital)
            {
                vitalLimbs.Add(part);
            }
            else
            {
                deadThresholds.Add(part, (deadThreshold, partComp.OverallDamageScale));
            }

            totalThreshold += deadThreshold * partComp.OverallDamageScale;
        }

        foreach (var part in vitalLimbs)
        {
            if (!TryComp<BodyPartThresholdsComponent>(part, out var thresholdsComp))
                continue;

            if (!thresholdsComp.Thresholds.ContainsKey(WoundState.Dead))
                continue;

            thresholdsComp.Thresholds[WoundState.Dead] = threshold;
            Dirty(part, thresholdsComp);
        }

        if (totalThreshold >= threshold)
            return;

        var damageLeft = totalThreshold - threshold;

        foreach (var (part, (deadThreshold, scale)) in deadThresholds)
        {
            if (!TryComp<BodyPartThresholdsComponent>(part, out var thresholdsComp))
                continue;

            var weight = deadThreshold * scale;
            var newThreshold = (weight / damageLeft) * threshold / scale;

            newThreshold += deadThreshold;
            newThreshold *= LimbMultiplier;

            thresholdsComp.Thresholds[WoundState.Dead] = newThreshold;
            Dirty(part, thresholdsComp);
        }
    }

    protected override void AddPart(
        Entity<BodyComponent?> bodyEnt,
        Entity<BodyPartComponent> partEnt,
        string slotId)
    {
        // TODO: Predict this probably.
        base.AddPart(bodyEnt, partEnt, slotId);

        var layer = partEnt.Comp.ToHumanoidLayers();
        if (layer != null)
        {
            var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
            _humanoidSystem.SetLayersVisibility(bodyEnt.Owner, layers, visible: true);
        }
    }

    protected override void RemovePart(
        Entity<BodyComponent?> bodyEnt,
        Entity<BodyPartComponent> partEnt,
        string slotId)
    {
        base.RemovePart(bodyEnt, partEnt, slotId);

        if (!TryComp<HumanoidAppearanceComponent>(bodyEnt, out var humanoid))
            return;

        var layer = partEnt.Comp.ToHumanoidLayers();

        if (layer is null)
            return;

        var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
        _humanoidSystem.SetLayersVisibility((bodyEnt, humanoid), layers, visible: false);
    }

    public override HashSet<EntityUid> GibBody(
        EntityUid bodyId,
        bool gibOrgans = false,
        BodyComponent? body = null,
        bool launchGibs = true,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null
    )
    {
        if (!Resolve(bodyId, ref body, logMissing: false)
            || TerminatingOrDeleted(bodyId)
            || EntityManager.IsQueuedForDeletion(bodyId))
        {
            return new HashSet<EntityUid>();
        }

        if (HasComp<GodmodeComponent>(bodyId))
            return new HashSet<EntityUid>();

        var xform = Transform(bodyId);
        if (xform.MapUid is null)
            return new HashSet<EntityUid>();

        var gibs = base.GibBody(bodyId, gibOrgans, body, launchGibs: launchGibs,
            splatDirection: splatDirection, splatModifier: splatModifier, splatCone:splatCone);

        var ev = new BeingGibbedEvent(gibs);
        RaiseLocalEvent(bodyId, ref ev);

        QueueDel(bodyId);

        return gibs;
    }
}
