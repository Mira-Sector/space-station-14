using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.SubFloor;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public sealed class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    Vector2 Offset = new Vector2(0.5f, 0.5f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingComponent, ComponentRemove>(OnRemoved);

        SubscribeLocalEvent<PipeCrawlingComponent, MoveInputEvent>(OnMove);
    }

    private void OnInit(EntityUid uid, PipeCrawlingComponent component, ref ComponentInit args)
    {
        SetState(uid, component, true);

        if (!TryComp<InputMoverComponent>(uid, out var inputComp))
            return;

        inputComp.CanMove = false;
    }

    private void OnRemoved(EntityUid uid, PipeCrawlingComponent component, ref ComponentRemove args)
    {
        SetState(uid, component, false);

        if (!TryComp<InputMoverComponent>(uid, out var inputComp))
            return;

        inputComp.CanMove = true;
    }

    private void SetState(EntityUid uid, PipeCrawlingComponent component, bool enabled)
    {
        if (!TryComp<FixturesComponent>(uid, out var playerFixturesComp))
            return;

        foreach ((var fixtureId, var fixture) in playerFixturesComp.Fixtures)
        {
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

        var trayComp = EnsureComp<TrayScannerComponent>(uid);
        trayComp.EnabledEntity = true;
        trayComp.Enabled = true;
    }

    private void OnMove(EntityUid uid, PipeCrawlingComponent component, ref MoveInputEvent args)
    {
        if (!TryComp<PipeCrawlingPipeComponent>(component.CurrentPipe, out var pipeComp))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(uid, out var speedComp))
            return;

        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.ResetDynamics(uid, physics);

        if (component.NextMoveAttempt > _timing.CurTime)
        {
            ResetPosition(uid, component);
            return;
        }

        component.NextMoveAttempt = _timing.CurTime + TimeSpan.FromSeconds(1f / speedComp.BaseSprintSpeed);

        var direction = Transform(uid).LocalRotation.GetDir();

        // does the pipe has a connection to annother pipe in that direction
        if (!pipeComp.ConnectedPipes.ContainsKey(direction))
        {
            ResetPosition(uid, component);
            return;
        }

        component.CurrentPipe = pipeComp.ConnectedPipes[direction];
        ResetPosition(uid, component);
        Dirty(uid, component);
    }

    private void ResetPosition(EntityUid uid, PipeCrawlingComponent component)
    {
        _xform.TryGetGridTilePosition(component.CurrentPipe, out var pipePos);
        _xform.SetLocalPositionNoLerp(uid, Vector2.Add(pipePos, Offset));
    }
}
