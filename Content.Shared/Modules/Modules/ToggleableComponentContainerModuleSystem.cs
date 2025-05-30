using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableComponentContainerModuleSystem : BaseToggleableModuleSystem<ToggleableComponentContainerModuleComponent>
{
    protected override void OnEnabled(Entity<ToggleableComponentContainerModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        base.OnEnabled(ent, ref args);
        EntityManager.AddComponents(args.Container, ent.Comp.Components);
    }

    protected override void OnDisabled(Entity<ToggleableComponentContainerModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        base.OnDisabled(ent, ref args);
        EntityManager.RemoveComponents(args.Container, ent.Comp.Components);
    }
}
