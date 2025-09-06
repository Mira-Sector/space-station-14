using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Map;

namespace Content.Shared.Teleportation.Components;
/// <summary>
/// A machine that is combined and linked to the <see cref="TeleporterConsoleComponent"/>
/// in order to teleport entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class TeleporterComponent : Component
{
    /// <summary>
    /// The amount of time the teleporter charges for before teleporting
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan ChargeDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The amount of time after the teleporter has teleported before it can be used again
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Entity Spawned at Teleport Start Point
    /// </summary>
    [DataField]
    public EntProtoId TeleportFromEffect = "TeleportFromEffect";
    /// <summary>
    /// Entity Spawned at Teleport End Point
    /// </summary>
    [DataField]
    public EntProtoId TeleportToEffect = "TeleportToEffect";

    /// <summary>
    /// Effect produced when teleport entities spawn
    /// </summary>
    [DataField]
    public string TeleportStartEffect = "EffectGravityPulse";

    /// <summary>
    /// Effect produced when teleport ends
    /// </summary>
    [DataField]
    public string TeleportEndEffect = "EffectFlashTeleportFinish";

    /// <summary>
    /// TeleportFrom Entity
    /// </summary>
    public EntityUid TeleportFrom;

    /// <summary>
    /// TeleportTo Entity
    /// </summary>
    public EntityUid? TeleportTo;

    /// <summary>
    /// Direction of Teleport Process. "Send" is Teleporter to Target (true), "Receive" is Target to Teleporter (false).
    /// </summary>
    public bool TeleportSend = true;

    public MapCoordinates Target;

    /// <summary>
    /// The corresponding Teleporter Console entity this teleporter is linked to.
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    /// <summary>
    /// Marker, is teleporter ready to teleport again? If recharging indicates time left.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ReadyToTeleport = TimeSpan.FromSeconds(5);
}
