using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableComponentContainerModuleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableComponentContainerModuleComponent, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<ToggleableComponentContainerModuleComponent, ModuleDisabledEvent>(OnDisabled);
    }

    private void OnEnabled(Entity<ToggleableComponentContainerModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        EntityManager.AddComponents(args.Container, ent.Comp.Components);
    }

    private void OnDisabled(Entity<ToggleableComponentContainerModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        EntityManager.RemoveComponents(args.Container, ent.Comp.Components);
    }
}
