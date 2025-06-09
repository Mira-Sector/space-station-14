using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private void InitializeHacking()
    {
        SubscribeLocalEvent<StationAiHackableComponent, GetStationAiRadialEvent>(OnHackableGetRadial);

        SubscribeLocalEvent<ShowHackingHUDComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShowHackingHUDComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShowHackingHUDComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ShowHackingHUDComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<HackableHUDVisualsComponent, AppearanceChangeEvent>(OnHUDAppearance);
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
                Event = new StationAiHackAttemptEvent()
            }
        );
    }

    private void OnPlayerAttached(EntityUid uid, ShowHackingHUDComponent component, ref LocalPlayerAttachedEvent args)
    {
        ShowHUD();
    }

    private void OnPlayerDetached(EntityUid uid, ShowHackingHUDComponent component, ref LocalPlayerDetachedEvent args)
    {
        RemoveHUD();
    }

    private void OnInit(EntityUid uid, ShowHackingHUDComponent component, ref ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            ShowHUD();
    }

    private void OnShutdown(EntityUid uid, ShowHackingHUDComponent component, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            RemoveHUD();
    }

    private void ShowHUD()
    {
        var query = AllEntityQuery<HackableHUDVisualsComponent, AppearanceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var _, out var appearance, out var sprite))
        {
            if (!_appearance.TryGetData<bool>(uid, HackingVisuals.Hacked, out var hacked, appearance))
                continue;

            if (hacked)
            {
                sprite.LayerSetVisible(HackingLayers.HUD, true);
            }
            else
            {
                sprite.LayerSetVisible(HackingLayers.HUD, false);
            }
        }
    }

    private void RemoveHUD()
    {
        var query = AllEntityQuery<HackableHUDVisualsComponent, AppearanceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var _, out var _, out var sprite))
        {
            sprite.LayerSetVisible(HackingLayers.HUD, false);
        }
    }

    private void OnHUDAppearance(EntityUid uid, HackableHUDVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, HackingVisuals.Hacked, out var hacked, args.Component))
            return;

        var player = _player.LocalEntity;
        if (hacked && HasComp<ShowHackingHUDComponent>(player))
        {
            args.Sprite.LayerSetVisible(HackingLayers.HUD, true);
        }
        else
        {
            args.Sprite.LayerSetVisible(HackingLayers.HUD, false);
        }
    }
}
