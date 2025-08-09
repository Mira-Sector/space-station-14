using Robust.Shared.GameStates;

namespace Content.Shared.Body.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [DataField]
    public bool EnableOnTrigger;

    [DataField]
    public bool PreventToggle = true;

    [DataField]
    public LocId? PreventToggleReason = "body-damage-item-toggle-fail-generic";

    [ViewVariables, AutoNetworkedField]
    public bool Triggered;
}
