using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.SubFloor;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public sealed class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    const CollisionGroup PipeCollision = CollisionGroup.PipeCrawling;
    const string PipeCollisionName = "pipe";
    const float PipeCollisionRadius = 0.2f;

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

    private void OnMove(EntityUid uid, PipeCrawlingComponent component, ref MoveInputEvent args)
    {
        var buttons = SharedMoverController.GetNormalizedMovement(args.Entity.Comp.HeldMoveButtons);

        if (buttons == MoveButtons.None)
            return;

        // dont allow diagonal movement
        if ((buttons & (buttons - 1)) != 0)
        {
            ResetPosition(uid, component);
            return;
        }

        if (!TryComp<PipeCrawlingPipeComponent>(component.CurrentPipe, out var pipeComp))
            return;

        var direction = _xform.GetWorldRotation(uid).GetDir();
        (var oldPos, var oldDirection) = component.LastPos;

        // are we changing directions
        // dont check going backwards or the same direction
        if (direction != oldDirection && direction.GetOpposite() != oldDirection)
        {
            // does the pipe has a connection to annother pipe in that direction
            if (!HasDir(pipeComp, direction))
            {
                ResetPosition(uid, component);
                return;
            }

            // are we around the pipes center to allow turning
            if (AroundCenter(uid, component.CurrentPipe, component))
                return;
        }

        // are we near the center and can we continue
        if (!HasDir(pipeComp, direction) && AroundCenter(uid, component.CurrentPipe, component))
        {
            ResetPosition(uid, component);
            return;
        }

        _xform.TryGetMapOrGridCoordinates(uid, out var currentPos);
        _xform.TryGetMapOrGridCoordinates(component.CurrentPipe, out var pipePos);

        if (currentPos == null || pipePos == null)
            return;

        var nextPipePos = pipePos.Value.Position + direction.ToVec();

        // have we moved onto the next pipe
        if (nextPipePos.Length() <= currentPos.Value.Position.Length())
        {
            // is there a connected pipe
            if (pipeComp.ConnectedPipes.ContainsKey(direction))
            {
                component.CurrentPipe = pipeComp.ConnectedPipes[direction];
            }
        }

        component.LastPos = (currentPos.Value, direction);
        Dirty(uid, component);
    }

    private bool HasDir(PipeCrawlingPipeComponent component, Direction direction)
    {
        return component.ConnectedPipes.ContainsKey(direction);
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
