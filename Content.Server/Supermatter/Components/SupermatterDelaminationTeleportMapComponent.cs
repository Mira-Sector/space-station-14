using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterDelaminationTeleportMapComponent: Component
{
    [DataField]
    public ResPath MapPath;

    [DataField]
    public Vector2 MapPosition = new();
}
