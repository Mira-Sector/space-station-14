using Content.Server.Power.Components;
using Content.Shared.Silicons.StationAi;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private void InitializeShunting()
    {
        SubscribeLocalEvent<StationAiShuntingComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnChargeChanged(EntityUid uid, StationAiShuntingComponent component, ref ChargeChangedEvent args)
    {
        var isPowered = args.Charge > 0;

        OnPowerChange(uid, component, isPowered);
    }
}

