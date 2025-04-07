using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterSpawnOnIntegerityComponent : Component
{
    [DataField]
    public HashSet<SupermatterIntegeritySpawns> Spawns = new();
}

[DataDefinition]
public sealed partial class SupermatterIntegeritySpawns
{
    [ViewVariables]
    public bool CanSpawn;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public HashSet<EntProtoId> Prototypes = new();

    [DataField]
    public float? Range;

    [DataField]
    public int MinSpawns = 1;

    [DataField]
    public int MaxSpawns = 1;

    [DataField]
    public TimeSpan MinDelay;

    [DataField]
    public TimeSpan MaxDelay;

    [ViewVariables]
    public TimeSpan NextSpawn;
}
