using Content.Shared.Actions;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules.Modules;

public abstract partial class BaseToggleableModuleSystem<T> : BaseModuleSystem<T> where T : BaseToggleableModuleComponent
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    [MustCallBase]
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, GetItemActionsEvent>(OnItemActions);

        SubscribeLocalEvent<T, ModuleToggleActionEvent>(OnAction);
        SubscribeLocalEvent<T, ModuleToggleAttemptEvent>(OnToggleAttempt);

        SubscribeLocalEvent<T, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<T, ModuleDisabledEvent>(OnDisabled);
    }

    [MustCallBase]
    protected override void OnRemoved(Entity<T> ent, ref ModuleRemovedContainerEvent args)
    {
        base.OnRemoved(ent, ref args);
        _actions.RemoveAction(ent.Comp.Action);
    }

    private void OnAction(Entity<T> ent, ref ModuleToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Container == null)
            return;

        var beforeEv = new ModuleToggleAttemptEvent(ent.Comp.Container.Value, args.Performer);
        RaiseLocalEvent(ent.Owner, beforeEv);

        if (beforeEv.Cancelled)
            return;

        if (ent.Comp.Toggled)
        {
            var ev = new ModuleDisabledEvent(ent.Comp.Container.Value, args.Performer);
            RaiseLocalEvent(ent.Owner, ev);
        }
        else
        {
            var ev = new ModuleEnabledEvent(ent.Comp.Container.Value, args.Performer);
            RaiseLocalEvent(ent.Owner, ev);
        }

        args.Handled = true;
    }

    protected virtual void OnToggleAttempt(Entity<T> ent, ref ModuleToggleAttemptEvent args)
    {
    }

    [MustCallBase]
    protected virtual void OnItemActions(Entity<T> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.ActionId != null)
            args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
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
}
