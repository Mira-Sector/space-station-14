using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableModuleActionSystem : BaseToggleableUiModuleSystem<ToggleableModuleActionComponent>
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleRelayedEvent<ClothingGotEquippedEvent>>(OnEquipped);
        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleRelayedEvent<ClothingGotUnequippedEvent>>(OnUnequipped);

        SubscribeLocalEvent<ToggleableModuleActionComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleToggleActionEvent>(OnAction);
    }

    protected override void OnAdded(Entity<ToggleableModuleActionComponent> ent, ref ModuleAddedContainerEvent args)
    {
        base.OnAdded(ent, ref args);

        if (!TryGetUser(ent, out var user))
            return;

        if (!_actions.AddAction(user.Value, ref ent.Comp.Action, ent.Comp.ActionId, ent.Owner))
            return;

        Dirty(ent);
    }

    protected override void OnRemoved(Entity<ToggleableModuleActionComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        base.OnRemoved(ent, ref args);
        _actions.RemoveAction(ent.Comp.Action);
    }

    protected override void OnEnabled(Entity<ToggleableModuleActionComponent> ent, ref ModuleEnabledEvent args)
    {
        base.OnEnabled(ent, ref args);

        _actions.SetToggled(ent.Comp.Action, true);
    }

    protected override void OnDisabled(Entity<ToggleableModuleActionComponent> ent, ref ModuleDisabledEvent args)
    {
        base.OnDisabled(ent, ref args);

        _actions.SetToggled(ent.Comp.Action, false);
    }

    private void OnEquipped(Entity<ToggleableModuleActionComponent> ent, ref ModuleRelayedEvent<ClothingGotEquippedEvent> args)
    {
        if (!_actions.AddAction(args.Args.Wearer, ref ent.Comp.Action, ent.Comp.ActionId, ent.Owner))
            return;

        Dirty(ent);
    }

    private void OnUnequipped(Entity<ToggleableModuleActionComponent> ent, ref ModuleRelayedEvent<ClothingGotUnequippedEvent> args)
    {
        _actions.RemoveAction(ent.Comp.Action);
        Dirty(ent);
    }

    private void OnRemove(Entity<ToggleableModuleActionComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.Action);
    }

    private void OnAction(Entity<ToggleableModuleActionComponent> ent, ref ModuleToggleActionEvent args)
    {
        if (args.Handled)
            return;

        Toggle(ent.AsNullable(), args.Performer);
        args.Handled = true;
    }
}
