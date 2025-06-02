using System.Diagnostics.CodeAnalysis;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using JetBrains.Annotations;

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

    [PublicAPI]
    public bool TryGetUser(Entity<T> ent, [NotNullWhen(true)] out EntityUid? user)
    {
        user = null;

        if (ent.Comp.Container == null)
            return false;

        var ev = new ModuleGetUserEvent();
        RaiseLocalEvent(ent.Comp.Container.Value, ev);

        user = ev.User;
        return ev.User != null;
    }
}
