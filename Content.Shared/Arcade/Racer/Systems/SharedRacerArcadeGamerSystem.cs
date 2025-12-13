using Content.Shared.ActionBlocker;
using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Racer.Systems;

public abstract partial class SharedRacerArcadeGamerSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeGamerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RacerArcadeGamerComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<RacerArcadeGamerComponent, UpdateCanMoveEvent>(OnCanMove);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.RacerPitchUp, new RacerArcadeGamerInputCmdHandler(RacerGameButtons.PitchUp, _timing))
            .Bind(ContentKeyFunctions.RacerPitchDown, new RacerArcadeGamerInputCmdHandler(RacerGameButtons.PitchDown, _timing))
            .Bind(ContentKeyFunctions.RacerTurnLeft, new RacerArcadeGamerInputCmdHandler(RacerGameButtons.TurnLeft, _timing))
            .Bind(ContentKeyFunctions.RacerTurnRight, new RacerArcadeGamerInputCmdHandler(RacerGameButtons.TurnRight, _timing))
            .Bind(ContentKeyFunctions.RacerAirbrakeLeft, new RacerArcadeGamerInputCmdHandler(RacerGameButtons.AirbrakeLeft, _timing))
            .Bind(ContentKeyFunctions.RacerAirbrakeRight, new RacerArcadeGamerInputCmdHandler(RacerGameButtons.AirbrakeRight, _timing))
            .Bind(ContentKeyFunctions.RacerAccelerate, new RacerArcadeGamerInputCmdHandler(RacerGameButtons.Accelerate, _timing))
            .Register<SharedRacerArcadeGamerSystem>();
    }

    private void OnStartup(Entity<RacerArcadeGamerComponent> ent, ref ComponentStartup args)
    {
        _actionBlocker.UpdateCanMove(ent.Owner);
        SetInputContext(ent);
    }

    private void OnShutdown(Entity<RacerArcadeGamerComponent> ent, ref ComponentShutdown args)
    {
        _actionBlocker.UpdateCanMove(ent.Owner);
        ResetInputContext(ent);
    }

    private void OnCanMove(Entity<RacerArcadeGamerComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    protected virtual void SetInputContext(Entity<RacerArcadeGamerComponent> ent)
    {
    }

    protected virtual void ResetInputContext(Entity<RacerArcadeGamerComponent> ent)
    {
    }
}
