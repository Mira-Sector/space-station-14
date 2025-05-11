using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private void DeployableInitialize()
    {
        SubscribeLocalEvent<ModSuitModulePartDeployableComponent, ClothingGotEquippedEvent>(OnDeployableEquipped);
        SubscribeLocalEvent<ModSuitModulePartDeployableComponent, ClothingGotUnequippedEvent>(OnDeployableUnequipped);
    }

    private void OnDeployableEquipped(Entity<ModSuitModulePartDeployableComponent> ent, ref ClothingGotEquippedEvent args)
    {
        // cleanup any mess we forgot to do
        UndeployAll(ent, args.Wearer);

        foreach (var (partId, slot) in ent.Comp.DeployableParts)
        {
            if (!_inventory.HasSlot(args.Wearer, slot))
                continue;

            var part = Spawn(partId);
            var beforeEv = new ModSuitDeployablePartBeforeEquippedEvent(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, beforeEv);

            _inventory.TryUnequip(args.Wearer, slot, true, true, true);

            if (!_inventory.TryEquip(args.Wearer, part, slot, true, true, true))
            {
                var failedEv = new ModSuitDeployablePartUndeployedEvent(ent.Owner, args.Wearer);
                RaiseLocalEvent(part, failedEv);

                QueueDel(part);
                continue;
            }

            var afterEv = new ModSuitDeployablePartDeployed(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, afterEv);
        }
    }

    private void OnDeployableUnequipped(Entity<ModSuitModulePartDeployableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UndeployAll(ent, args.Wearer);
    }

    internal void UndeployAll(Entity<ModSuitModulePartDeployableComponent> ent, EntityUid wearer)
    {
        var ev = new ModSuitDeployablePartUndeployedEvent(ent.Owner, wearer);

        foreach (var part in ent.Comp.DeployedParts)
        {
            RaiseLocalEvent(part, ev);
            QueueDel(part);
        }

        ent.Comp.DeployedParts.Clear();
    }
}
