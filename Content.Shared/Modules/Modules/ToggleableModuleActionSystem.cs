using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableModuleActionSystem : EntitySystem
{
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;
    [Dependency] private readonly ToggleableModuleSystem _toggleableModule = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleAddedContainerEvent>(OnAdded);
        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleRemovedContainerEvent>(OnRemoved);

        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleDisabledEvent>(OnDisabled);

        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleRelayedEvent<ClothingGotEquippedEvent>>(OnEquipped);
        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleRelayedEvent<ClothingGotUnequippedEvent>>(OnUnequipped);

        SubscribeLocalEvent<ToggleableModuleActionComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<ToggleableModuleActionComponent, ModuleToggleActionEvent>(OnAction);
    }

    private void OnAdded(Entity<ToggleableModuleActionComponent> ent, ref ModuleAddedContainerEvent args)
    {
        if (!_moduleContained.TryGetUser(ent.Owner, out var user))
            return;

        if (!_actions.AddAction(user.Value, ref ent.Comp.Action, ent.Comp.ActionId, ent.Owner))
            return;

        Dirty(ent);
    }

    private void OnRemoved(Entity<ToggleableModuleActionComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        _actions.RemoveAction(ent.Comp.Action);
    }

    private void OnEnabled(Entity<ToggleableModuleActionComponent> ent, ref ModuleEnabledEvent args)
    {
        _actions.SetToggled(ent.Comp.Action, true);
    }

    private void OnDisabled(Entity<ToggleableModuleActionComponent> ent, ref ModuleDisabledEvent args)
    {
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

        _toggleableModule.Toggle(ent.Owner, args.Performer);
        args.Handled = true;
    }
}
