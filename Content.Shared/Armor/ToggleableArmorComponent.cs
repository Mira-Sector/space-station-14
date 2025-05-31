using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableArmorComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<BodyPartType, DamageModifierSet> DisabledModifiers = [];
}
