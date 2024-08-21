using Content.Shared.Vehicles;
using Robust.Client.GameObjects;

namespace Content.Server.Vehicles;

public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprites = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, VehicleComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, VehicleState.Animated, out bool animated))
            return;

        if (!_appearance.TryGetData<bool>(uid, VehicleState.DrawOver, out bool depth))
            return;

        if (!TryComp<SpriteComponent>(uid, out var spriteComp))
            return;

        if (depth)
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.OverMobs;
        }
        else
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;
        }
        spriteComp.LayerSetAutoAnimated(0, animated);
    }
}
