using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicles;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleComponent : Component
{
    [ViewVariables]
    public EntityUid? Driver;

    [ViewVariables]
    public EntityUid? HornAction;

    [ViewVariables]
    public EntityUid? SirenAction;

    public bool SirenEnabled = false;

    public EntityUid? SirenStream;

    /// <summary>
    /// If non-zero how many virtual items to spawn on the driver
    /// unbuckles them if they dont have enough
    /// </summary>
    [DataField]
    public int RequiredHands = 1;

    /// <summary>
    /// What sound to play when the driver presses the horn action (plays once)
    /// </summary>
    [DataField]
    public SoundSpecifier? HornSound = new SoundCollectionSpecifier("BikeHorn");

    /// <summary>
    /// What sound to play when the driver presses the siren action (loops)
    /// </summary>
    [DataField]
    public SoundSpecifier? SirenSound;
}
[Serializable, NetSerializable]
public enum VehicleState
{
    Animated
}
