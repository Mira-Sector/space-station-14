using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.SubFloor;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public sealed class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    const float PipeCollisionRadius = 0.2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingComponent, ComponentRemove>(OnRemoved);

        SubscribeLocalEvent<PipeCrawlingComponent, MoveEvent>(OnMove);
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

    private void OnMove(EntityUid uid, PipeCrawlingComponent component, ref MoveEvent args)
    {
        if (!TryComp<PipeCrawlingPipeComponent>(component.CurrentPipe, out var pipeComp))
            return;

        var direction = Transform(uid).LocalRotation.GetDir();
        (var oldPos, var oldDirection) = component.LastPos;

        // are we changing directions
        // dont check going backwards or the same direction
        if (direction != oldDirection && direction.GetOpposite() != oldDirection)
        {
            // does the pipe has a connection to annother pipe in that direction
            if (!pipeComp.ConnectedPipes.ContainsKey(direction))
            {
                ResetPosition(uid, component);
                return;
            }

            // are we around the pipes center to allow turning
            if (AroundCenter(uid, component.CurrentPipe, component))
                return;
        }

        // are we near the center and can we continue
        if (!pipeComp.ConnectedPipes.ContainsKey(direction) &&
            AroundCenter(uid, component.CurrentPipe, component))
        {
            ResetPosition(uid, component);
            return;
        }

        _xform.TryGetMapOrGridCoordinates(uid, out var currentPos);
        _xform.TryGetMapOrGridCoordinates(component.CurrentPipe, out var pipePos);

        if (currentPos == null || pipePos == null)
            return;

        // have we moved onto the next pipe
        if (pipeComp.ConnectedPipes.ContainsKey(direction) &&
            AroundCenter(uid, pipeComp.ConnectedPipes[direction], component))
        {
            component.CurrentPipe = pipeComp.ConnectedPipes[direction];
        }

        component.LastPos = (currentPos.Value, direction);
        Dirty(uid, component);
    }

    private bool AroundCenter(EntityUid player, EntityUid pipe, PipeCrawlingComponent component)
    {
        _xform.TryGetMapOrGridCoordinates(pipe, out var pipePos);
        _xform.TryGetMapOrGridCoordinates(player, out var intendedPos);

        if (pipePos == null || intendedPos == null)
            return false;

        (var lastPos, _) = component.LastPos;

        var distance = Vector2.DistanceSquared(lastPos.Position, intendedPos.Value.Position);

        return distance <= PipeCollisionRadius * PipeCollisionRadius;
    }

    private void ResetPosition(EntityUid uid, PipeCrawlingComponent component)
    {
        (var lastPos, _) = component.LastPos;
        _xform.TryGetMapOrGridCoordinates(component.CurrentPipe, out var pipePos);

        if (pipePos == null)
            return;

        _xform.SetCoordinates(uid, pipePos.Value);
    }
}
