using System.Numerics;

namespace Content.Server.Atmos;

public struct SpaceWind
{
    [ViewVariables]
    public Vector2 Wind = Vector2.Zero;

    [ViewVariables]
    public Vector2 PendingWind { get; set; } = Vector2.Zero;

    public SpaceWind()
    {
    }
}
