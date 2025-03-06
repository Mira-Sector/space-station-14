using Content.Client.Atmos.Components;
using Content.Client.DisplacementMap;
using Robust.Client.GameObjects;
using Content.Shared.Atmos.Piping;

namespace Content.Client.Atmos.EntitySystems;

public sealed class PipeLayerVisualizerSystem : VisualizerSystem<PipeLayerVisualsComponent>
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;

    protected override void OnAppearanceChange(EntityUid uid, PipeLayerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not {})
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, PipeLayerVisuals.Layer, out var currentLayer, args.Component))
            return;

        foreach (var layer in component.RevealedLayers)
            args.Sprite.RemoveLayer(layer);

        component.RevealedLayers.Clear();

        // we still need to cleanup our previous layer
        if (!component.Displacements.TryGetValue(currentLayer, out var displacement))
            return;

        foreach (var layerId in component.Layers)
        {
            if (!args.Sprite.LayerMapTryGet(layerId, out var index))
                continue;

            if (!args.Sprite.TryGetLayer(index, out var layer))
                continue;

            _displacement.TryAddDisplacement(displacement, args.Sprite, index, layer.State.Name!, component.RevealedLayers);
        }
    }
}
