using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Ghost;

public sealed class GhostVisualizerSystem : VisualizerSystem<GhostVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, GhostVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not {} sprite)
            return;

        if (!AppearanceSystem.TryGetData<Color>(uid, GhostVisuals.Color, out var color, args.Component))
            return;

        if (!args.Sprite.LayerMapTryGet(GhostVisuals.Layer, out var index))
            return;

        if (!args.Sprite.TryGetLayer(index, out var layer))
            return;

        layer.Color = color;
    }
}
