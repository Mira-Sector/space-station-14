using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    private void InitializeHacking()
    {
        SubscribeLocalEvent<StationAiCanHackComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, StationAiCanHackComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, component.ActionId);
    }
}

public sealed partial class StationAiShopActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed class StationAiHackEvent : BaseStationAiAction
{
}

[Serializable, NetSerializable]
public enum HackingLayers : byte
{
    HUD
}

[Serializable, NetSerializable]
public enum HackingVisuals : byte
{
    Hacked
}
