using Robust.Shared.GameStates;

namespace Content.Shared.Glowing;

/// <summary>
///     Exists for use as a status effect. Makes user glow.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GlowingSystem))]
public sealed partial class GlowingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.Black;

    [DataField, AutoNetworkedField]
    public float Radius = 1f;
}
