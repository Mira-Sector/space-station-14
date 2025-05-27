namespace Content.Server.Power.Components;

/// <summary>
///     Transfer power from one battery to another.
/// </summary>
[RegisterComponent]
public sealed partial class BatteryTransferComponent : Component
{
    [DataField]
    public bool CanTransfer = true;

    [DataField]
    public bool CanReceive = false;
}
