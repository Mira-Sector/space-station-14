using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableComponentUserModuleSystem : BaseToggleableModuleSystem<ToggleableComponentUserModuleComponent>
{
    protected override void OnEnabled(Entity<ToggleableComponentUserModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        base.OnEnabled(ent, ref args);

        if (!TryGetUser(ent, out var user))
            return;

        EntityManager.AddComponents(user.Value, ent.Comp.Components, true);
    }

    protected override void OnDisabled(Entity<ToggleableComponentUserModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        base.OnDisabled(ent, ref args);

        if (!TryGetUser(ent, out var user))
            return;

        EntityManager.RemoveComponents(user.Value, ent.Comp.Components);
    }
}
