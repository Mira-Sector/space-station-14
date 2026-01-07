namespace Content.Shared.Arcade.Racer;

public static class RacerGameButtonHelper
{
    public static RacerGameButtons CancelMutuallyExclusive(this RacerGameButtons buttons)
    {
        var newButtons = buttons;

        if ((buttons & RacerGameButtons.Pitching) == RacerGameButtons.Pitching)
            newButtons &= ~RacerGameButtons.Pitching;

        if ((buttons & RacerGameButtons.Turning) == RacerGameButtons.Turning)
            newButtons &= ~RacerGameButtons.Turning;

        // no airbraking as there is double air breaking to slow down the ship

        return newButtons;
    }
}
