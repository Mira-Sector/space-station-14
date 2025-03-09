using Content.Client.Atmos.Components;
using Content.Client.DisplacementMap;
using Content.Shared.Atmos.Piping;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client.Atmos.EntitySystems;

public sealed class PipeLayerVisualizerSystem : VisualizerSystem<PipeLayerVisualsComponent>
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly IReflectionManager _refMan = default!;

    protected override void OnAppearanceChange(EntityUid uid, PipeLayerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not {})
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, PipeLayerVisuals.Layer, out var currentLayer, args.Component))
            return;

        foreach (var layer in component.RevealedLayers)
            args.Sprite.LayerMapRemove(layer);

        if (component.ChangeDrawDepth)
        {
            // reset it back to what it was
            args.Sprite.DrawDepth -= component.LastLayer;
            args.Sprite.DrawDepth += currentLayer;
        }

        component.RevealedLayers.Clear();
        component.LastLayer = currentLayer;

        // we still need to cleanup our previous layer
        if (!component.Displacements.TryGetValue(currentLayer, out var displacement))
            return;

        foreach (var layer in component.Layers)
        {
            if (!args.Sprite.LayerMapTryGet(layer, out var index))
            {
                if (!_refMan.TryParseEnumReference(layer, out var @enum, false))
                    continue;

                if (!args.Sprite.LayerMapTryGet(@enum, out index))
                    continue;
            }

            _displacement.TryAddDisplacement(displacement, args.Sprite, index, layer, component.RevealedLayers);
        }
    }
}
