using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private void InitializeShunting()
    {
        SubscribeLocalEvent<StationAiShuntingComponent,GetStationAiRadialEvent>(OnGetRadial);
    }

    private void OnGetRadial(EntityUid uid, StationAiShuntingComponent component, ref GetStationAiRadialEvent args)
    {
        if (!component.IsPowered || !component.Enabled)
            return;

        args.Actions.Add(
            new StationAiRadial()
            {
                Sprite = component.Icon,
                Tooltip = component.Tooltip,
                Event = new StationAiShuntingAttemptEvent()
            }
            );
    }
}
