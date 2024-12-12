using Content.Shared.Actions;
using Content.Shared.UserInterface;

namespace Content.Shared.Instruments;

public abstract class SharedHeadphonesSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HeadphonesComponent, ActivatableUIOpenAttemptEvent>(OnUiAttempt);
    }

    private void OnUiAttempt(EntityUid uid, HeadphonesComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || component.IsWorn)
            return;

        args.Cancel();
    }

    public void Equip(EntityUid uid, EntityUid wearer, HeadphonesComponent component, SharedInstrumentComponent instrument)
    {
        instrument.Player = wearer;
        Dirty(uid, instrument);

        component.Action = _actions.AddAction(wearer, component.ActionId, uid);

        component.IsWorn = true;
        Dirty(uid, component);
    }

    public void Unequip(EntityUid uid, HeadphonesComponent component, SharedInstrumentComponent instrument)
    {
        instrument.Player = null;
        Dirty(uid, instrument);

        _actions.RemoveAction(component.Action);
        component.Action = null;

        component.IsWorn = false;
        Dirty(uid, component);
    }
}
