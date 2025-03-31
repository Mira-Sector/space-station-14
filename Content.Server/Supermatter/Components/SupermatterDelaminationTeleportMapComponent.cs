using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterDelaminationTeleportMapComponent: Component
{
    [DataField]
    public ComponentRegistry MapComponents = new();

    [DataField]
    public Vector2 MapPosition = new();
}
