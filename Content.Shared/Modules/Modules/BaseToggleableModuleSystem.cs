using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using JetBrains.Annotations;

namespace Content.Shared.Modules.Modules;

public abstract partial class BaseToggleableModuleSystem<T> : BaseModuleSystem<T> where T : BaseToggleableModuleComponent
{
    [MustCallBase]
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<T, ModuleDisabledEvent>(OnDisabled);
    }

    [MustCallBase]
    protected virtual void OnEnabled(Entity<T> ent, ref ModuleEnabledEvent args)
    {
        ent.Comp.Toggled = true;
    }

    [MustCallBase]
    protected virtual void OnDisabled(Entity<T> ent, ref ModuleDisabledEvent args)
    {
        ent.Comp.Toggled = false;
    }

    [PublicAPI]
    public void Toggle(Entity<T> ent, EntityUid? user)
    {
        if (ent.Comp.Container == null)
            return;

        var beforeEv = new ModuleToggleAttemptEvent(!ent.Comp.Toggled, ent.Comp.Container.Value, user);
        RaiseLocalEvent(ent.Owner, beforeEv);

        if (beforeEv.Cancelled)
            return;

        if (ent.Comp.Toggled)
        {
            var ev = new ModuleDisabledEvent(ent.Comp.Container.Value, user);
            RaiseLocalEvent(ent.Owner, ev);
        }
        else
        {
            var ev = new ModuleEnabledEvent(ent.Comp.Container.Value, user);
            RaiseLocalEvent(ent.Owner, ev);
        }
    }
}
