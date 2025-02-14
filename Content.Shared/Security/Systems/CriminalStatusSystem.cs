using Content.Shared.Access.Systems;
using Content.Shared.Contraband;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.CriminalRecords;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Security.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Security.Systems;

public sealed class CriminalStatusSystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _id = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CriminalRecordComponent, CriminalRecordChangeStatus>(OnCriminalRecordChanged);

        SubscribeLocalEvent<CriminalRecordComponent, ClothingDidEquippedEvent>((u, c, a) => OnEquippedOrUniquip(u, c, a.Clothing, true));
        SubscribeLocalEvent<CriminalRecordComponent, ClothingDidUnequippedEvent>((u, c, a) => OnEquippedOrUniquip(u, c, a.Clothing, false));

        SubscribeLocalEvent<CriminalRecordComponent, DidEquipHandEvent>((u, c, a) => OnPickupOrDrop(u, c, a.Equipped, true));
        SubscribeLocalEvent<CriminalRecordComponent, DidUnequipHandEvent>((u, c, a) => OnPickupOrDrop(u, c, a.Unequipped, false));
    }

    private void OnCriminalRecordChanged(EntityUid uid, CriminalRecordComponent component, ref CriminalRecordChangeStatus args)
    {
        if (component.LastSecurityStatus == args.Status)
            return;

        component.Points -= component.SecurityStatusPoints[component.LastSecurityStatus];
        component.Points += component.SecurityStatusPoints[args.Status];
        component.LastSecurityStatus = args.Status;
    }

    private void OnEquippedOrUniquip(EntityUid uid, CriminalRecordComponent component, Entity<ClothingComponent> clothing, bool equip)
    {
        if (clothing.Comp.InSlot == null)
            return;

        if (!TryComp<ContrabandComponent>(clothing, out var contraband))
            return;

        if (contraband.CriminalPoints == 0f)
            return;

        if (!_inventory.TryGetSlots(uid, out var slots))
            return;

        SlotFlags? slot = null;

        foreach (var invSlot in slots)
        {
            if (clothing.Comp.InSlot != invSlot.Name)
                continue;

            slot = invSlot.SlotFlags;
            break;
        }

        if (slot == null)
            return;

        if (!component.ClothingSlotPoints.TryGetValue(slot.Value, out var slotMultiplier))
            return;

        if (CheckIdCard(uid, contraband))
            return;

        if (equip)
            component.Points += contraband.CriminalPoints * slotMultiplier;
        else
            component.Points -= contraband.CriminalPoints * slotMultiplier;
    }

    private void OnPickupOrDrop(EntityUid uid, CriminalRecordComponent component, EntityUid item, bool pickup)
    {
        if (!TryComp<ContrabandComponent>(item, out var contraband))
            return;

        if (contraband.CriminalPoints == 0f)
            return;

        if (CheckIdCard(uid, contraband))
            return;

        if (pickup)
            component.Points += contraband.CriminalPoints;
        else
            component.Points -= contraband.CriminalPoints;
    }

    private bool CheckIdCard(EntityUid uid, ContrabandComponent contraband)
    {
        List<ProtoId<DepartmentPrototype>>? departments = null;
        if (_id.TryFindIdCard(uid, out var id))
        {
            departments = id.Comp.JobDepartments;
        }

        return contraband.AllowedDepartments != null && departments != null && departments.Intersect(contraband.AllowedDepartments).Any();
    }
}
