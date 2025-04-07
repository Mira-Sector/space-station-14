using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Server.Supermatter.Delaminations;

[DataDefinition]
public sealed partial class ExplosionDelamination : SupermatterDelaminationType
{
    [DataField(required: true)]
    public ProtoId<ExplosionPrototype> Type;

    [DataField(required: true)]
    public float TotalIntensity;

    [DataField(required: true)]
    public float IntensitySlope;

    [DataField(required: true)]
    public float MaxIntensity;

    public override void Delaminate(EntityUid supermatter, IEntityManager entMan)
    {
        var explosionSys = entMan.System<ExplosionSystem>();
        explosionSys.QueueExplosion(supermatter, Type, TotalIntensity, IntensitySlope, MaxIntensity);
        entMan.QueueDeleteEntity(supermatter);
    }
}
