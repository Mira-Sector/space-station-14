using Content.Shared.Arcade.Racer.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRacerArcadeGamerSystem), typeof(RacerArcadeGamerInputCmdHandler), typeof(SharedRacerArcadeSystem))]
public sealed partial class RacerArcadeGamerComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [ViewVariables, AutoNetworkedField]
    public EntityUid Cabinet;

    [ViewVariables, AutoNetworkedField]
    public RacerGameButtons HeldButtons = RacerGameButtons.None;

    public float CurTickTurning;
    public float CurTickPitching;
    public float CurTickAirbraking;
    public float CurTickAccelerating;

    public GameTick LastInputTick = GameTick.Zero;
    public ushort LastInputSubTick = 0;

    public string? PreviousInputContext;
}
