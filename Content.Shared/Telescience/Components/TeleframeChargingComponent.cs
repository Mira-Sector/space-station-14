using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Telescience.Components;

/// <summary>
/// Tracker for a charging Teleframe
/// <seealso cref="TeleframeComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class TeleframeChargingComponent : Component
{
    /// <summary>
    /// when charge will finish
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;

    /// <summary>
    /// total charge time
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration;

    /// <summary>
    /// Rolled at start of charge, if above set value, Teleframe explodes after teleportation.
    /// Explosion size scales with incident multiplier
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WillExplode = false;

    /// <summary>
    /// if false, teleportation doesn't continue
    /// EG: if EMP'd or otherwise de-powered
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TeleportSuccess = true;

    /// <summary>
    /// suffix to message stated by console on failiure reason, added onto "teleport-failiure-"
    /// </summary>
    [DataField]
    public string FailReason = "unknown";
}
