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
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// total charge time
    /// </summary>
    [DataField]
    public TimeSpan Duration;

    /// <summary>
    /// if false, teleportation doesn't continue
    /// EG: if EMP'd or otherwise de-powered
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TeleportSuccess = true;

    /// <summary>
    /// Message stated by console on failure reason
    /// </summary>
    [DataField]
    public LocId FailReason = "teleport-failure-unknown";
}
