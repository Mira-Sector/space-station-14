using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private void InitializeDeployable()
    {
        SubscribeLocalEvent<ModSuitModulePartDeployableComponent, ClothingGotEquippedEvent>(OnDeployableEquipped);
        SubscribeLocalEvent<ModSuitModulePartDeployableComponent, ClothingGotUnequippedEvent>(OnDeployableUnequipped);
    }

    private void OnDeployableEquipped(Entity<ModSuitModulePartDeployableComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var inventoryComp = CompOrNull<InventoryComponent>(args.Wearer);

        // cleanup any mess we forgot to do
        UndeployAll((ent.Owner, ent.Comp, inventoryComp), args.Wearer);

        if (inventoryComp == null)
            return;

        foreach (var (slot, partId) in ent.Comp.DeployableParts)
        {
            if (!_inventory.HasSlot(args.Wearer, slot, inventoryComp))
                continue;

            var part = Spawn(partId);
            var beforeEv = new ModSuitDeployablePartBeforeEquippedEvent(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, beforeEv);

            _inventory.TryUnequip(args.Wearer, slot, true, true, true, inventoryComp);

            if (!_inventory.TryEquip(args.Wearer, part, slot, true, true, true, inventoryComp))
            {
                var failedEv = new ModSuitDeployablePartUndeployedEvent(ent.Owner, args.Wearer);
                RaiseLocalEvent(part, failedEv);

                QueueDel(part);
                continue;
            }

            var afterEv = new ModSuitDeployablePartDeployed(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, afterEv);

            ent.Comp.DeployedParts.Add(slot, part);
        }

        Dirty(ent);
    }

    private void OnDeployableUnequipped(Entity<ModSuitModulePartDeployableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UndeployAll(ent, args.Wearer);
    }

    internal void UndeployAll(Entity<ModSuitModulePartDeployableComponent, InventoryComponent?> ent, EntityUid wearer)
    {
        var ev = new ModSuitDeployablePartUndeployedEvent(ent.Owner, wearer);

        foreach (var (slot, part) in ent.Comp1.DeployedParts)
        {
            _inventory.TryUnequip(wearer, slot, true, true, true, ent.Comp2);
            RaiseLocalEvent(part, ev);
            QueueDel(part);
        }

        ent.Comp1.DeployedParts.Clear();
    }
}
