using Content.Server.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Robust.Shared.Containers;

namespace Content.Server.Modules.ModSuit;

public partial class ModSuitSystem
{
    private void InitializeDeployable()
    {
        SubscribeAllEvent<ModSuitDeployableGetPartEvent>(OnDeployableGetPart);

        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ModSuitDeployablePartBeforeEquippedEvent>(OnDeployableInventoryBeforeEquipped);
        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ModSuitDeployablePartUnequippedEvent>(OnDeployableInventoryUnequipped);
    }

    #region Deployable

    private void OnDeployableGetPart(ModSuitDeployableGetPartEvent args)
    {
        if (args.Handled)
            return;

        var modSuit = GetEntity(args.ModSuit);

        if (!TryComp<ModSuitPartDeployableComponent>(modSuit, out var deployableComp))
            return;

        if (deployableComp.DeployableContainers.TryGetValue(args.Slot, out var container) && container.ContainedEntity is { } part)
        {
            args.Handled = true;
            args.Part = GetNetEntity(part);
            return;
        }

        if (!deployableComp.DeployablePartIds.TryGetValue(args.Slot, out var partId))
            return;

        part = Spawn(partId);
        args.Handled = true;
        args.Part = GetNetEntity(part);
    }

    #endregion

    #region Inventory

    private void OnDeployableInventoryBeforeEquipped(Entity<ModSuitDeployableInventoryComponent> ent, ref ModSuitDeployablePartBeforeEquippedEvent args)
    {
        ent.Comp.StoredItem = Container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);
        Container.EmptyContainer(ent.Comp.StoredItem, true);

        if (!Inventory.TryGetSlotEntity(args.Wearer, args.Slot, out var slotEntity))
            return;

        Inventory.TryUnequip(args.Wearer, args.Slot, true, true);
        Container.Insert(slotEntity.Value, ent.Comp.StoredItem);
    }

    private void OnDeployableInventoryUnequipped(Entity<ModSuitDeployableInventoryComponent> ent, ref ModSuitDeployablePartUnequippedEvent args)
    {
        if (ent.Comp.StoredItem?.ContainedEntity is not { } containedEntity)
            return;

        if (args.Wearer is not { } wearer)
        {
            Container.Remove(containedEntity, ent.Comp.StoredItem);
            return;
        }

        Inventory.TryEquip(wearer, containedEntity, args.Slot, true, true);
    }

    #endregion
}
