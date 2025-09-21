using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Shared.Medical.Healing;

public sealed class HealingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealingComponent, UseInHandEvent>(OnHealingUse);
        SubscribeLocalEvent<HealingComponent, AfterInteractEvent>(OnHealingAfterInteract);
        SubscribeLocalEvent<DamageableComponent, HealingDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<DamageableComponent> target, ref HealingDoAfterEvent args)
    {
        OnDoAfter(args.Target ?? target.Owner, target.Owner, target.Comp.Damage, target.Comp.DamageContainerID, ref args);
    }

    private void OnDoAfter(EntityUid uid, EntityUid? limb, DamageSpecifier damage, ProtoId<DamageContainerPrototype>? damageContainer, ref HealingDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp(args.Used, out HealingComponent? healing))
            return;

        if (healing.DamageContainers is not null &&
            damageContainer is not null &&
            !healing.DamageContainers.Contains(damageContainer.Value))
        {
            return;
        }

        TryComp<BloodstreamComponent>(uid, out var bloodstream);

        // Heal some bloodloss damage.
        if (healing.BloodlossModifier != 0 && bloodstream != null)
        {
            var isBleeding = bloodstream.BleedAmount > 0;
            _bloodstreamSystem.TryModifyBleedAmount((uid, bloodstream), healing.BloodlossModifier);
            if (isBleeding != bloodstream.BleedAmount > 0)
            {
                var popup = (args.User == uid)
                    ? Loc.GetString("medical-item-stop-bleeding-self")
                    : Loc.GetString("medical-item-stop-bleeding", ("target", Identity.Entity(uid, EntityManager)));
                _popupSystem.PopupClient(popup, uid, args.User);
            }
        }

        // Restores missing blood
        if (healing.ModifyBloodLevel != 0 && bloodstream != null)
            _bloodstreamSystem.TryModifyBloodLevel((uid, bloodstream), healing.ModifyBloodLevel);

        var healed = _damageable.TryChangeDamage(limb, healing.Damage * _damageable.UniversalTopicalsHealModifier, true, origin: args.Args.User);

        if (healed == null && healing.BloodlossModifier != 0)
            return;

        var total = healed?.GetTotal() ?? FixedPoint2.Zero;

        // Re-verify that we can heal the damage.
        var dontRepeat = false;
        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stacks.Use(args.Used.Value, 1, stackComp);

            if (_stacks.GetCount(args.Used.Value, stackComp) <= 0)
                dontRepeat = true;
        }
        else
        {
            PredictedQueueDel(args.Used.Value);
        }

        if (uid != args.User)
        {
            _adminLogger.Add(LogType.Healed,
                $"{ToPrettyString(args.User):user} healed {ToPrettyString(uid):target} for {total:damage} damage");
        }
        else
        {
            _adminLogger.Add(LogType.Healed,
                $"{ToPrettyString(args.User):user} healed themselves for {total:damage} damage");
        }

        _audio.PlayPredicted(healing.HealingEndSound, uid, args.User);

        // Logic to determine the whether or not to repeat the healing action
        args.Repeat = (CheckPartAiming(args.User, uid, damage, healing, out _) && !dontRepeat);
        if (!args.Repeat && !dontRepeat)
            _popupSystem.PopupClient(Loc.GetString("medical-item-finished-using", ("item", args.Used)), uid, args.User);
        args.Handled = true;
    }

    private bool HasDamage(DamageSpecifier damage, HealingComponent healing)
    {
        var damageableDict = damage.DamageDict;
        var healingDict = healing.Damage.DamageDict;

        foreach (var type in healingDict)
        {
            if (damageableDict[type.Key].Value > 0)
                return true;
        }

        return false;
    }

    private bool CheckBloodloss(EntityUid uid, HealingComponent healing)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return false;

        // Is ent missing blood that we can restore?
        if (healing.ModifyBloodLevel > 0
            && _solutionContainerSystem.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)
            && bloodSolution.Volume < bloodSolution.MaxVolume)
        {
            return true;
        }

        // Is ent bleeding and can we stop it?
        if (healing.BloodlossModifier < 0 && bloodstream.BleedAmount > 0)
            return true;

        return false;
    }

    private bool CheckPartAiming(EntityUid uid, EntityUid target, DamageSpecifier damage, HealingComponent healing, out EntityUid? part)
    {
        part = null;

        if (!TryComp<DamagePartSelectorComponent>(uid, out var damageSelectorComp) || !TryComp<BodyComponent>(target, out var bodyComp))
            return HasDamage(damage, healing) || CheckBloodloss(target, healing);

        var parts = _body.GetBodyChildren(target, bodyComp);

        foreach ((var partUid, var partComp) in parts)
        {
            if (partComp.PartType != damageSelectorComp.SelectedPart.Type)
                continue;

            if (partComp.Symmetry != damageSelectorComp.SelectedPart.Side)
                continue;

            if (!TryComp<DamageableComponent>(partUid, out var damageableComp))
                continue;

            if (HasDamage(damageableComp.Damage, healing))
            {
                part = partUid;
                return true;
            }
        }

        return CheckBloodloss(target, healing);
    }

    private void OnHealingUse(Entity<HealingComponent> healing, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryHeal(healing, args.User, args.User))
            args.Handled = true;
    }

    private void OnHealingAfterInteract(Entity<HealingComponent> healing, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryHeal(healing, args.Target.Value, args.User))
            args.Handled = true;
    }

    private bool TryHeal(Entity<HealingComponent> healing, Entity<DamageableComponent?> target, EntityUid user)
    {
        var bodyDamage = _body.GetBodyDamage(target);
        var bodyDamageContainer = _body.GetMostFrequentDamageContainer(target);

        DamageSpecifier damage;

        if (bodyDamage != null && bodyDamageContainer != null)
        {
            if (healing.Comp.DamageContainers is not null &&
                !healing.Comp.DamageContainers.Contains(bodyDamageContainer.Value))
            {
                return false;
            }

            damage = bodyDamage;
        }
        else if (TryComp<DamageableComponent>(target, out var targetDamage))
        {
            if (healing.Comp.DamageContainers is not null &&
                targetDamage.DamageContainerID is not null &&
                !healing.Comp.DamageContainers.Contains(targetDamage.DamageContainerID.Value))
            {
                return false;
            }

            damage = targetDamage.Damage;
        }
        else
        {
            return false;
        }

        if (user != target.Owner && !_interactionSystem.InRangeUnobstructed(user, target.Owner, popup: true))
            return false;

        if (TryComp<StackComponent>(healing, out var stack) && stack.Count < 1)
            return false;

        if (!CheckPartAiming(user, target, damage, healing.Comp, out var limb))
        {
            _popupSystem.PopupClient(Loc.GetString("medical-item-cant-use", ("item", healing.Owner)), healing, user);
            return false;
        }

        _audio.PlayPredicted(healing.Comp.HealingBeginSound, healing, user);

        var isNotSelf = user != target.Owner;

        if (isNotSelf)
        {
            var msg = Loc.GetString("medical-item-popup-target", ("user", Identity.Entity(user, EntityManager)), ("item", healing.Owner));
            _popupSystem.PopupEntity(msg, target, target, PopupType.Medium);
        }

        var delay = isNotSelf
            ? healing.Comp.Delay
            : healing.Comp.Delay * GetScaledHealingPenalty(healing);

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, delay, new HealingDoAfterEvent(), limb ?? target, target: target, used: healing.Owner)
            {
                // Didn't break on damage as they may be trying to prevent it and
                // not being able to heal your own ticking damage would be frustrating.
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    /// <summary>
    /// Scales the self-heal penalty based on the amount of damage taken
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public float GetScaledHealingPenalty(Entity<HealingComponent> healing)
    {
        if (!TryComp<MobThresholdsComponent>(healing.Owner, out var mobThreshold))
            return 1f;

        var totalDamage = FixedPoint2.Zero;

        if (TryComp<DamageableComponent>(healing.Owner, out var damageable))
        {
            totalDamage = damageable.TotalDamage;
        }
        else if (_body.GetBodyDamage(healing.Owner) is { } bodyDamage)
        {
            totalDamage = bodyDamage.GetTotal();
        }
        else
        {
            return 1f;
        }

        if (!_mobThresholdSystem.TryGetThresholdForState(healing.Owner, MobState.Critical, out var amount, mobThreshold))
            return 1f;

        var percentDamage = (float)(totalDamage / amount);
        //basically make it scale from 1 to the multiplier.
        var modifier = percentDamage * (healing.Comp.SelfHealPenaltyMultiplier - 1) + 1;
        return Math.Max(modifier, 1);
    }
}
