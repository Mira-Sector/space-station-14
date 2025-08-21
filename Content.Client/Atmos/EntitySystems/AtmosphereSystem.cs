using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;

namespace Content.Client.Atmos.EntitySystems;

public sealed class AtmosphereSystem : SharedAtmosphereSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapAtmosphereComponent, ComponentHandleState>(OnMapHandleState);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var spaceWind = EntityQueryEnumerator<MovedByPressureComponent, TransformComponent, PhysicsComponent>();
        while (spaceWind.MoveNext(out var uid, out var moved, out var xform, out var physics))
            UpdateSpaceWindMovableEntity((uid, moved, xform, physics), frameTime);
    }

    private void OnMapHandleState(EntityUid uid, MapAtmosphereComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MapAtmosphereComponentState state)
            return;

        // Struct so should just copy by value.
        component.OverlayData = state.Overlay;
    }
}
