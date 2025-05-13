using Content.Shared.Modules.ModSuit.Components;
using Robust.Shared.Containers;

namespace Content.Server.Modules.ModSuit;

public partial class ModSuitSystem
{
    protected override void OnDeployableInit(Entity<ModSuitPartDeployableComponent> ent, ref ComponentInit args)
    {
        foreach (var (slot, partId) in ent.Comp.DeployablePartIds)
        {
            var container = Container.EnsureContainer<ContainerSlot>(ent.Owner, GetDeployableSlotId(slot));

            var part = Spawn(partId);

            if (!Container.Insert(part, container))
            {
                Del(part);
                continue;
            }

            ent.Comp.DeployableContainers.Add(slot, container);
        }

        Dirty(ent);
    }
}
