using Content.Shared.Actions;
using Content.Shared.Power;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    private void InitializeHacking()
    {
        SubscribeLocalEvent<StationAiCanHackComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationAiHackableComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnMapInit(EntityUid uid, StationAiCanHackComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, component.ActionId);
    }

    private void OnPowerChanged(EntityUid uid, StationAiHackableComponent component, ref PowerChangedEvent args)
    {
        component.IsPowered = args.Powered;
        Dirty(uid, component);
    }
}

public sealed partial class StationAiShopActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed class StationAiHackEvent : BaseStationAiAction
{
}
