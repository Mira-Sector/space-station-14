using Content.Shared.Wounds.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WoundRecieverComponent : Component
{
    [DataField]
    public List<WoundPrototype> SelectableWounds = new ();
}
