using Robust.Shared.GameStates;

namespace Content.Shared.Body.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DefibrillationDisableOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [DataField]
    public LocId? Reason;
}
