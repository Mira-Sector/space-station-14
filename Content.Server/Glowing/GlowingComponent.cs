//using Robust.Shared.GameStates;

namespace Content.Server.Glowing;

/// <summary>
///     Exists for use as a status effect. Makes user glow.
/// </summary>
[RegisterComponent]
[Access(typeof(GlowingSystem))]
public sealed partial class GlowingComponent : Component
{
    [DataField]
    public Color Color = Color.Black;

    [DataField]
    public float Radius = 1f;
}
