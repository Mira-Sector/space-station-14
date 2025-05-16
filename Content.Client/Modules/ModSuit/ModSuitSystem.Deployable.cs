using Content.Shared.Modules.ModSuit.Components;
using Robust.Shared.Containers;

namespace Content.Client.Modules.ModSuit;

public partial class ModSuitSystem
{
    protected override void OnDeployableInit(Entity<ModSuitPartDeployableComponent> ent, ref ComponentInit args)
    {
        foreach (var (slot, _) in ent.Comp.DeployablePartIds)
        {
            var container = Container.EnsureContainer<ContainerSlot>(ent.Owner, GetDeployableSlotId(slot));
            ent.Comp.DeployableContainers.Add(slot, container);
        }
    }
}
