using Content.Shared.Silicons.StationAi;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : SharedBatteryWeaponFireModesSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetStationAiRadialEvent>(OnGetRadial);
    }

    private void OnGetRadial(EntityUid uid, BatteryWeaponFireModesComponent component, ref GetStationAiRadialEvent args)
    {
        if (!component.AiInteract)
            return;

        if (component.FireModes.Count < 2)
            return;

        var (nextFiremode, nextIndex) = GetNextFireMode(component);

        if (!_prototypeManager.TryIndex<EntityPrototype>(nextFiremode.Prototype, out var prototype))
            return;

        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = nextFiremode.Icon,
                Tooltip = Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)),
                Event = new StationAiFireModeChangeEvent()
                {
                    FireMode = nextIndex
                }
            }
        );
    }
}
