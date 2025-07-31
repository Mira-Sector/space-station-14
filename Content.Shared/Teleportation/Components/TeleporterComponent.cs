
using Robust.Shared.Audio;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, AutoGenerateComponentState(true)]

public sealed partial class TeleporterComponent : Component
{
    /// <summary>
    /// The amount of time between the teleporter being activated and beginning to charge
    /// </summary>
    [DataField]
    public TimeSpan InitialiseDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The amount of time the teleporter charges for before teleporting
    /// </summary>
    [DataField]
    public TimeSpan ChargeDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The amount of time after the teleporter has teleported before it can be used again
    /// </summary>
    [DataField]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public SoundSpecifier? TeleportSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public string TeleportToEffect;

    [DataField]
    public string TeleportFromEffect;

    [DataField]
    public string TeleportEffect = "EffectFlashBluespace";

    /// <summary>
    /// The corresponding Teleporter Console entity this teleporter is linked to.
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    /// <summary>
    /// Marker, is teleporter ready to teleport again?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ReadyToTeleport = false;
}
