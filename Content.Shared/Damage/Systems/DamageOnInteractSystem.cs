using Content.Shared.Administration.Logs;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnInteractSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnInteractComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<DamageOnInteractComponent, DamageOnInteractDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Damages the user that interacts with the entity with an empty hand and
    /// plays a sound or pops up text in response. If the user does not have
    /// proper protection, the user will only be damaged and other interactions
    /// will be cancelled.
    /// </summary>
    /// <param name="entity">The entity being interacted with</param>
    /// <param name="args">Contains the user that interacted with the entity</param>
    private void OnHandInteract(Entity<DamageOnInteractComponent> entity, ref InteractHandEvent args)
    {
        if (!entity.Comp.IsDamageActive)
            return;

        var damageReciever = GetDamageReciever(entity, args.User);

        if (!CheckMobState(damageReciever, entity.Comp))
            return;

        if (!entity.Comp.UseDoAfter)
        {
            args.Handled = DealDamage(entity, damageReciever, args.Target);
            return;
        }

        if (entity.Comp.PopupText != null)
            _popupSystem.PopupClient(Loc.GetString(entity.Comp.PopupText), args.User, args.User);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, entity.Comp.DoAfterTime, new DamageOnInteractDoAfterEvent(), entity.Owner, target: args.Target, used: entity.Owner)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true
        });
    }

    private void OnDoAfter(Entity<DamageOnInteractComponent> entity, ref DamageOnInteractDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null)
            return;

        var damageReciever = GetDamageReciever(entity, args.User);

        // players state can change while its repeating
        if (entity.Comp.DoAfterRepeatable && !CheckMobState(damageReciever, entity.Comp))
        {
            args.Repeat = false;
            args.Handled = true;
            return;
        }

        var dealtDamage = DealDamage(entity, damageReciever, args.Target.Value);
        args.Handled = dealtDamage;
        args.Repeat = dealtDamage && entity.Comp.DoAfterRepeatable;
    }

    private EntityUid GetDamageReciever(Entity<DamageOnInteractComponent> entity, EntityUid initialUid)
    {
        var damageReciever = initialUid;

        if (!entity.Comp.DamageUser)
            damageReciever = entity.Owner;

        return damageReciever;
    }

    private bool CheckMobState(EntityUid uid, DamageOnInteractComponent component)
    {
        if (component.RequiredStates == null)
            return true;

        if (!TryComp<MobStateComponent>(uid, out var mobstateComp) ||
            !component.RequiredStates.Contains(mobstateComp.CurrentState))
        {
            return false;
        }

        return true;
    }


    private bool DealDamage(Entity<DamageOnInteractComponent> entity, EntityUid user, EntityUid target)
    {
        var totalDamage = entity.Comp.Damage;

        if (!entity.Comp.IgnoreResistances)
        {
            // try to get damage on interact protection from either the inventory slots of the entity
            _inventorySystem.TryGetInventoryEntity<DamageOnInteractProtectionComponent>(user, out var protectiveEntity);

            // or checking the entity for  the comp itself if the inventory didn't work
            if (protectiveEntity.Comp == null && TryComp<DamageOnInteractProtectionComponent>(user, out var protectiveComp))
            {
                protectiveEntity = (user, protectiveComp);
            }

            // if protectiveComp isn't null after all that, it means the user has protection,
            // so let's calculate how much they resist
            if (protectiveEntity.Comp != null)
            {
                totalDamage = DamageSpecifier.ApplyModifierSet(totalDamage, protectiveEntity.Comp.DamageProtection);
            }
        }

        totalDamage = _damageableSystem.TryChangeDamage(user, totalDamage, true, origin: target);

        var positiveDamage = false;

        if (totalDamage != null && totalDamage.AnyPositive())
        {
            _adminLogger.Add(LogType.Damaged, $"{ToPrettyString(user):user} injured their hand by interacting with {ToPrettyString(target):target} and received {totalDamage.GetTotal():damage} damage");
            positiveDamage = true;
        }

        if (entity.Comp.IgnoreDamage || positiveDamage)
        {
            _audioSystem.PlayPredicted(entity.Comp.InteractSound, target, user);

            if (!entity.Comp.UseDoAfter && entity.Comp.PopupText != null)
                _popupSystem.PopupClient(Loc.GetString(entity.Comp.PopupText), user, user);

            return true;
        }

        return false;
    }

    public void SetIsDamageActiveTo(Entity<DamageOnInteractComponent> entity, bool mode)
    {
        if (entity.Comp.IsDamageActive == mode)
            return;

        entity.Comp.IsDamageActive = mode;
        Dirty(entity);
    }
}
