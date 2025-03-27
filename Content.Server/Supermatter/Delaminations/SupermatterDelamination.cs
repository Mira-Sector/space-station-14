namespace Content.Server.Supermatter.Delaminations;

[ImplicitDataDefinitionForInheritors]
public abstract partial class SupermatterDelamination
{
    [DataField]
    public HashSet<DelaminationRequirement> Requirements = new();

    public bool RequirementsMet(EntityUid supermatter, IEntityManager entMan)
    {
        foreach (var requirement in Requirements)
        {
            if (!requirement.RequirementMet(supermatter, entMan))
                return false;
        }

        return true;
    }

    public abstract void Delaminate(EntityUid supermatter, IEntityManager entMan);
}
