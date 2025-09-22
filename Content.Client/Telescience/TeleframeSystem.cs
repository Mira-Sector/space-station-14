using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Telescience;

/// <summary>
/// <inheritdoc cref="SharedTeleframeSystem"/>
/// </summary>
public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TeleframeComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<TeleframeComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<TeleframeVisualState>(ent.Owner, TeleframeVisuals.VisualState, out var state, args.Component))
            state = TeleframeVisualState.Off;

        switch (state)
        {
            case TeleframeVisualState.On:
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.On, true);
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.Charging, false);
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.Recharging, false);
                break;
            case TeleframeVisualState.Charging:
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.On, false);
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.Charging, true);
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.Recharging, false);
                break;
            case TeleframeVisualState.Recharging:
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.On, false);
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.Charging, false);
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.Recharging, true);
                break;
            case TeleframeVisualState.Off:
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.On, false);
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.Charging, false);
                _sprite.LayerSetVisible((ent.Owner, args.Sprite), TeleframeVisualState.Recharging, false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
