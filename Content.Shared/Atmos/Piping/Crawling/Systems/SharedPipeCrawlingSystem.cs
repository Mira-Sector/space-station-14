using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Camera;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SubFloor;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public abstract partial class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _movement = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    const string PipeContainer = "pipe";
    const float CrawlSpeedMultiplier = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        EnterPointInitialize();

        SubscribeLocalEvent<PipeCrawlingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingComponent, ComponentRemove>(OnRemoved);

        SubscribeLocalEvent<PipeCrawlingComponent, MoveInputEvent>(OnMove);
        SubscribeLocalEvent<PipeCrawlingComponent, GetEyeOffsetEvent>(OnEyeOffset);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PipeCrawlingComponent, CanEnterPipeCrawlingComponent, MovementSpeedModifierComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out var component, out var pipeCrawlerComp, out var speedComp, out var inputComp))
        {
            if (!component.IsMoving)
                continue;

            if (!TryComp<PipeCrawlingPipeComponent>(component.CurrentPipe, out var pipeComp))
                continue;

            if (TryComp<PhysicsComponent>(uid, out var physics))
                _physics.ResetDynamics(uid, physics);

            (_, var sprintingVec) = _movement.GetVelocityInput(inputComp);
            var direction = sprintingVec.GetDir();

            if (_mobState.IsIncapacitated(uid))
                continue;

            if (component.NextMoveAttempt > _timing.CurTime)
            {
                if (!pipeComp.ConnectedPipes.ContainsKey(direction))
                    continue;

                if (component.TargetDirection != direction)
                {
                    component.TargetDirection = direction;
                    Dirty(uid, component);
                }

                _eye.SetOffset(uid, InterpEye(component, pipeCrawlerComp, speedComp));
                continue;
            }

            component.NextMoveAttempt += GetNextMoveAttemptFrequency(pipeCrawlerComp, speedComp);

            // does the pipe has a connection to annother pipe in that direction
            if (!pipeComp.ConnectedPipes.ContainsKey(direction))
                continue;

            component.TargetDirection = direction;
            var newPipe = pipeComp.ConnectedPipes[direction];

            if (_containers.TryGetContainer(component.CurrentPipe, PipeContainer, out var currentPipeContainer) && HasComp<PipeCrawlingPipeComponent>(component.CurrentPipe))
            {
                var newPipeCoords = Transform(newPipe).Coordinates;
                _containers.Remove(uid, currentPipeContainer, destination: newPipeCoords, localRotation: direction.ToAngle());
            }

            if (_containers.TryGetContainer(newPipe, PipeContainer, out var newPipeContainer) && HasComp<PipeCrawlingPipeComponent>(newPipe))
            {
                _containers.Insert(uid, newPipeContainer);
            }

            component.CurrentPipe = newPipe;
            Dirty(uid, component);
        }
    }

    private Vector2 InterpEye(PipeCrawlingComponent pipeCrawlingComp, CanEnterPipeCrawlingComponent pipeCrawlerComp, MovementSpeedModifierComponent speedComp)
    {
        var targetOffset = pipeCrawlingComp.TargetDirection.ToVec();
        var frequency = GetNextMoveAttemptFrequency(pipeCrawlerComp, speedComp);
        var nextMove = pipeCrawlingComp.NextMoveAttempt + frequency;
        var timeElapsed = nextMove - _timing.CurTime;
        var scalar = (float) (timeElapsed.TotalSeconds / frequency.TotalSeconds);

        return Vector2.Multiply(targetOffset, scalar);
    }

    private TimeSpan GetNextMoveAttemptFrequency(CanEnterPipeCrawlingComponent pipeCrawlerComp, MovementSpeedModifierComponent speedComp)
    {
        return TimeSpan.FromSeconds(pipeCrawlerComp.PipeMoveSpeed ?? (1f / speedComp.BaseSprintSpeed) * CrawlSpeedMultiplier);
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

        if (!TryComp<InputMoverComponent>(uid, out var inputComp))
            return;

        inputComp.CanMove = !enabled;
    }

    private void OnMove(EntityUid uid, PipeCrawlingComponent component, ref MoveInputEvent args)
    {
        component.IsMoving = (args.Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) != 0;
        Dirty(uid, component);
    }

    private void OnEyeOffset(EntityUid uid, PipeCrawlingComponent component, ref GetEyeOffsetEvent args)
    {
        if (!TryComp<CanEnterPipeCrawlingComponent>(uid, out var pipeCrawlerComp))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(uid, out var speedComp))
            return;

        args.Offset += InterpEye(component, pipeCrawlerComp, speedComp);
    }
}
