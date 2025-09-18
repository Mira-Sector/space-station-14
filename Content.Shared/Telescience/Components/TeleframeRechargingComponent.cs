using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Telescience.Components;

/// <summary>
/// Tracker for a recharging Teleframe
/// <seealso cref="TeleframeComponent"/>
/// </summary>
/// <remarks>
/// Gee Bill! How come you get TWO charging-related components?
/// Well Bob the two need differentiating as a finished charge results in teleportation whereas a finished recharge just results in re-activiation!
/// Well Bill couldn't you just do that inside the TeleframeComponent itself and have two seperate variables to keep track of the two times?
/// I sure could Bob but we here use !ENTITY COMPONENT SYSTEM! and having events tied to the creation and destruction of components lets us keep our code nice and organised!
/// You're making that up as you go Bill and I know it! This is just ActiveMaterialReclaimerComponent but with the specificity sanded off!
/// Sure is Bob!
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class TeleframeRechargingComponent : Component
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
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration;
}
