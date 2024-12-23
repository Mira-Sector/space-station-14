using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Armor;
using Content.Shared.Clothing.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Explosion.Components;
using Content.Server.Footprints.Components;
using Content.Shared.Hands;
using Content.Shared.HealthExaminable;
using Content.Shared.Movement.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Radiation.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Physics.Components;

namespace Content.Server.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    const string FootstepTag = "FootstepSound";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonDisguiseComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<ChameleonDisguiseComponent, DisguiseToggleNoRotEvent>(OnToggleNoRot);
        SubscribeLocalEvent<ChameleonDisguiseComponent, DisguiseToggleAnchoredEvent>(OnToggleAnchored);
    }

    private void OnEquippedHand(Entity<ChameleonDisguiseComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!TryComp<PolymorphedEntityComponent>(ent, out var poly))
            return;

        _polymorph.Revert((ent, poly));
        args.Handled = true;
    }

    public override void Disguise(ChameleonProjectorComponent proj, EntityUid user, EntityUid entity)
    {
        if (_polymorph.PolymorphEntity(user, proj.Polymorph) is not {} disguise)
            return;

        // make disguise look real (for simple things at least)
        var meta = MetaData(entity);
        _meta.SetEntityName(disguise, meta.EntityName);
        _meta.SetEntityDescription(disguise, meta.EntityDescription);

        var comp = EnsureComp<ChameleonDisguiseComponent>(disguise);
        comp.SourceEntity = entity;
        comp.SourceProto = Prototype(entity)?.ID;
        Dirty(disguise, comp);

        // no sechud trolling
        if (!proj.Action)
            RemComp<StatusIconComponent>(disguise);

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

        if (TryComp<LeavesFootprintsComponent>(entity, out var entFootprintComp))
        {
            var disguiseFootprintComp = EnsureComp<LeavesFootprintsComponent>(disguise);
            disguiseFootprintComp.MaxFootsteps = entFootprintComp.MaxFootsteps;
            disguiseFootprintComp.Distance = entFootprintComp.Distance;
            disguiseFootprintComp.FootprintPrototype = entFootprintComp.FootprintPrototype;
            disguiseFootprintComp.FootprintPrototypeAlternative = entFootprintComp.FootprintPrototypeAlternative;
        }

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

        var mass = CompOrNull<PhysicsComponent>(entity)?.Mass ?? 0f;

        // let the disguise die when its taken enough damage, which then transfers to the player
        // health is proportional to mass, and capped to not be insane
        if (TryComp<MobThresholdsComponent>(disguise, out var thresholds))
        {
            // if the player is of flesh and blood, cap max health to theirs
            // so that when reverting damage scales 1:1 and not round removing
            var playerMax = _mobThreshold.GetThresholdForState(user, MobState.Dead).Float();
            var max = playerMax == 0f ? proj.MaxHealth : Math.Max(proj.MaxHealth, playerMax);
            var health = Math.Clamp(mass, proj.MinHealth, proj.MaxHealth);
            _mobThreshold.SetMobStateThreshold(disguise, health, MobState.Critical, thresholds);
            _mobThreshold.SetMobStateThreshold(disguise, max, MobState.Dead, thresholds);
        }

        // add actions for controlling transform aspects
        if (HasComp<AnchorableComponent>(entity))
        {
            _actions.AddAction(disguise, proj.NoRotAction);
            _actions.AddAction(disguise, proj.AnchorAction);
        }
    }

    private void OnToggleNoRot(Entity<ChameleonDisguiseComponent> ent, ref DisguiseToggleNoRotEvent args)
    {
        var xform = Transform(ent);
        xform.NoLocalRotation = !xform.NoLocalRotation;
    }

    private void OnToggleAnchored(Entity<ChameleonDisguiseComponent> ent, ref DisguiseToggleAnchoredEvent args)
    {
        var uid = ent.Owner;
        var xform = Transform(uid);
        if (xform.Anchored)
            _xform.Unanchor(uid, xform);
        else
            _xform.AnchorEntity((uid, xform));
    }
}
