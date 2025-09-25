using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
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
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? TeleportFromEffect = "TeleportFromEffect";
    /// <summary>
    /// Entity Spawned at Teleport End Point
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? TeleportToEffect = "TeleportToEffect";

    /// <summary>
    /// Effect produced when teleport entities spawn
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? TeleportBeginEffect = null;

    /// <summary>
    /// Effect produced when teleport finishes
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? TeleportFinishEffect = null;

    /// <summary>
    /// Chance of an Anomalous Incident occuring from a Teleportation event. Chance is per Teleported entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float IncidentChance = 0.00f;

    /// <summary>
    /// Severity Multiplier of Anomalous incidents. High Severity increases the likelyhood of very significant events.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float IncidentMultiplier = 1f;

    /// <summary>
    /// Randomness of Teleportation arrival positions entities will be placed +/- of this value from exact target
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportScatterRange = 0.75f;

    /// <summary>
    /// Radius from centre of teleportation within which entities will be teleported
    /// Don't make this value too high
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRadius = 1.5f;

    /// <summary>
    /// Power draw when actively charging/recharging
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PowerUseActive = 10000;

    /// <summary>
    /// Power draw when idle
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PowerUseIdle = 1000;

    /// <summary>
    /// score that must be met or exceeded for the teleframe to explode due to a random incident, incidentMult*(1d100/100)
    /// avoid setting below 1.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]

    public float ExplosionScore = 1000f;

    //##########################################

    /// <summary>
    /// Whether the Teleframe is powered
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsPowered = false;

    /// <summary>
    /// The corresponding Teleframe Console entity this Teleframe is linked to.
    /// Can be null if not linked.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    /// <summary>
    /// Marker, is Teleframe ready to teleport again?
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool ReadyToTeleport = true;

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

[NetSerializable, Serializable]
public enum TeleframeVisuals : byte
{
    VisualState
}

[NetSerializable, Serializable]
public enum TeleframeVisualState : byte
{
    On,
    Charging,
    Recharging,
    Off
}
