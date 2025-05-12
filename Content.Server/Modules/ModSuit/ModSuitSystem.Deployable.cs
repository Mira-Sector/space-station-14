using Content.Server.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Server.Modules.ModSuit;

public partial class ModSuitSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private void InitializeDeployable()
    {
        SubscribeAllEvent<ModSuitDeployableGetPartEvent>(OnDeployableGetEntity);

        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ModSuitDeployablePartBeforeEquippedEvent>(OnDeployableInventoryBeforeEquipped);
        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ModSuitDeployablePartUnequippedEvent>(OnDeployableInventoryUnequipped);
    }

    #region Deployable

    private void OnDeployableGetEntity(ModSuitDeployableGetPartEvent args)
    {
        if (args.Handled)
            return;

        var modSuit = GetEntity(args.ModSuit);

        if (!TryComp<ModSuitPartDeployableComponent>(modSuit, out var deployableComp))
            return;

        if (deployableComp.DeployedParts.TryGetValue(args.Slot, out var part))
        {
            args.Part = GetNetEntity(part);
            args.Handled = true;
            return;
        }

        if (!deployableComp.DeployableParts.TryGetValue(args.Slot, out var partId))
            return;

        part = Spawn(partId);
        deployableComp.DeployedParts.Add(args.Slot, part);
        args.Part = GetNetEntity(part);
        args.Handled = true;
    }

    #endregion

    #region Inventory

    private void OnDeployableInventoryBeforeEquipped(Entity<ModSuitDeployableInventoryComponent> ent, ref ModSuitDeployablePartBeforeEquippedEvent args)
    {
        ent.Comp.StoredItem = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
        _container.EmptyContainer(ent.Comp.StoredItem, true);

        if (!Inventory.TryGetSlotEntity(args.Wearer, args.Slot, out var slotEntity))
            return;

        Inventory.TryUnequip(args.Wearer, args.Slot, true, false, true);

        _container.Insert(slotEntity.Value, ent.Comp.StoredItem);
    }

    private void OnDeployableInventoryUnequipped(Entity<ModSuitDeployableInventoryComponent> ent, ref ModSuitDeployablePartUnequippedEvent args)
    {
        if (ent.Comp.StoredItem?.ContainedEntities.Any() != true)
            return;

        var items = _container.EmptyContainer(ent.Comp.StoredItem, true);

        Inventory.TryEquip(args.Wearer, items[0], args.Slot, true, true, true);
    }

    #endregion
}
