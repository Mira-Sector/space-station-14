using Content.Shared.Actions;
using Content.Shared.Intents.Events;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Intents;

public sealed partial class IntentSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntentsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IntentsComponent, IntentActionEvent>(OnAction);
        SubscribeLocalEvent<IntentsComponent, IntentChangeMessage>(OnIntentChange);
    }

    private void OnMapInit(Entity<IntentsComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.SelectionAction, ent.Comp.SelectionActionId);
        DirtyField(ent.Owner, ent.Comp, nameof(IntentsComponent.SelectionAction));
        UpdateAction(ent);
    }

    private void OnAction(Entity<IntentsComponent> ent, ref IntentActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        if (!TryComp<ActorComponent>(ent, out var actorComp))
            return;

        _userInterface.TryToggleUi(ent.Owner, IntentUiKey.Key, actorComp.PlayerSession);
        args.Handled = true;
    }

    private void OnIntentChange(Entity<IntentsComponent> ent, ref IntentChangeMessage args)
    {
        if (ent.Comp.SelectedIntent == args.Intent)
            return;

        var changedEv = new IntentChangedEvent(args.Intent, ent.Comp.SelectedIntent);
        RaiseLocalEvent(ent, changedEv);

        var newIntent = _prototype.Index(args.Intent);
        var oldIntent = _prototype.Index(ent.Comp.SelectedIntent);

        var activatedEv = newIntent.ActivatedEvent;
        RaiseLocalEvent(ent, (object)activatedEv);

        var deactivatedEv = oldIntent.DeactivatedEvent;
        RaiseLocalEvent(ent, (object)deactivatedEv);

        ent.Comp.SelectedIntent = args.Intent;
        DirtyField(ent.Owner, ent.Comp, nameof(IntentsComponent.SelectedIntent));

        UpdateAction(ent);
    }

    private void UpdateAction(Entity<IntentsComponent> ent)
    {
        if (!_prototype.TryIndex(ent.Comp.SelectedIntent, out var intent))
            return;

        if (!_actions.TryGetActionData(ent.Comp.SelectionAction, out var action))
            return;

        action.Icon = intent.Icon;
        _metaData.SetEntityName(ent.Comp.SelectionAction.Value, Loc.GetString(intent.Name));
    }

    [PublicAPI]
    public bool TryGetIntent(Entity<IntentsComponent?> ent, [NotNullWhen(true)] out ProtoId<IntentPrototype>? intentId)
    {
        intentId = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        intentId = ent.Comp.SelectedIntent;
        return true;
    }
}
