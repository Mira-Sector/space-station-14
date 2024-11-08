using Content.Shared.Actions;
using Content.Shared.Armor;
using Content.Shared.Coordinates;
using Content.Shared.Construction.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Damage;
using Content.Shared.Explosion.Components;
using Content.Shared.Hands;
using Content.Shared.HealthExaminable;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Polymorph.Components;
using Content.Shared.Popups;
using Content.Shared.Radiation.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Polymorph.Systems;

/// <summary>
/// Handles disguise validation, disguising and revealing.
/// Most appearance copying is done clientside.
/// </summary>
public abstract class SharedChameleonProjectorSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _serMan = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    const string FootstepTag = "FootstepSound";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonProjectorComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChameleonProjectorComponent, DisguiseActionEvent>(OnAction);

        SubscribeLocalEvent<ChameleonDisguiseComponent, InteractHandEvent>(OnDisguiseInteractHand, before: [typeof(SharedItemSystem)]);
        SubscribeLocalEvent<ChameleonDisguiseComponent, DamageChangedEvent>(OnDisguiseDamaged);
        SubscribeLocalEvent<ChameleonDisguiseComponent, InsertIntoEntityStorageAttemptEvent>(OnDisguiseInsertAttempt);
        SubscribeLocalEvent<ChameleonDisguiseComponent, ComponentShutdown>(OnDisguiseShutdown);

        SubscribeLocalEvent<ChameleonProjectorComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ChameleonProjectorComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ChameleonProjectorComponent, DisguiseToggleNoRotEvent>(OnToggleNoRot);
        SubscribeLocalEvent<ChameleonProjectorComponent, DisguiseToggleAnchoredEvent>(OnToggleAnchored);
        SubscribeLocalEvent<ChameleonProjectorComponent, HandDeselectedEvent>(OnDeselected);
        SubscribeLocalEvent<ChameleonProjectorComponent, GotUnequippedHandEvent>(OnUnequipped);
        SubscribeLocalEvent<ChameleonProjectorComponent, ComponentShutdown>(OnProjectorShutdown);

    }

    private void OnInit(EntityUid uid, ChameleonProjectorComponent component, MapInitEvent args)
    {
        if (!component.Action)
            return;

        _actions.AddAction(uid, ref component.DisguiseActionEntity, component.DisguiseAction);
    }

    private void OnAction(EntityUid uid, ChameleonProjectorComponent component, DisguiseActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryDisguise((uid, component), uid, args.Target);
    }

    #region Disguise entity

    private void OnDisguiseInteractHand(Entity<ChameleonDisguiseComponent> ent, ref InteractHandEvent args)
    {
        TryReveal(ent.Comp.User);
        args.Handled = true;
    }

    private void OnDisguiseDamaged(Entity<ChameleonDisguiseComponent> ent, ref DamageChangedEvent args)
    {
        // this mirrors damage 1:1
        if (args.DamageDelta is {} damage)
            _damageable.TryChangeDamage(ent.Comp.User, damage);
    }

    private void OnDisguiseInsertAttempt(Entity<ChameleonDisguiseComponent> ent, ref InsertIntoEntityStorageAttemptEvent args)
    {
        // stay parented to the user, not the storage
        args.Cancelled = true;
    }

    private void OnDisguiseShutdown(Entity<ChameleonDisguiseComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveProvidedActions(ent.Comp.User, ent.Comp.Projector);
    }

    #endregion

    #region Projector

    private void OnInteract(Entity<ChameleonProjectorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not {} target)
            return;

        args.Handled = true;
        TryDisguise(ent, args.User, target);
    }

    private void OnGetVerbs(Entity<ChameleonProjectorComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess)
            return;

        var user = args.User;
        var target = args.Target;
        args.Verbs.Add(new UtilityVerb()
        {
            Act = () =>
            {
                TryDisguise(ent, user, target);
            },
            Text = Loc.GetString("chameleon-projector-set-disguise")
        });
    }

    public bool TryDisguise(Entity<ChameleonProjectorComponent> ent, EntityUid user, EntityUid target)
    {
        if (_container.IsEntityInContainer(target))
        {
            _popup.PopupClient(Loc.GetString("chameleon-projector-inside-container"), target, user);
            return false;
        }

        if (IsInvalid(ent.Comp, target))
        {
            _popup.PopupClient(Loc.GetString("chameleon-projector-invalid"), target, user);
            return false;
        }

        _popup.PopupClient(Loc.GetString("chameleon-projector-success"), target, user);
        Disguise(ent, user, target);
        return true;
    }

    private void OnToggleNoRot(Entity<ChameleonProjectorComponent> ent, ref DisguiseToggleNoRotEvent args)
    {
        if (ent.Comp.Disguised is not {} uid)
            return;

        var xform = Transform(uid);
        _xform.SetLocalRotationNoLerp(uid, 0, xform);
        xform.NoLocalRotation = !xform.NoLocalRotation;
        args.Handled = true;
    }

    private void OnToggleAnchored(Entity<ChameleonProjectorComponent> ent, ref DisguiseToggleAnchoredEvent args)
    {
        if (ent.Comp.Disguised is not {} uid)
            return;

        var xform = Transform(uid);
        if (xform.Anchored)
            _xform.Unanchor(uid, xform);
        else
            _xform.AnchorEntity((uid, xform));

        args.Handled = true;
    }

    private void OnDeselected(Entity<ChameleonProjectorComponent> ent, ref HandDeselectedEvent args)
    {
        RevealProjector(ent);
    }

    private void OnUnequipped(Entity<ChameleonProjectorComponent> ent, ref GotUnequippedHandEvent args)
    {
        RevealProjector(ent);
    }

    private void OnProjectorShutdown(Entity<ChameleonProjectorComponent> ent, ref ComponentShutdown args)
    {
        RevealProjector(ent);
    }

    #endregion

    #region API

    /// <summary>
    /// Returns true if an entity cannot be used as a disguise.
    /// </summary>
    public bool IsInvalid(ChameleonProjectorComponent comp, EntityUid target)
    {
        return _whitelist.IsWhitelistFail(comp.Whitelist, target)
            || _whitelist.IsBlacklistPassOrNull(comp.Blacklist, target);
    }

    /// <summary>
    /// On server, polymorphs the user into an entity and sets up the disguise.
    /// </summary>
    public void Disguise(Entity<ChameleonProjectorComponent> ent, EntityUid user, EntityUid entity)
    {
        var proj = ent.Comp;

        // no spawning prediction sorry
        if (_net.IsClient)
            return;

        // reveal first to allow quick switching
        TryReveal(user);

        // add actions for controlling transform aspects
        if (HasComp<AnchorableComponent>(entity))
        {
            _actions.AddAction(user, ref proj.NoRotActionEntity, proj.NoRotAction, container: ent);
            _actions.AddAction(user, ref proj.AnchorActionEntity, proj.AnchorAction, container: ent);
        }

        proj.Disguised = user;

        var disguise = SpawnAttachedTo(proj.DisguiseProto, user.ToCoordinates());

        var disguised = AddComp<ChameleonDisguisedComponent>(user);
        disguised.Disguise = disguise;
        Dirty(user, disguised);

        // make disguise look real (for simple things at least)
        var meta = MetaData(entity);
        _meta.SetEntityName(disguise, meta.EntityName);
        _meta.SetEntityDescription(disguise, meta.EntityDescription);

        var comp = EnsureComp<ChameleonDisguiseComponent>(disguise);
        comp.User = user;
        comp.Projector = ent;
        comp.SourceEntity = entity;
        comp.SourceProto = Prototype(entity)?.ID;
        Dirty(disguise, comp);

        // item disguises can be picked up to be revealed, also makes sure their examine size is correct
        CopyComp<ItemComponent>((disguise, comp));

        _appearance.CopyData(entity, disguise);

        // mimic humans
        CopyComp<HumanoidAppearanceComponent>((disguise, comp));
        CopyComp<HealthExaminableComponent>((disguise, comp));
        CopyComp<TypingIndicatorComponent>((disguise, comp));
        CopyComp<FootstepModifierComponent>((disguise, comp));
        CopyComp<SpeechComponent>((disguise, comp));
        CopyComp<GrammarComponent>((disguise, comp));

        if (_tag.HasTag(entity, FootstepTag))
        {
            var tagComp = EnsureComp<TagComponent>(disguise);
            _tag.AddTag((disguise, tagComp), FootstepTag);
        }

        var ev = new ChameleonDisguisedEvent(entity);
        RaiseLocalEvent(disguise, ev);

        // manually set the emotes
        if (TryComp<VocalComponent>(entity, out var entVocalComp))
        {
            CopyComp<VocalComponent>((disguise, comp));
            if (!TryComp<VocalComponent>(disguise, out var disguiseVocalComp))
                return;

            disguiseVocalComp.EmoteSounds = entVocalComp.EmoteSounds;
        }

        // show the humans clothes
        if (TryComp<InventoryComponent>(entity, out var entInvComp))
        {
            CopyComp<InventoryComponent>((disguise, comp));
            if (!TryComp<InventoryComponent>(disguise, out var disguiseInvComp))
                return;

            var coords = Transform(disguise).Coordinates;

            foreach (var entSlot in disguiseInvComp.Slots)
            {
                _inventory.TryGetSlotContainer(entity, entSlot.Name, out var entContainer, out _);

                if (entContainer== null || entContainer.ContainedEntity == null)
                    continue;

                if (!TryComp<ClothingComponent>(entContainer.ContainedEntity, out var clothingComp) || clothingComp.RsiPath == null || clothingComp.InSlot == null)
                    continue;

                var metaData = MetaData(entContainer.ContainedEntity.Value);

                if (metaData.EntityPrototype == null)
                    continue;

                var clothing = EntityManager.SpawnEntity(metaData.EntityPrototype.ID, coords);

                if (!_inventory.TryEquip(disguise, clothing, entSlot.Name, true, true))
                {
                    EntityManager.DeleteEntity(clothing);
                    continue;
                }

                RemComp<ArmorComponent>(clothing);
                RemComp<GeigerComponent>(clothing);
                RemComp<ExplosionResistanceComponent>(clothing);
                RemComp<ActionsContainerComponent>(clothing);
            }
        }
    }

    /// <summary>
    /// Removes the disguise, if the user is disguised.
    /// </summary>
    public bool TryReveal(Entity<ChameleonDisguisedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (TryComp<ChameleonDisguiseComponent>(ent.Comp.Disguise, out var disguise)
            && TryComp<ChameleonProjectorComponent>(disguise.Projector, out var proj))
        {
            proj.Disguised = null;
        }

        var xform = Transform(ent);
        xform.NoLocalRotation = false;
        _xform.Unanchor(ent, xform);

        Del(ent.Comp.Disguise);
        RemComp<ChameleonDisguisedComponent>(ent);
        return true;
    }

    /// <summary>
    /// Reveal a projector's user, if any.
    /// </summary>
    public void RevealProjector(Entity<ChameleonProjectorComponent> ent)
    {
        if (ent.Comp.Disguised is {} user)
            TryReveal(user);
    }

    #endregion

    /// <summary>
    /// Copy a component from the source entity/prototype to the disguise entity.
    /// </summary>
    /// <remarks>
    /// This would probably be a good thing to add to engine in the future.
    /// </remarks>
    protected bool CopyComp<T>(Entity<ChameleonDisguiseComponent> ent) where T: Component, new()
    {
        if (!GetSrcComp<T>(ent.Comp, out var src))
            return true;

        // remove then re-add to prevent a funny
        RemComp<T>(ent);
        var dest = AddComp<T>(ent);
        _serMan.CopyTo(src, ref dest, notNullableOverride: true);
        Dirty(ent, dest);
        return false;
    }

    /// <summary>
    /// Try to get a single component from the source entity/prototype.
    /// </summary>
    private bool GetSrcComp<T>(ChameleonDisguiseComponent comp, [NotNullWhen(true)] out T? src) where T: Component
    {
        src = null;
        if (TryComp(comp.SourceEntity, out src))
            return true;

        if (comp.SourceProto is not {} protoId)
            return false;

        if (!_proto.TryIndex<EntityPrototype>(protoId, out var proto))
            return false;

        return proto.TryGetComponent(out src);
    }
}

/// <summary>
/// Action event for toggling transform NoRot on a disguise.
/// </summary>
public sealed partial class DisguiseToggleNoRotEvent : InstantActionEvent
{
}

/// <summary>
/// Action event for toggling transform Anchored on a disguise.
/// </summary>
public sealed partial class DisguiseToggleAnchoredEvent : InstantActionEvent
{
}

public sealed partial class DisguiseActionEvent : EntityTargetActionEvent
{
}
