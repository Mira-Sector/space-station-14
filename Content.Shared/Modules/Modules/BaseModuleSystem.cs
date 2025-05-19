using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules.Modules;

public abstract partial class BaseModuleSystem<T> : EntitySystem where T : BaseModuleComponent
{
    [MustCallBase]
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ModuleAddedContainerEvent>(OnAdded);
        SubscribeLocalEvent<T, ModuleRemovedContainerEvent>(OnRemoved);
    }

    [MustCallBase]
    protected virtual void OnAdded(Entity<T> ent, ref ModuleAddedContainerEvent args)
    {
        ent.Comp.Container = args.Container;
    }

    [MustCallBase]
    protected virtual void OnRemoved(Entity<T> ent, ref ModuleRemovedContainerEvent args)
    {
        ent.Comp.Container = null;
    }
}
