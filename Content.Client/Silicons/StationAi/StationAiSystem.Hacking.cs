using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private void InitializeHacking()
    {
        SubscribeLocalEvent<StationAiHackableComponent , GetStationAiRadialEvent>(OnHackableGetRadial);
    }

    private void OnHackableGetRadial(EntityUid uid, StationAiHackableComponent component, ref GetStationAiRadialEvent args)
    {
        if (!component.Enabled || component.Hacked)
            return;

        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = component.RadialSprite,
                Tooltip = component.RadialTooltip,
                Event = new StationAiHackEvent()
            }
        );
    }
}
