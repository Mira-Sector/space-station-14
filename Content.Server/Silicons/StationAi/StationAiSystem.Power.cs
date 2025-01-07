using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Player;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;

    private void InitializePower()
    {
        SubscribeLocalEvent<StationAiRequirePowerComponent, PowerChangedEvent>(OnCorePowerChange);
        SubscribeLocalEvent<StationAiRequirePowerComponent, IntellicardAttemptEvent>(OnIntellicardAttempt);
        SubscribeLocalEvent<StationAiRequirePowerComponent, StationAiShuntingEjectAttemptEvent>(OnShuntingAttempt);
    }

    private void UpdatePower(float frameTime)
    {
        var query = EntityQueryEnumerator<StationAiRequirePowerComponent, StationAiCoreComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var aiPower, out var core, out var battery))
        {
            if (aiPower.IsPowered)
            {
                if (!TryComp<ApcPowerReceiverComponent>(uid, out var power))
                    continue;

                if (_battery.IsFull(uid, battery))
                    continue;

                var inputCharge = power.Load > power.PowerReceived ? power.PowerReceived : power.Load;
                var newCharge = battery.CurrentCharge + inputCharge * frameTime;
                _battery.SetCharge(uid, newCharge, battery);
            }
            else
            {
                if (!_battery.TryUseCharge(uid, aiPower.Wattage * frameTime, battery))
                {
                    aiPower.IsPowered = false;
                    TurnOff(uid, core);
                    _battery.SetCharge(uid, 0f, battery);
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

    private void OnShuntingAttempt(EntityUid uid, StationAiRequirePowerComponent component, StationAiShuntingEjectAttemptEvent args)
    {
        if (component.IsPowered || args.Cancelled)
            return;

        args.Cancel();
    }

    private void UpdateState(EntityUid uid, StationAiRequirePowerComponent component)
    {
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            return;

        if (receiver.Powered == component.IsPowered)
            return;

        if (!TryComp<StationAiCoreComponent>(uid, out var core))
            return;

        component.IsPowered = receiver.Powered;

        if (receiver.Powered)
        {
            TurnOn(uid, core);
        }
        else
        {
            if (_timing.CurTime < component.LastWarning + component.WarningDelay)
                return;

            if (!TryGetInsertedAI((uid, core), out var ai))
                return;

            var msg = Loc.GetString("ai-power-warning");
            AnnounceAi(ai.Value, msg, component.WarningSound);

            component.LastWarning = _timing.CurTime;

            // dont run turn off now as it will instantly kill the ai
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
