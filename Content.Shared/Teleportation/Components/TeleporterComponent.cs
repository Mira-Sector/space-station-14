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
    public TimeSpan ChargeDuration = TimeSpan.FromSeconds(0.25);

    /// <summary>
    /// The amount of time after the teleporter has teleported before it can be used again
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(1);

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
    public string TeleportBeginEffect = "EffectGravityPulse";

    /// <summary>
    /// Effect produced when teleport finishes
    /// </summary>
    [DataField]
    public string TeleportFinishEffect = "EffectFlashTeleportFinish";

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

    /// <summary>
    /// Target portal location
    /// </summary>
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

    /// <summary>
    /// Chance of an Anomalous Incident occuring from a Teleportation event. Chance is per teleporter entity.
    /// </summary>
    [DataField]
    public float IncidentChance = 0f;

    /// <summary>
    /// Severity Multiplier of Anomalous incidents. High Severity increases the likelyhood of very significant events.
    /// </summary>
    [DataField]
    public float IncidentMultiplier = 1f;

    /// <summary>
    /// Randomness of Teleportation arrival
    /// </summary>
    [DataField]
    public float TeleportScatterRange = 0.75f;

    /// <summary>
    /// Radius from centre of teleportation within which entities will be teleported
    /// </summary>
    [DataField]
    public float TeleportRadius = 1.5f;
}
