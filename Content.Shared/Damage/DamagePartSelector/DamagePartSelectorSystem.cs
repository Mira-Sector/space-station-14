using Content.Shared.Actions;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.DamageSelector;

public sealed class DamagePartSelectorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;


    private static readonly EntProtoId ActionId = "ActionDamageSelector";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamagePartSelectorComponent, MapInitEvent >(OnInit);
        SubscribeLocalEvent<DamagePartSelectorComponent, ComponentRemove>(OnRemoved);

        SubscribeLocalEvent<DamagePartSelectorComponent, DamageSelectorActionEvent>(OnAction);
        SubscribeLocalEvent<DamagePartSelectorComponent, DamageSelectorSystemMessage>(OnMessage);
    }

    private void OnInit(EntityUid uid, DamagePartSelectorComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.Action, ActionId, uid);
        Dirty(uid, component);
    }

    private void OnRemoved(EntityUid uid, DamagePartSelectorComponent component, ComponentRemove args)
    {
        _actions.RemoveAction(component.Action);
        Dirty(uid, component);
    }

    private void OnAction(EntityUid uid, DamagePartSelectorComponent component, InstantActionEvent args)
    {
        if (args.Handled || args.Performer != uid)
            return;

        if (!TryComp<ActorComponent>(uid, out var actorComp))
            return;

        _userInterface.TryToggleUi(uid, DamageSelectorUiKey.Key, actorComp.PlayerSession);
        args.Handled = true;
    }

    private void OnMessage(EntityUid uid, DamagePartSelectorComponent component, DamageSelectorSystemMessage args)
    {
        component.SelectedPart = args.Part;
        Dirty(uid, component);

        // update the actions icon
        if (component.Action == null)
            return;

        if (!TryComp<InstantActionComponent>(component.Action, out var actionComp))
            return;

        //enumerate over all of them because c# fuckery
        foreach ((var part, var sprite) in component.SelectableParts)
        {
            if (part.Type != args.Part.Type || part.Side != args.Part.Side)
                continue;

            actionComp.Icon = sprite;
            Dirty(component.Action.Value, actionComp);
            break;
        }
    }
}
