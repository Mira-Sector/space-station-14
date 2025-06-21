using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganRequiredForDefibrillationComponent : Component
{
    [DataField]
    public LocId? DisableReason = "defibrillator-heart-off";
}
