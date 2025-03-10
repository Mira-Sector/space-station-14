using Content.Client.Atmos.Components;
using Content.Client.DisplacementMap;
using Content.Shared.Atmos.Piping;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;
using System.Diagnostics.CodeAnalysis;

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

        if (component.Offsets is {} &&
            component.OffsetLayers is {})
        {
            component.Offsets.TryGetValue(currentLayer, out var newOffset);
            component.Offsets.TryGetValue(component.LastLayer, out var oldOffset);

            foreach (var layerId in component.OffsetLayers)
            {
                if (!TryGetIndex(layerId, args.Sprite, out var index))
                    continue;

                if (!args.Sprite.TryGetLayer(index.Value, out var layer))
                    continue;

                layer.Offset -= oldOffset;
                layer.Offset += newOffset;
            }
        }

        component.RevealedLayers.Clear();
        component.LastLayer = currentLayer;

        // we still need to cleanup our previous layer
        if (component.Displacements is {} &&
            component.DisplacementLayers is {} &&
            component.Displacements.TryGetValue(currentLayer, out var displacement))
        {
            foreach (var layer in component.DisplacementLayers)
            {
                if (!TryGetIndex(layer, args.Sprite, out var index))
                    continue;

                _displacement.TryAddDisplacement(displacement, args.Sprite, index.Value, layer, component.RevealedLayers);
            }
        }
    }

    private bool TryGetIndex(string layer, SpriteComponent sprite, [NotNullWhen(true)] out int? index)
    {
        index = null;

        if (!sprite.LayerMapTryGet(layer, out var layerIndex))
        {
            if (!_refMan.TryParseEnumReference(layer, out var @enum, false))
                return false;

            if (!sprite.LayerMapTryGet(@enum, out layerIndex))
                return false;
        }

        index = layerIndex;
        return true;
    }
}
