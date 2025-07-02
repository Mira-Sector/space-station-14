using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Damage.DamageSelector;

public sealed class DamagePartSelectorSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamagePartSelectorComponent, ComponentRemove>(OnRemoved);

        Subs.BuiEvents<DamagePartSelectorComponent>(DamageSelectorUiKey.Key, subs =>
        {
            subs.Event<DamageSelectorSystemMessage>(OnMessage);
        });

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.DamagePartSelector, InputCmdHandler.FromDelegate(EnableSelectorKeybind, DisableSelectorKeybind, handle: false))
            .Register<DamagePartSelectorSystem>();
    }

    private void OnRemoved(Entity<DamagePartSelectorComponent> ent, ref ComponentRemove args)
    {
        _userInterface.CloseUi(ent.Owner, DamageSelectorUiKey.Key);
    }

    private void OnMessage(Entity<DamagePartSelectorComponent> ent, ref DamageSelectorSystemMessage args)
    {
        ent.Comp.SelectedPart = args.Part;
        Dirty(ent);

        // update the actions icon
        /*
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
        */
    }

    private void EnableSelectorKeybind(ICommonSession? session = null)
    {
        if (GetKeybindEntity(session) is not { } uid)
            return;

        _userInterface.OpenUi(uid, DamageSelectorUiKey.Key, session!);
    }

    private void DisableSelectorKeybind(ICommonSession? session = null)
    {
        if (GetKeybindEntity(session) is not { } uid)
            return;

        _userInterface.OpenUi(uid, DamageSelectorUiKey.Key, session!);
    }

    private EntityUid? GetKeybindEntity(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } uid)
            return null;

        if (!HasComp<DamagePartSelectorComponent>(uid))
            return null;

        return uid;
    }
}
