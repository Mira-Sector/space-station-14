using Content.Shared.Glasses;
using Content.Shared.Inventory.Events;
using Content.Client.Overlays;
using Robust.Client.Graphics;
using System.Linq;

namespace Content.Client.Glasses;

public sealed class GlassesOverlaySystem : EquipmentHudSystem<GlassesOverlayComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private GlassesOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<GlassesOverlayComponent> args)
    {
        base.UpdateInternal(args);

        if (!_overlayMan.HasOverlay<GlassesOverlay>())
            _overlayMan.AddOverlay(_overlay);

        _overlay.Providers = args.Components.ToHashSet();
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
