using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private void InitializeTurret()
    {
        SubscribeLocalEvent<StationAiTurretComponent, GetStationAiRadialEvent>(OnGetRadial);
    }

    private void OnGetRadial(EntityUid uid, StationAiTurretComponent component, ref GetStationAiRadialEvent args)
    {
        if (component.Modes.Count < 2)
            return;

        var (nextMode, nextIndex) = GetNextMode(component);

        var tooltip = nextMode.Factions != null
            ? Loc.GetString("ai-turret-faction-change", ("faction", Loc.GetString(nextMode.Tooltip)))
            : Loc.GetString(nextMode.Tooltip);

        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = nextMode.Icon,
                Tooltip = tooltip,
                Event = new StationAiTurretEvent()
                {
                    Mode = nextIndex,
                }
            }
        );
    }
}
