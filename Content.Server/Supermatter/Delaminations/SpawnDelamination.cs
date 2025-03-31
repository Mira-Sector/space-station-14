using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Supermatter.Delaminations;

[DataDefinition]
public sealed partial class SpawnDelamination : SupermatterDelaminationType
{
    [DataField]
    public float? Range;

    [DataField]
    public HashSet<EntProtoId> Prototypes = new();

    [DataField]
    public uint Count;

    public override void Delaminate(EntityUid supermatter, IEntityManager entMan)
    {
        var pos = entMan.System<TransformSystem>().GetMapCoordinates(supermatter);
        var random = IoCManager.Resolve<IRobustRandom>();

        for (var i = 0; i < Count; i++)
        {
            var newPos = Range == null ? pos : new MapCoordinates(pos.Position + random.NextVector2(-Range.Value, Range.Value), pos.MapId);
            entMan.Spawn(random.Pick(Prototypes), newPos);
        }

        entMan.QueueDeleteEntity(supermatter);
    }
}
