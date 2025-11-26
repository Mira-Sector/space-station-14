using Content.Shared.Arcade.Racer.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Racer;

public sealed class RacerArcadeGamerInputCmdHandler(RacerGameButtons button, IGameTiming timing) : InputCmdHandler
{
    private readonly RacerGameButtons _button = button;
    private readonly IGameTiming _timing = timing;

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (session?.AttachedEntity is not { } actor)
            return false;

        if (!entManager.TryGetComponent<RacerArcadeGamerComponent>(actor, out var gamer))
            return false;

        ResetSubtick((actor, gamer));

        if (message.SubTick >= gamer.LastInputSubTick)
        {
            var fraction = (message.SubTick - gamer.LastInputSubTick) / (float)ushort.MaxValue;

            ApplyTick((actor, gamer), fraction);
            gamer.LastInputSubTick = message.SubTick;
        }

        gamer.HeldButtons = message.State == BoundKeyState.Down
            ? gamer.HeldButtons |= _button
            : gamer.HeldButtons &= ~_button;
        gamer.HeldButtons = gamer.HeldButtons.CancelMutuallyExclusive();
        entManager.Dirty(actor, gamer);
        return false;
    }

    public void ResetSubtick(Entity<RacerArcadeGamerComponent> ent)
    {
        if (_timing.CurTick < ent.Comp.LastInputTick)
            return;

        ent.Comp.CurTickTurning = 0f;
        ent.Comp.CurTickPitching = 0f;
        ent.Comp.CurTickAirbraking = 0f;
        ent.Comp.CurTickAccelerating = 0f;

        ent.Comp.LastInputTick = _timing.CurTick;
        ent.Comp.LastInputSubTick = 0;
    }

    public void ApplyTick(Entity<RacerArcadeGamerComponent> ent, float fraction)
    {
        {
            var pitch = 0f;
            if ((ent.Comp.HeldButtons & RacerGameButtons.PitchUp) != 0)
                pitch += 1f;

            if ((ent.Comp.HeldButtons & RacerGameButtons.PitchDown) != 0)
                pitch -= 1f;

            ent.Comp.CurTickPitching += pitch * fraction;
        }

        {
            var turn = 0f;
            if ((ent.Comp.HeldButtons & RacerGameButtons.TurnLeft) != 0)
                turn += 1f;

            if ((ent.Comp.HeldButtons & RacerGameButtons.TurnRight) != 0)
                turn -= 1f;

            ent.Comp.CurTickTurning += turn * fraction;
        }

        {
            var airbrake = 0f;
            if ((ent.Comp.HeldButtons & RacerGameButtons.AirbrakeLeft) != 0)
                airbrake += 1f;

            if ((ent.Comp.HeldButtons & RacerGameButtons.AirbrakeRight) != 0)
                airbrake -= 1f;

            ent.Comp.CurTickAirbraking += airbrake * fraction;
        }

        {
            var accelerate = 0f;
            if ((ent.Comp.HeldButtons & RacerGameButtons.Accelerate) != 0)
                accelerate = 1f;

            ent.Comp.CurTickAccelerating += accelerate * fraction;
        }
    }
}
