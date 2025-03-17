//using Robust.Shared.GameStates;

namespace Content.Server.Glowing;

/// <summary>
///     Exists for use as a status effect (eventually). Make user glow
/// </summary>
[RegisterComponent]
[Access(typeof(GlowingSystem))]
public sealed partial class GlowingComponent : Component
{
    //public Color Color = Color.Gold;
    //public int Radius = 5;
}
