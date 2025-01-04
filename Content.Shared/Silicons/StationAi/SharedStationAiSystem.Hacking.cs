using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    private void InitializeHacking()
    {
        SubscribeLocalEvent<StationAiCanHackComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationAiHackableComponent, StationAiHackAttemptEvent>(OnHackAttempt);
    }

    private void OnMapInit(EntityUid uid, StationAiCanHackComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, component.ActionId);
    }

    private void OnHackAttempt(EntityUid uid, StationAiHackableComponent component, StationAiHackAttemptEvent args)
    {
        if (!component.Enabled || !component.IsPowered || component.Hacked)
            return;

        if (!HasComp<StationAiCanHackComponent>(args.User))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.Delay, new StationAiHackDoAfterEvent(), uid, uid));
    }
}

public sealed partial class StationAiShopActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class StationAiHackAttemptEvent : BaseStationAiAction;

[Serializable, NetSerializable]
public sealed partial class StationAiHackDoAfterEvent : SimpleDoAfterEvent;

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
