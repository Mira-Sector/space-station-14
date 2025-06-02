using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorMusicComponent : Component
{
    [DataField]
    public SoundSpecifier Music = new SoundPathSpecifier("/Audio/Misc/elevator.ogg");

    [ViewVariables, AutoNetworkedField]
    public float NextPlayOffset;

    public (EntityUid, AudioComponent)? SoundEntity;
}
