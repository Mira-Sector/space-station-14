using Content.Shared.Ghost;
using Robust.Client.GameObjects;

namespace Content.Client.Ghost;

public sealed class GhostVisualizerSystem : VisualizerSystem<GhostVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, GhostVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not {} sprite)
            return;

        if (AppearanceSystem.TryGetData<Color>(uid, GhostVisuals.Color, out var color, args.Component))
        {
            foreach (var layer in sprite.AllLayers)
                layer.Color = color;
        }
    }
}
