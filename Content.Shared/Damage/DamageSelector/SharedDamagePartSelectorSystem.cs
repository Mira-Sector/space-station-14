using Content.Shared.Alert;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Damage.DamageSelector;

public abstract partial class SharedDamagePartSelectorSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamagePartSelectorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DamagePartSelectorComponent, ComponentRemove>(OnRemoved);

        SubscribeLocalEvent<DamagePartSelectorComponent, ShowDamagePartSelectorAlertEvent>(OnAlert);

        Subs.BuiEvents<DamagePartSelectorComponent>(DamageSelectorUiKey.Key, subs =>
        {
            subs.Event<DamageSelectorSystemMessage>(OnMessage);
        });

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.DamagePartSelector, InputCmdHandler.FromDelegate(SelectorKeybind, handle: false))
            .Register<SharedDamagePartSelectorSystem>();
    }

    private void OnInit(Entity<DamagePartSelectorComponent> ent, ref ComponentInit args)
    {
        _alerts.ShowAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnRemoved(Entity<DamagePartSelectorComponent> ent, ref ComponentRemove args)
    {
        _userInterface.CloseUi(ent.Owner, DamageSelectorUiKey.Key);
        _alerts.ClearAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnAlert(Entity<DamagePartSelectorComponent> ent, ref ShowDamagePartSelectorAlertEvent args)
    {
        if (args.Handled)
            return;

        _userInterface.OpenUi(ent.Owner, DamageSelectorUiKey.Key, true);
        args.Handled = true;
    }

    private void OnMessage(Entity<DamagePartSelectorComponent> ent, ref DamageSelectorSystemMessage args)
    {
        ent.Comp.SelectedPart = args.Part;
        Dirty(ent);

        _alerts.ShowAlert(ent.Owner, ent.Comp.Alert);
    }

    private void SelectorKeybind(ICommonSession? session = null)
    {
        if (GetKeybindEntity(session) is not { } uid)
            return;

        _userInterface.OpenUi(uid, DamageSelectorUiKey.Key, session!, true);
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
