using Robust.Shared.GameStates;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class IdExaminableComponent : Component
{
    [DataField]
    public string? IdOverride;
}
