using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Silicons.StationAi;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;

    private void InitializePower()
    {
        SubscribeLocalEvent<StationAiRequirePowerComponent, PowerChangedEvent>(OnCorePowerChange);
        SubscribeLocalEvent<StationAiRequirePowerComponent, IntellicardAttemptEvent>(OnIntellicardAttempt);

    }

    private void UpdatePower(float frameTime)
    {
        var query = EntityQueryEnumerator<StationAiRequirePowerComponent, StationAiCoreComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var aiPower, out var core, out var battery))
        {
            if (aiPower.IsPowered)
            {
                if (!_battery.TryUseCharge(uid, aiPower.Wattage * frameTime, battery))
                {
                    aiPower.IsPowered = false;
                    TurnOff(uid, core);
                }
            }
        }
    }

    private void OnCorePowerChange(EntityUid uid, StationAiRequirePowerComponent component, ref PowerChangedEvent args)
    {
        UpdateState(uid, component);
    }

    private void OnIntellicardAttempt(EntityUid uid, StationAiRequirePowerComponent component, IntellicardAttemptEvent args)
    {
        if (component.IsPowered || args.Cancelled)
            return;

        args.Cancel();
        _popup.PopupEntity(Loc.GetString("base-computer-ui-component-not-powered", ("machine", uid)), args.User, args.User, PopupType.MediumCaution);
    }

    private void UpdateState(EntityUid uid, StationAiRequirePowerComponent component)
    {
        if (!TryComp<StationAiCoreComponent>(uid, out var core))
            return;

        if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            return;

        component.IsPowered = receiver.Powered;

        if (receiver.Powered)
        {
            TurnOn(uid, core);
        }
        else
        {
            TurnOff(uid, core);
        }
    }

    private void TurnOff(EntityUid uid, StationAiCoreComponent core)
    {
        ClearEye((uid, core));

        if (TryGetInsertedAI((uid, core), out var ai))
            EntityManager.DeleteEntity(ai);
    }

    private void TurnOn(EntityUid uid, StationAiCoreComponent core)
    {
        if (!SetupEye((uid, core)))
            return;

        AttachEye((uid, core));
    }
}
