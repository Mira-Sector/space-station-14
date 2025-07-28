using Content.Shared.Body.Part;
using Content.Shared.Inventory;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Events;

namespace Content.Shared.Surgery.Systems;

public sealed partial class AllowSurgeryClothingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private EntityQuery<AllowSurgeryClothingComponent> _allowSurgeryQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, SurgeryInteractionAttemptEvent>(OnInventoryAttempt);

        _allowSurgeryQuery = GetEntityQuery<AllowSurgeryClothingComponent>();
    }

    private void OnInventoryAttempt(Entity<InventoryComponent> ent, ref SurgeryInteractionAttemptEvent args)
    {
        _inventory.RelayEvent(ent, ref args);

        if (args.Cancelled)
            return;

        HashSet<BodyPartType> allowedParts = [];
        allowedParts.EnsureCapacity(Enum.GetValues<BodyPartType>().Length);

        var enumerator = _inventory.GetSlotEnumerator(ent.AsNullable(), args.TargetSlots);
        while (enumerator.NextSlot(out var slot, out var item))
        {
            var slotBodyParts = SlotToBodyParts(slot.SlotFlags);

            if (item == null)
            {
                allowedParts.UnionWith(slotBodyParts);
                continue;
            }

            if (!_allowSurgeryQuery.TryComp(item, out var allowSurgery))
            {
                // something blocked us
                if (slotBodyParts.Contains(args.Part.Type))
                {
                    Cancel(ref args);
                    return;
                }

                continue;
            }

            foreach (var bodyPart in allowSurgery.BodyParts)
            {
                if (bodyPart != args.Part.Type)
                    continue;

                allowedParts.UnionWith(slotBodyParts);
                break;
            }
        }

        if (!allowedParts.Contains(args.Part.Type))
            Cancel(ref args);
    }

    private static HashSet<BodyPartType> SlotToBodyParts(SlotFlags slot)
    {
        return slot switch
        {
            SlotFlags.HEAD => [BodyPartType.Head],
            SlotFlags.EYES => [BodyPartType.Head],
            SlotFlags.EARS => [BodyPartType.Head],
            SlotFlags.MASK => [BodyPartType.Head],
            SlotFlags.OUTERCLOTHING => [BodyPartType.Torso, BodyPartType.Arm, BodyPartType.Leg],
            SlotFlags.INNERCLOTHING => [BodyPartType.Torso, BodyPartType.Arm, BodyPartType.Leg],
            SlotFlags.NECK => [BodyPartType.Head],
            SlotFlags.GLOVES => [BodyPartType.Arm],
            SlotFlags.LEGS => [BodyPartType.Leg],
            SlotFlags.FEET => [BodyPartType.Leg],
            _ => []
        };
    }

    private void Cancel(ref SurgeryInteractionAttemptEvent args)
    {
        args.Reason = Loc.GetString("surgery-interaction-failure-allow-clothing", ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(args.Part))));
        args.Cancel();
    }
}
