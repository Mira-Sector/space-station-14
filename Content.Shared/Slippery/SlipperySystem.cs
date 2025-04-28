using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Robust.Shared.Network;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Slippery;

[UsedImplicitly]
public sealed class SlipperySystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SpeedModifierContactsSystem _speedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlipperyComponent, StepTriggerAttemptEvent>(HandleAttemptCollide);
        SubscribeLocalEvent<SlipperyComponent, StepTriggeredOffEvent>(HandleStepTrigger);
        SubscribeLocalEvent<NoSlipComponent, SlipAttemptEvent>(OnNoSlipAttempt);
        SubscribeLocalEvent<SlowedOverSlipperyComponent, SlipAttemptEvent>(OnSlowedOverSlipAttempt);
        SubscribeLocalEvent<ThrownItemComponent, SlipCausingAttemptEvent>(OnThrownSlipAttempt);
        // as long as slip-resistant mice are never added, this should be fine (otherwise a mouse-hat will transfer it's power to the wearer).
        SubscribeLocalEvent<NoSlipComponent, InventoryRelayedEvent<SlipAttemptEvent>>((e, c, ev) => OnNoSlipAttempt(e, c, ev.Args));
        SubscribeLocalEvent<SlowedOverSlipperyComponent, InventoryRelayedEvent<SlipAttemptEvent>>((e, c, ev) => OnSlowedOverSlipAttempt(e, c, ev.Args));
        SubscribeLocalEvent<SlowedOverSlipperyComponent, InventoryRelayedEvent<GetSlowedOverSlipperyModifierEvent>>(OnGetSlowedOverSlipperyModifier);
        SubscribeLocalEvent<SlipperyComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<RecentlySlipppedComponent, SlipAttemptEvent>(OnRecentSlipAttempt);
        SubscribeLocalEvent<SlipGraceComponent, SlippedEvent>(OnGraceSlipped);
    }

    private void HandleStepTrigger(EntityUid uid, SlipperyComponent component, ref StepTriggeredOffEvent args)
    {
        TrySlip(uid, component, args.Tripper);
    }

    private void HandleAttemptCollide(
        EntityUid uid,
        SlipperyComponent component,
        ref StepTriggerAttemptEvent args)
    {
        args.Continue |= CanSlip(uid, args.Tripper);
    }

    private static void OnNoSlipAttempt(EntityUid uid, NoSlipComponent component, SlipAttemptEvent args)
    {
        args.NoSlip = true;
    }

    private void OnSlowedOverSlipAttempt(EntityUid uid, SlowedOverSlipperyComponent component, SlipAttemptEvent args)
    {
        args.SlowOverSlippery = true;
    }

    private void OnThrownSlipAttempt(EntityUid uid, ThrownItemComponent comp, ref SlipCausingAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnGetSlowedOverSlipperyModifier(EntityUid uid, SlowedOverSlipperyComponent comp, ref InventoryRelayedEvent<GetSlowedOverSlipperyModifierEvent> args)
    {
        args.Args.SlowdownModifier *= comp.SlowdownModifier;
    }

    private void OnEntityExit(EntityUid uid, SlipperyComponent component, ref EndCollideEvent args)
    {
        if (HasComp<SpeedModifiedByContactComponent>(args.OtherEntity))
            _speedModifier.AddModifiedEntity(args.OtherEntity);
    }

    private void OnRecentSlipAttempt(EntityUid uid, RecentlySlipppedComponent component, SlipAttemptEvent args)
    {
        if (!TryComp<SlipGraceComponent>(uid, out var slipGrace))
        {
            Log.Error($"{ToPrettyString(uid)} has {nameof(RecentlySlipppedComponent)} but no {nameof(SlipGraceComponent)}.");
            return;
        }

        if (!slipGrace.SuperSlippery && args.SuperSlippery)
            return;

        if (component.NextSlip > _timing.CurTime)
        {
            args.NoSlip = true;
            return;
        }

        RemCompDeferred(uid, component);
    }

    private void OnGraceSlipped(EntityUid uid, SlipGraceComponent component, SlippedEvent args)
    {
        if (args.SuperSlippery && !component.SuperSlippery)
            return;

        EnsureComp<RecentlySlipppedComponent>(uid).NextSlip = _timing.CurTime + component.Delay;
    }

    private bool CanSlip(EntityUid uid, EntityUid toSlip)
    {
        return !_container.IsEntityInContainer(uid)
                && _statusEffects.CanApplyEffect(toSlip, "Stun"); //Should be KnockedDown instead?
    }

    public void TrySlip(EntityUid uid, SlipperyComponent component, EntityUid other, bool requiresContact = true)
    {
        if (HasComp<KnockedDownComponent>(other) && !component.SuperSlippery)
            return;

        var attemptEv = new SlipAttemptEvent(component.SuperSlippery);
        RaiseLocalEvent(other, attemptEv);
        if (attemptEv.SlowOverSlippery)
            _speedModifier.AddModifiedEntity(other);

        if (attemptEv.NoSlip)
            return;

        var attemptCausingEv = new SlipCausingAttemptEvent();
        RaiseLocalEvent(uid, ref attemptCausingEv);
        if (attemptCausingEv.Cancelled)
            return;

        var slipEv = new SlipEvent(other);
        RaiseLocalEvent(uid, ref slipEv);

        var slippedEv = new SlippedEvent(uid, component.SuperSlippery);
        RaiseLocalEvent(other, slippedEv);

        if (TryComp(other, out PhysicsComponent? physics) && !HasComp<SlidingComponent>(other))
        {
            _physics.SetLinearVelocity(other, physics.LinearVelocity * component.LaunchForwardsMultiplier, body: physics);

            if (component.SuperSlippery && requiresContact)
            {
                var sliding = EnsureComp<SlidingComponent>(other);
                sliding.CollidingEntities.Add(uid);
                DebugTools.Assert(_physics.GetContactingEntities(other, physics).Contains(uid));
            }
        }

        var playSound = !_statusEffects.HasStatusEffect(other, "KnockedDown");

        _stun.TryParalyze(other, TimeSpan.FromSeconds(component.ParalyzeTime), true);

        // Preventing from playing the slip sound when you are already knocked down.
        if (playSound)
        {
            _audio.PlayPredicted(component.SlipSound, other, other);
        }

        _adminLogger.Add(LogType.Slip, LogImpact.Low,
            $"{ToPrettyString(other):mob} slipped on collision with {ToPrettyString(uid):entity}");
    }
}

