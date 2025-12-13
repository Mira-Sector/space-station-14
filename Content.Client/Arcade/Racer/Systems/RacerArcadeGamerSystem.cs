using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Systems;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Client.Arcade.Racer.Systems;

public sealed partial class RacerArcadeGamerSystem : SharedRacerArcadeGamerSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const string ContextName = "racer";
    private const string BaseContextName = "common";

    public override void Initialize()
    {
        base.Initialize();

        var racer = _input.Contexts.New(ContextName, BaseContextName);
        racer.AddFunction(ContentKeyFunctions.RacerPitchUp);
        racer.AddFunction(ContentKeyFunctions.RacerPitchDown);
        racer.AddFunction(ContentKeyFunctions.RacerTurnLeft);
        racer.AddFunction(ContentKeyFunctions.RacerTurnRight);
        racer.AddFunction(ContentKeyFunctions.RacerAirbrakeLeft);
        racer.AddFunction(ContentKeyFunctions.RacerAirbrakeRight);
        racer.AddFunction(ContentKeyFunctions.RacerAccelerate);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _input.Contexts.Remove(ContextName);
    }

    protected override void SetInputContext(Entity<RacerArcadeGamerComponent> ent)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        if (_input.Contexts.ActiveContext.Name != ContextName)
            ent.Comp.PreviousInputContext = _input.Contexts.ActiveContext.Name;

        _input.Contexts.SetActiveContext(ContextName);
    }

    protected override void ResetInputContext(Entity<RacerArcadeGamerComponent> ent)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        if (ent.Comp.PreviousInputContext is not { } context)
            return;

        _input.Contexts.SetActiveContext(context);
        ent.Comp.PreviousInputContext = null;
    }
}
