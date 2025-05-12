using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] protected readonly InventorySystem Inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private void InitializeDeployable()
    {
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ClothingGotEquippedEvent>(OnDeployableEquipped);
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ClothingGotUnequippedEvent>(OnDeployableUnequipped);
    }

    private void OnDeployableEquipped(Entity<ModSuitPartDeployableComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var inventoryComp = CompOrNull<InventoryComponent>(args.Wearer);

        // cleanup any mess we forgot to do
        UndeployAll(ent, (args.Wearer, inventoryComp));

        if (inventoryComp == null)
            return;

        foreach (var (slot, _) in ent.Comp.DeployableParts)
        {
            if (!Inventory.HasSlot(args.Wearer, slot, inventoryComp))
                continue;

            var getPartEv = new ModSuitDeployableGetPartEvent(GetNetEntity(ent.Owner), slot);

            if (_net.IsServer)
                RaiseLocalEvent(getPartEv);
            else
                RaiseNetworkEvent(getPartEv);

            if (!getPartEv.Handled)
                continue;

            var part = GetEntity(getPartEv.Part);

            var beforeEv = new ModSuitDeployablePartBeforeEquippedEvent(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, beforeEv);

            Inventory.TryUnequip(args.Wearer, slot, true, true, true, inventoryComp);

            if (!Inventory.TryEquip(args.Wearer, part, slot, true, true, true, inventoryComp))
            {
                var failedEv = new ModSuitDeployablePartUnequippedEvent(ent.Owner, args.Wearer, slot);
                RaiseLocalEvent(part, failedEv);

                if (_net.IsServer)
                    QueueDel(part);

                continue;
            }

            var afterEv = new ModSuitDeployablePartDeployed(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, afterEv);
        }

        // safe to check as this gets reset when undeploying any remaining parts
        if (ent.Comp.DeployedParts.Any())
            Dirty(ent);
    }

    private void OnDeployableUnequipped(Entity<ModSuitPartDeployableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!ent.Comp.DeployedParts.Any())
            return;

        UndeployAll(ent, args.Wearer);
        Dirty(ent);
    }

    internal void UndeployAll(Entity<ModSuitPartDeployableComponent> ent, Entity<InventoryComponent?> wearer)
    {
        foreach (var (slot, part) in ent.Comp.DeployedParts)
        {
            var ev = new ModSuitDeployablePartUnequippedEvent(ent.Owner, wearer.Owner, slot);

            Inventory.TryUnequip(wearer.Owner, slot, true, true, true, wearer.Comp);
            RaiseLocalEvent(part, ev);

            if (_net.IsServer)
                QueueDel(part);
        }

        ent.Comp.DeployedParts.Clear();
    }
}
