using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public sealed partial class AddComponentContainerModuleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddComponentContainerModuleComponent, ModuleAddedContainerEvent>(OnAdded);
        SubscribeLocalEvent<AddComponentContainerModuleComponent, ModuleRemovedContainerEvent>(OnRemoved);
    }

    private void OnAdded(Entity<AddComponentContainerModuleComponent> ent, ref ModuleAddedContainerEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        EntityManager.AddComponents(args.Container, ent.Comp.Components);
    }

    private void OnRemoved(Entity<AddComponentContainerModuleComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        EntityManager.RemoveComponents(args.Container, ent.Comp.Components);
    }
}
