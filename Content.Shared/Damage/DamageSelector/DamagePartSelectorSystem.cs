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

        SubscribeLocalEvent<DamagePartSelectorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DamagePartSelectorComponent, ComponentRemove>(OnRemoved);

        SubscribeLocalEvent<DamagePartSelectorComponent, DamageSelectorActionEvent>(OnAction);

        Subs.BuiEvents<DamagePartSelectorComponent>(DamageSelectorUiKey.Key, subs =>
        {
            subs.Event<DamageSelectorSystemMessage>(OnMessage);
        });
    }

    private void OnMapInit(Entity<DamagePartSelectorComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.Action, ActionId, ent.Owner);
        Dirty(ent);
    }

    private void OnRemoved(Entity<DamagePartSelectorComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.Action);
        Dirty(ent);
    }

    private void OnAction(Entity<DamagePartSelectorComponent> ent, ref DamageSelectorActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        if (!TryComp<ActorComponent>(ent.Owner, out var actorComp))
            return;

        _userInterface.TryToggleUi(ent.Owner, DamageSelectorUiKey.Key, actorComp.PlayerSession);
        args.Handled = true;
    }

    private void OnMessage(Entity<DamagePartSelectorComponent> ent, ref DamageSelectorSystemMessage args)
    {
        ent.Comp.SelectedPart = args.Part;
        Dirty(ent);

        // update the actions icon
        if (ent.Comp.Action == null)
            return;

        //enumerate over all of them because c# fuckery
        foreach (var data in ent.Comp.SelectableParts)
        {
            if (data.BodyPart.Type != args.Part.Type || data.BodyPart.Side != args.Part.Side)
                continue;

            _actions.SetIcon(ent.Comp.Action.Value, data.Sprite);
            break;
        }
    }
}
