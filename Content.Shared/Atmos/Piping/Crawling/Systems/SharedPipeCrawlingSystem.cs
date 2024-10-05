using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMoverController _movement = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    Vector2 Offset = new Vector2(0.5f, 0.5f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingComponent, ComponentRemove>(OnRemoved);

        SubscribeLocalEvent<PipeCrawlingComponent, MoveInputEvent>(OnMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PipeCrawlingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsMoving)
                continue;

            if (!TryComp<PipeCrawlingPipeComponent>(component.CurrentPipe, out var pipeComp))
                continue;

            if (!TryComp<MovementSpeedModifierComponent>(uid, out var speedComp))
                continue;

            if (!TryComp<InputMoverComponent>(uid, out var inputComp))
                continue;

            if (TryComp<PhysicsComponent>(uid, out var physics))
                _physics.ResetDynamics(uid, physics);


            (_, var sprintingVec) = _movement.GetVelocityInput(inputComp);
            var direction = sprintingVec.GetDir();

            if (component.NextMoveAttempt > _timing.CurTime)
            {
                ResetPosition(uid, component, direction);
                continue;
            }

            component.NextMoveAttempt = _timing.CurTime + TimeSpan.FromSeconds(1f / speedComp.BaseSprintSpeed);

            if (_mobState.IsIncapacitated(uid))
                continue;

            // does the pipe has a connection to annother pipe in that direction
            if (!pipeComp.ConnectedPipes.ContainsKey(direction))
            {
                ResetPosition(uid, component, direction);
                continue;
            }

            var newPipe = pipeComp.ConnectedPipes[direction];

            if (TryComp<PipeCrawlingPipeComponent>(component.CurrentPipe, out var currentPipeComp))
            {
                currentPipeComp.ContainedEntities.Remove(uid);
                Dirty(component.CurrentPipe, currentPipeComp);
            }

            if (TryComp<PipeCrawlingPipeComponent>(newPipe, out var newPipeComp))
            {
                newPipeComp.ContainedEntities.Add(uid);
                Dirty(newPipe, newPipeComp);
            }

            component.CurrentPipe = newPipe;
            ResetPosition(uid, component, direction);
            Dirty(uid, component);
        }
    }

    private void ResetPosition(EntityUid uid, PipeCrawlingComponent component, Direction direction)
    {
        _xform.TryGetGridTilePosition(component.CurrentPipe, out var pipePos);
        _xform.SetLocalPositionNoLerp(uid, Vector2.Add(pipePos, Offset));
        _xform.SetLocalRotationNoLerp(uid, direction.ToAngle());

        if (!TryComp<AppearanceComponent>(uid, out var appearanceComp))
            return;

        _appearance.SetData(uid, SubFloorVisuals.Covered, true, appearanceComp);
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

        if (enabled)
        {
            var trayComp = EnsureComp<TrayScannerComponent>(uid);
            trayComp.EnabledEntity = true;
            trayComp.Enabled = true;
        }
        else if (HasComp<TrayScannerComponent>(uid))
        {
            RemComp<TrayScannerComponent>(uid);
        }

        var subfloorComp = EnsureComp<SubFloorHideComponent>(uid);

        var appearanceComp = EnsureComp<AppearanceComponent>(uid);
        _appearance.SetData(uid, SubFloorVisuals.Covered, enabled, appearanceComp);

        if (!TryComp<InputMoverComponent>(uid, out var inputComp))
            return;

        inputComp.CanMove = !enabled;
    }

    private void OnMove(EntityUid uid, PipeCrawlingComponent component, ref MoveInputEvent args)
    {
        component.IsMoving = (args.Entity.Comp.HeldMoveButtons & (MoveButtons.Down | MoveButtons.Left | MoveButtons.Up | MoveButtons.Right)) != 0x0;
    }
}
