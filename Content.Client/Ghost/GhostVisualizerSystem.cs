using Content.Client.DisplacementMap;
using Content.Shared.Ghost;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.Client.Ghost;

public sealed class GhostVisualizerSystem : VisualizerSystem<GhostVisualsComponent>
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    protected override void OnAppearanceChange(EntityUid uid, GhostVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not {} sprite)
            return;

        if (AppearanceSystem.TryGetData<Color>(uid, GhostVisuals.Color, out var color, args.Component))
            SetColor(sprite, color);

        foreach (var (layerId, displacement) in component.LayerDisplacements)
        {
            if (!_reflection.TryParseEnumReference(layerId, out var @enum))
                continue;

            if (!AppearanceSystem.TryGetData(uid, @enum, out _, args.Component))
                continue;

            if (!component.LayersModified.TryGetValue(@enum, out var markings))
                continue;

            foreach (var markingId in markings)
            {
                if (!_prototype.TryIndex<MarkingPrototype>(markingId, out var marking))
                    continue;


                foreach (var markingLayer in marking.Sprites)
                {
                    if (markingLayer is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var markingLayerId = $"{markingId}-{rsi.RsiState}";

                    if (!sprite.LayerMapTryGet(markingLayerId, out var index))
                        continue;

                    _displacement.TryAddDisplacement(displacement, sprite, index, markingLayerId, component.RevealedLayers);
                }
            }
        }
    }

    private void SetColor(SpriteComponent sprite, Color color)
    {
        if (!sprite.LayerMapTryGet(GhostVisuals.Layer, out var index))
            return;

        if (!sprite.TryGetLayer(index, out var layer))
            return;

        layer.Color = color;
    }
}
