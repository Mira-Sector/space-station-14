using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using System.Linq;

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
        UndeployAll(ent, (args.Wearer, inventoryComp));

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

        // safe to check as this gets reset when undeploying any remaining parts
        if (ent.Comp.DeployedParts.Any())
            Dirty(ent);
    }

    private void OnDeployableUnequipped(Entity<ModSuitModulePartDeployableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!ent.Comp.DeployedParts.Any())
            return;

        UndeployAll(ent, args.Wearer);
        Dirty(ent);
    }

    internal void UndeployAll(Entity<ModSuitModulePartDeployableComponent> ent, Entity<InventoryComponent?> wearer)
    {
        var ev = new ModSuitDeployablePartUndeployedEvent(ent.Owner, wearer);

        foreach (var (slot, part) in ent.Comp.DeployedParts)
        {
            _inventory.TryUnequip(wearer.Owner, slot, true, true, true, wearer.Comp);
            RaiseLocalEvent(part, ev);
            QueueDel(part);
        }

        ent.Comp.DeployedParts.Clear();
    }
}
