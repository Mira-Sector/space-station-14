using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules.Modules;

public sealed partial class AddComponentContainerModuleSystem : BaseModuleSystem<AddComponentContainerModuleComponent>
{
    protected override void OnAdded(Entity<AddComponentContainerModuleComponent> ent, ref ModuleAddedContainerEvent args)
    {
        base.OnAdded(ent, ref args);
        EntityManager.AddComponents(args.Container, ent.Comp.Components, true);
    }

    protected override void OnRemoved(Entity<AddComponentContainerModuleComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        base.OnRemoved(ent, ref args);
        EntityManager.RemoveComponents(args.Container, ent.Comp.Components);
    }
}
