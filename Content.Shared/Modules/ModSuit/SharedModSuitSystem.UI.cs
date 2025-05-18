using Content.Shared.Actions;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using JetBrains.Annotations;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem Ui = default!;

    private void InitializeUI()
    {
        SubscribeLocalEvent<ModSuitUserInterfaceComponent, GetItemActionsEvent>(OnUiGetActions);
        SubscribeLocalEvent<ModSuitUserInterfaceComponent, ComponentRemove>(OnUiRemoved);
        SubscribeLocalEvent<ModSuitUserInterfaceComponent, ModSuitViewUiEvent>(OnUiViewUi);
        SubscribeLocalEvent<ModSuitUserInterfaceComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    private void OnUiGetActions(Entity<ModSuitUserInterfaceComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnUiRemoved(Entity<ModSuitUserInterfaceComponent> ent, ref ComponentRemove args)
    {
        _action.RemoveAction(ent.Comp.Action);
    }

    private void OnUiViewUi(Entity<ModSuitUserInterfaceComponent> ent, ref ModSuitViewUiEvent args)
    {
        if (args.Handled)
            return;

        Ui.TryToggleUi(ent.Owner, ModSuitUiKey.Key, args.Performer);
        args.Handled = true;
    }

    private void OnUiOpened(Entity<ModSuitUserInterfaceComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUI(ent.AsNullable());
    }

    [PublicAPI]
    public virtual void UpdateUI(Entity<ModSuitUserInterfaceComponent?> ent)
    {
    }
}
