using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Map;

namespace Content.Shared.Telescience.Components;
/// <summary>
/// A machine that is combined and linked to the <see cref="TeleframeConsoleComponent"/>
/// in order to teleport entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TeleframeComponent : Component
{
    /// <summary>
    /// The amount of time the Teleframe charges for before teleporting
    /// </summary>
    [DataField]
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChargeDuration = TimeSpan.FromSeconds(0.25);

    /// <summary>
    /// The amount of time after the Teleframe has teleported before it can be used again
    /// </summary>
    [DataField]
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
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
    /// The corresponding Teleframe Console entity this Teleframe is linked to.
    /// Can be null if not linked.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    /// <summary>
    /// Marker, is Teleframe ready to teleport again?
    /// </summary>
    public bool ReadyToTeleport;

    /// <summary>
    /// Chance of an Anomalous Incident occuring from a Teleportation event. Chance is per Teleframe entity.
    /// </summary>
    [DataField, ViewVariables]
    public float IncidentChance = 0f;

    /// <summary>
    /// Severity Multiplier of Anomalous incidents. High Severity increases the likelyhood of very significant events.
    /// </summary>
    [DataField, ViewVariables]
    public float IncidentMultiplier = 1f;

    /// <summary>
    /// Randomness of Teleportation arrival
    /// </summary>
    [DataField, ViewVariables]
    public float TeleportScatterRange = 0.75f;

    /// <summary>
    /// Radius from centre of teleportation within which entities will be teleported
    /// </summary>
    [DataField, ViewVariables]
    public float TeleportRadius = 1.5f;

    /// <summary>
    /// TeleportFrom Entity
    /// </summary>
    public EntityUid? TeleportFrom;

    /// <summary>
    /// TeleportTo Entity
    /// </summary>
    public EntityUid? TeleportTo;

    /// <summary>
    /// Direction of Teleport Process. "Send" is Teleframe to Target (true), "Receive" is Target to Teleframe (false).
    /// </summary>
    public bool TeleportSend = true;

    /// <summary>
    /// Target portal location
    /// </summary>
    public MapCoordinates Target;
}
