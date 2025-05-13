using Content.Shared.Clothing;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using System.Linq;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly InventorySystem Inventory = default!;
    [Dependency] private readonly INetManager _net = default!;

    private const string DeployableSlotPrefix = "deployable-";

    private void InitializeDeployable()
    {
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ComponentInit>(OnDeployableInit);
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ComponentRemove>(OnDeployableRemoved);

        SubscribeLocalEvent<ModSuitPartDeployableComponent, ClothingGotEquippedEvent>(OnDeployableEquipped);
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ClothingGotUnequippedEvent>(OnDeployableUnequipped);
    }

    private void OnDeployableInit(Entity<ModSuitPartDeployableComponent> ent, ref ComponentInit args)
    {
        foreach (var (slot, partId) in ent.Comp.DeployablePartIds)
        {
            var ev = new ModSuitDeployableGetPartEvent(GetNetEntity(ent.Owner), slot);

            if (_net.IsServer)
                RaiseLocalEvent(ev);
            else if (_net.IsClient)
                RaiseNetworkEvent(ev);
            else
                continue;

            if (!ev.Handled && _net.IsServer)
            {
                Log.Warning($"Unable to create deployable part {partId} for {ToPrettyString(ent.Owner)}.");
                continue;
            }

            var part = GetEntity(ev.Part);

            var container = Container.EnsureContainer<ContainerSlot>(ent.Owner, GetDeployableSlotId(slot));

            if (!Container.Insert(part, container))
            {
                if (_net.IsServer)
                    Del(part);

                continue;
            }

            ent.Comp.DeployableContainers.Add(slot, container);
        }

        Dirty(ent);
    }

    private void OnDeployableRemoved(Entity<ModSuitPartDeployableComponent> ent, ref ComponentRemove args)
    {
        foreach (var (slot, part) in ent.Comp.DeployedParts)
        {
            var ev = new ModSuitDeployablePartUnequippedEvent(ent.Owner, ent.Comp.Wearer, slot);
            RaiseLocalEvent(part, ev);

            if (_net.IsServer)
                Del(part);
        }
    }

    private void OnDeployableEquipped(Entity<ModSuitPartDeployableComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.Wearer = args.Wearer;

        var inventoryComp = CompOrNull<InventoryComponent>(args.Wearer);

        // cleanup any mess we forgot to do
        UndeployAll(ent, (args.Wearer, inventoryComp));

        if (inventoryComp == null)
        {
            Dirty(ent);
            return;
        }

        foreach (var (slot, part) in ent.Comp.DeployableParts)
        {
            if (!Inventory.HasSlot(args.Wearer, slot, inventoryComp))
                continue;

            var beforeEv = new ModSuitDeployablePartBeforeEquippedEvent(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, beforeEv);

            var hadUnremovable = RemComp<UnremoveableComponent>(part);

            if (!Inventory.TryEquip(args.Wearer, part, slot, true, true, true, inventoryComp))
            {
                var failedEv = new ModSuitDeployablePartUnequippedEvent(ent.Owner, args.Wearer, slot);
                RaiseLocalEvent(part, failedEv);

                var container = Container.EnsureContainer<ContainerSlot>(ent.Owner, GetDeployableSlotId(slot));
                Container.Insert(part, container);
                continue;
            }

            if (hadUnremovable)
                EnsureComp<UnremoveableComponent>(part);

            ent.Comp.DeployedParts.Add(slot, part);

            var afterEv = new ModSuitDeployablePartDeployedEvent(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, afterEv);
        }
    }

    private void OnDeployableUnequipped(Entity<ModSuitPartDeployableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;

        if (!ent.Comp.DeployedParts.Any())
        {
            Dirty(ent);
            return;
        }

        UndeployAll(ent, args.Wearer);
        Dirty(ent);
    }

    internal void UndeployAll(Entity<ModSuitPartDeployableComponent> ent, Entity<InventoryComponent?> wearer)
    {
        foreach (var (slot, part) in ent.Comp.DeployedParts)
        {
            Inventory.TryUnequip(wearer.Owner, slot, true, true, true, wearer.Comp);

            var container = Container.EnsureContainer<ContainerSlot>(ent.Owner, GetDeployableSlotId(slot));
            Container.Insert(part, container);

            var ev = new ModSuitDeployablePartUnequippedEvent(ent.Owner, wearer.Owner, slot);
            RaiseLocalEvent(part, ev);
        }

        ent.Comp.DeployedParts.Clear();
    }

    protected static string GetDeployableSlotId(string slot)
    {
        return DeployableSlotPrefix + slot;
    }
}
