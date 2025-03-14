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

        if (AppearanceSystem.TryGetData<Color>(uid, GhostVisuals.Color, out var color, args.Component))
        {
            foreach (var layer in sprite.AllLayers)
                layer.Color = color;
        }

        foreach (var layerId in component.LayersToTransfer)
        {
            if (!AppearanceSystem.TryGetData<SpriteSpecifier>(uid, layerId, out var layerSprite, args.Component))
                continue;

            if (args.Sprite.LayerMapTryGet(layerId, out var index))
            {
                if (!args.Sprite.TryGetLayer(index, out var layer))
                    continue;
            }
            else
            {
                index = args.Sprite.LayerMapReserveBlank(layerId);
            }

            args.Sprite.AddLayer(layerSprite, index);
        }

        foreach (var marking in component.Markings)
        {
            if (!AppearanceSystem.TryGetData<SpriteSpecifier>(uid, marking, out var markingSprite, args.Component))
                continue;

            if (args.Sprite.LayerMapTryGet(marking, out var index))
            {
                if (!args.Sprite.TryGetLayer(index, out var layer))
                    continue;
            }
            else
            {
                index = args.Sprite.LayerMapReserveBlank(marking);
            }

            args.Sprite.AddLayer(markingSprite, index);
        }
    }
}
