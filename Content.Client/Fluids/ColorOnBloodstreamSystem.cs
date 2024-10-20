using Content.Shared.Fluids.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Fluids;

public sealed class ColorOnBloodstreamSystem : VisualizerSystem<ColorOnBloodstreamComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ColorOnBloodstreamComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }

        if (!AppearanceSystem.TryGetData<Color>(uid, BloodColor.Color, out var color, args.Component))
        {
           return;
        }

        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        foreach (var spriteLayer in args.Sprite.AllLayers)
        {
            sprite.Color = color;
        }
    }
}