/// <summary>
///     Raised on an entity to determine if it can slip or not.
/// </summary>
public sealed class SlipAttemptEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool NoSlip;
    public bool SlowOverSlippery;
    public SlotFlags TargetSlots { get; } = SlotFlags.FEET;

    public bool SuperSlippery;

    public SlipAttemptEvent(bool superSlippery)
    {
        SuperSlippery = superSlippery;
    }
}

/// <summary>
/// Raised on an entity that is causing the slip event (e.g, the banana peel), to determine if the slip attempt should be cancelled.
/// </summary>
/// <param name="Cancelled">If the slip should be cancelled</param>
[ByRefEvent]
public record struct SlipCausingAttemptEvent (bool Cancelled);

/// Raised on an entity that CAUSED some other entity to slip (e.g., the banana peel).
/// <param name="Slipped">The entity being slipped</param>
[ByRefEvent]
public readonly record struct SlipEvent(EntityUid Slipped);

/// Raised on the entity that got slipped
/// <param name="Slipper">The entity being slipped</param>
/// <param name="SuperSlippery">Was whatever slipped us super slippery</param>
public sealed class SlippedEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    public EntityUid Slipper;
    public bool SuperSlippery;

    public SlippedEvent(EntityUid slipper, bool superSlippery)
    {
        Slipper = slipper;
        SuperSlippery = superSlippery;
    }
}
