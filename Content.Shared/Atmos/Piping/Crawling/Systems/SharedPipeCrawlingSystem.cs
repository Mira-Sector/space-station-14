using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Physics;
using Content.Shared.SubFloor;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public sealed class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    const CollisionGroup PipeCollision = CollisionGroup.PipeCrawling;
    const string PipeCollisionName = "pipe";
    const float PipeCollisionRadius = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingComponent, ComponentRemove>(OnRemoved);
    }

    private void OnInit(EntityUid uid, PipeCrawlingComponent component, ref ComponentInit args)
    {
        SetState(uid, component, true);
    }

    private void OnRemoved(EntityUid uid, PipeCrawlingComponent component, ref ComponentRemove args)
    {
        SetState(uid, component, false);
    }

    private void SetState(EntityUid uid, PipeCrawlingComponent component, bool enabled)
    {
        if (!TryComp<FixturesComponent>(uid, out var playerFixturesComp))
            return;

        foreach ((var fixtureId, var fixture) in playerFixturesComp.Fixtures)
        {
            if (fixtureId == PipeCollisionName)
                continue;

            if (enabled)
            {
                component.OriginalCollision.Add(fixtureId, fixture.Hard);
                _physics.SetHard(uid, fixture, !enabled);
            }
            else if (component.OriginalCollision.ContainsKey(fixtureId))
            {
                _physics.SetHard(uid, fixture, component.OriginalCollision[fixtureId]);
            }
        }

        if (playerFixturesComp.Fixtures.ContainsKey(PipeCollisionName))
            _physics.SetHard(uid, playerFixturesComp.Fixtures[PipeCollisionName], enabled);
        else
            Log.Warning($"{ToPrettyString(uid)} does not have a {PipeCollisionName} fixture!");

        var trayComp = EnsureComp<TrayScannerComponent>(uid);
        trayComp.EnabledEntity = true;
        trayComp.Enabled = true;
    }
}
