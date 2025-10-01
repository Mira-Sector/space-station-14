using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
    [AutoNetworkedField]
    public TimeSpan ChargeDuration = TimeSpan.FromSeconds(0.25);

    /// <summary>
    /// The amount of time after the Teleframe has teleported before it can be used again
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public Dictionary<TeleframeActivationMode, EntProtoId?> TeleportModeEffects = new()
    {
        { TeleframeActivationMode.Send, "TeleportFromEffect" },
        { TeleframeActivationMode.Receive, "TeleportToEffect" }
    };

    /// <summary>
    /// Effect produced when teleport entities spawn
    /// </summary>
    [DataField]
    public EntProtoId? TeleportBeginEffect = null;

    /// <summary>
    /// Effect produced when teleport finishes
    /// </summary>
    [DataField]
    public EntProtoId? TeleportFinishEffect = null;

    /// <summary>
    /// Randomness of Teleportation arrival positions entities will be placed +/- of this value from exact target
    /// </summary>
    [DataField]
    public float TeleportScatterRange = 0.75f;

    /// <summary>
    /// Radius from centre of teleportation within which entities will be teleported
    /// Don't make this value too high
    /// </summary>
    [DataField]
    public float TeleportRadius = 1.5f;

    /// <summary>
    /// Power draw when actively charging/recharging
    /// </summary>
    [DataField]
    public int PowerUseActive = 10000;

    /// <summary>
    /// Power draw when idle
    /// </summary>
    [DataField]
    public int PowerUseIdle = 1000;

    /// <summary>
    /// score that must be met or exceeded for the teleframe to explode due to a random incident, incidentMult*(1d100/100)
    /// avoid setting below 1.
    /// </summary>
    [DataField]
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

    [ViewVariables, AutoNetworkedField]
    public TeleframeActiveTeleportInfo? ActiveTeleportInfo;
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
