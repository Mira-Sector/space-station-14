using Robust.Shared.GameStates;

namespace Content.Shared.Wounds.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WoundRecieverComponent : Component
{
    [DataField]
    public List<string> SelectableWounds = new ();
}
