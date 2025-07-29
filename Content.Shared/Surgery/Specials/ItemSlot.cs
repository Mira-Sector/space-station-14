using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Surgery.Events;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class ItemSlot : SurgerySpecial
{
    [DataField(required: true)]
    public string SlotId;

    [DataField]
    public TimeSpan? Delay;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    private static readonly SpriteSpecifier.Rsi Icon = new(new("/Textures/Interface/surgery_icons.rsi"), "item_slot");

    public override void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.NodeReached(body, limb, user, used, bodyPart, out ui, out bodyUi);

        var entity = IoCManager.Resolve<IEntityManager>();
        var containerSys = entity.System<SharedContainerSystem>();
        containerSys.EnsureContainer<ContainerSlot>((limb ?? body)!.Value, SlotId);
    }

    public override SurgeryInteractionState Interacted(SurgerySpecialInteractionPhase phase, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        ui = null;
        bodyUi = false;

        // so we dont absorb items t
        if (phase != SurgerySpecialInteractionPhase.AfterGraph)
            return SurgeryInteractionState.Failed;

        var entity = IoCManager.Resolve<IEntityManager>();
        var containerSys = entity.System<SharedContainerSystem>();
        if (!containerSys.TryGetContainer((limb ?? body)!.Value, SlotId, out var baseContainer) || baseContainer is not ContainerSlot container)
            return SurgeryInteractionState.Failed;

        if (used == null)
        {
            if (container.ContainedEntity is not { } contained)
                return SurgeryInteractionState.Failed;

            if (Delay != null)
                return SurgeryInteractionState.DoAfter;

            var handSys = entity.System<SharedHandsSystem>();
            if (handSys.TryPickup(user, contained))
                return SurgeryInteractionState.Passed;
        }
        else
        {
            if (container.ContainedEntity != null)
                return SurgeryInteractionState.Failed;

            var whitelistSys = entity.System<EntityWhitelistSystem>();
            if (!whitelistSys.CheckBoth(used.Value, Blacklist, Whitelist))
                return SurgeryInteractionState.Failed;

            if (Delay != null)
                return SurgeryInteractionState.DoAfter;

            if (containerSys.Insert(used.Value, container))
                return SurgeryInteractionState.Passed;
        }

        return SurgeryInteractionState.Failed;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;

        var entity = IoCManager.Resolve<IEntityManager>();
        var containerSys = entity.System<SharedContainerSystem>();
        if (!containerSys.TryGetContainer((limb ?? body)!.Value, SlotId, out var baseContainer) || baseContainer is not ContainerSlot container)
            return false;

        var doAfterArgs = new DoAfterArgs(entity, user, Delay!.Value, new SurgerySpecialDoAfterEvent(this, bodyPart), limb, used: tool)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true,
            RequireDown = true
        };

        return doAfter.TryStartDoAfter(doAfterArgs, out doAfterId);
    }

    public override void OnDoAfter(EntityUid? body, EntityUid? limb, SurgerySpecialDoAfterEvent args)
    {
        var entity = IoCManager.Resolve<IEntityManager>();
        var containerSys = entity.System<SharedContainerSystem>();
        if (!containerSys.TryGetContainer((limb ?? body)!.Value, SlotId, out var baseContainer) || baseContainer is not ContainerSlot container)
            return;

        if (args.Used == null)
        {
            if (container.ContainedEntity is not { } contained)
                return;

            var handSys = entity.System<SharedHandsSystem>();
            handSys.PickupOrDrop(args.User, contained);
        }
        else
        {
            if (container.ContainedEntity != null)
                return;

            var whitelistSys = entity.System<EntityWhitelistSystem>();
            if (!whitelistSys.CheckBoth(args.Used.Value, Blacklist, Whitelist))
                return;

            containerSys.Insert(args.Used.Value, container);
        }
    }

    public override string Name(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-special-item-slot-name");
    }

    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-special-item-slot-desc");
    }

    public override SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Icon;
    }
}
