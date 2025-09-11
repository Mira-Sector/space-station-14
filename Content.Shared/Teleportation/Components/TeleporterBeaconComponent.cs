using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Links to TeleporterConsoleComponent, lets teleporters target wherever it is placed.
/// Basically just a clone of FultonBeaconComponent
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleporterBeaconComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundLink"), AutoNetworkedField]
    public SoundSpecifier? LinkSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");

    /// <summary>
    /// Whether or not a beacon is valid for teleporters to target, might be changed for example if an anchorable beacon isn't anchored
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public bool ValidBeacon = true;
}
