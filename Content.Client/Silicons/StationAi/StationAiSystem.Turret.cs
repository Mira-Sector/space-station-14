using Content.Shared.Silicons.StationAi;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const string AnimationKey = "turret";

    private void InitializeTurret()
    {
        SubscribeLocalEvent<StationAiTurretComponent, GetStationAiRadialEvent>(OnGetRadial);
        SubscribeLocalEvent<StationAiTurretVisualsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StationAiTurretVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
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

    private void OnInit(EntityUid uid, StationAiTurretVisualsComponent component, ComponentInit args)
    {
        component.OpeningAnimation = new Animation()
        {
            Length = component.OpeningTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = TurretVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(component.OpeningSpriteState, 0f)
                    }
                }
            }
        };

        component.ClosingAnimation = new Animation()
        {
            Length = component.ClosingTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = TurretVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(component.ClosedSpriteState, 0f)
                    }
                }
            }
        };
    }

    private void OnAppearanceChange(EntityUid uid, StationAiTurretVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not {} sprite)
            return;

        if (!sprite.LayerMapTryGet(TurretVisualLayers.Base, out var baseLayer) ||
            !sprite.LayerMapTryGet(TurretVisualLayers.Turret, out var turretLayer))
            return;

        if (!_appearance.TryGetData<TurretState>(uid, TurretVisuals.State, out var state, args.Component))
            return;

        args.Sprite.LayerSetVisible(baseLayer, true);

        EnsureComp<AnimationPlayerComponent>(uid, out var animation);

        if (_animation.HasRunningAnimation(uid, animation, AnimationKey))
            _animation.Stop(uid, animation, AnimationKey);

        switch (state)
        {
            case TurretState.Open:
            {
                args.Sprite.LayerSetVisible(turretLayer, true);
                break;
            }
            case TurretState.Closed:
            {
                args.Sprite.LayerSetVisible(turretLayer, false);
                break;
            }
            case TurretState.Opening:
            {
                args.Sprite.LayerSetVisible(turretLayer, false);
                _animation.Play(uid, component.OpeningAnimation, AnimationKey);
                break;
            }
            case TurretState.Closing:
            {
                args.Sprite.LayerSetVisible(turretLayer, false);
                _animation.Play(uid, component.ClosingAnimation, AnimationKey);
                break;
            }
        }
    }
}
