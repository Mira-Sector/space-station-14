using Content.Shared.Light;
using Content.Shared.Light.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Light;

public sealed class LightColorCycleSystem : SharedLightColorCycleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightColorCycleComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, LightColorCycleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<string>(uid, LightColorCycleVisuals.State, out var state))
            state = component.UnpoweredState;

        args.Sprite.LayerSetState(LightColorCycleLayers.Base, state);
    }
}
