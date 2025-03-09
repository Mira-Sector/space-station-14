using Content.Client.SubFloor;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Layerable;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using System.Linq;

namespace Content.Client.Atmos.EntitySystems;

[UsedImplicitly]
public sealed class AtmosPipeAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeAppearanceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeAppearanceComponent, AppearanceChangeEvent>(OnAppearanceChanged, after: [typeof(SubFloorHideSystem)]);
    }

    private void OnInit(EntityUid uid, PipeAppearanceComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        var zLayers = GetZLayers(uid);

        foreach (PipeConnectionLayer layerKey in Enum.GetValues(typeof(PipeConnectionLayer)))
        {
            foreach (var zLayer in zLayers)
            {
                var key = layerKey + "_" + zLayer.ToString();

                sprite.LayerMapReserveBlank(key);
                var layer = sprite.LayerMapGet(key);
                sprite.LayerSetRSI(layer, component.Sprite.RsiPath);
                sprite.LayerSetState(layer, component.Sprite.RsiState);
                sprite.LayerSetDirOffset(layer, ToOffset(layerKey));
            }
        }
    }

    private void HideAllPipeConnection(Entity<SpriteComponent> ent)
    {
        var zLayers = GetZLayers(ent);

        foreach (PipeConnectionLayer layerKey in Enum.GetValues(typeof(PipeConnectionLayer)))
        {
            foreach (var zLayer in zLayers)
            {
                if (!ent.Comp.LayerMapTryGet(layerKey + "_" + zLayer.ToString(), out var key))
                    continue;

                var layer = ent.Comp[key];
                layer.Visible = false;
            }
        }
    }

    private void OnAppearanceChanged(EntityUid uid, PipeAppearanceComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.Visible)
        {
            // This entity is probably below a floor and is not even visible to the user -> don't bother updating sprite data.
            // Note that if the subfloor visuals change, then another AppearanceChangeEvent will get triggered.
            return;
        }

        if (!TryComp<PipeAppearanceComponent>(uid, out var pipeAppearanceComp))
        {
            HideAllPipeConnection((uid, args.Sprite));
            return;
        }

        if (!_appearance.TryGetData<Color>(uid, PipeColorVisuals.Color, out var color, args.Component))
            color = Color.White;

        // transform connected directions to local-coordinates
        foreach (var (zLayer, direction) in pipeAppearanceComp.ConnectedDirections)
        {
            var connectedDirections = direction.RotatePipeDirection(-Transform(uid).LocalRotation);

            foreach (PipeConnectionLayer layerKey in Enum.GetValues(typeof(PipeConnectionLayer)))
            {
                if (!args.Sprite.LayerMapTryGet(layerKey + "_" + zLayer.ToString(), out var key))
                    continue;

                var layer = args.Sprite[key];
                var dir = (PipeDirection) layerKey;
                var visible = connectedDirections.HasDirection(dir);

                layer.Visible &= visible;

                if (!visible)
                    continue;

                layer.Color = color;
            }
        }

        // set the rest of the zlayers to disabled
        foreach (var zLayer in GetZLayers(uid))
        {
            if (pipeAppearanceComp.ConnectedDirections.ContainsKey(zLayer))
                continue;

            foreach (PipeConnectionLayer layerKey in Enum.GetValues(typeof(PipeConnectionLayer)))
            {
                if (!args.Sprite.LayerMapTryGet(layerKey + "_" + zLayer.ToString(), out var key))
                    continue;

                var layer = args.Sprite[key];
                layer.Visible = false;
            }
        }
    }

    private IEnumerable<int> GetZLayers(EntityUid uid)
    {
        if (TryComp<PipeLayerableComponent>(uid, out var layerableComp))
        {
            var totalLayers = Math.Abs(layerableComp.MinLayer) + Math.Abs(layerableComp.MaxLayer);

            for (var i = 0; i <= totalLayers; i++)
                yield return (layerableComp.MinLayer + i);
        }
        else
        {
            yield return 0;
        }
    }

    private SpriteComponent.DirectionOffset ToOffset(PipeConnectionLayer layer)
    {
        return layer switch
        {
            PipeConnectionLayer.NorthConnection => SpriteComponent.DirectionOffset.Flip,
            PipeConnectionLayer.EastConnection => SpriteComponent.DirectionOffset.CounterClockwise,
            PipeConnectionLayer.WestConnection => SpriteComponent.DirectionOffset.Clockwise,
            _ => SpriteComponent.DirectionOffset.None,
        };
    }

    private enum PipeConnectionLayer : byte
    {
        NorthConnection = PipeDirection.North,
        SouthConnection = PipeDirection.South,
        EastConnection = PipeDirection.East,
        WestConnection = PipeDirection.West,
    }
}
