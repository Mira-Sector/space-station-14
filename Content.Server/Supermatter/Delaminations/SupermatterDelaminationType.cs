namespace Content.Server.Supermatter.Delaminations;

[ImplicitDataDefinitionForInheritors]
public abstract partial class SupermatterDelaminationType
{
    public abstract void Delaminate(EntityUid supermatter, IEntityManager entMan);
}
