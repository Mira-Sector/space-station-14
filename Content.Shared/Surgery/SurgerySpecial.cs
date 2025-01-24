namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors]
public abstract partial class SurgerySpecial
{
    public abstract void NodeReached(EntityUid body, EntityUid limb);

    public abstract void NodeLeft(EntityUid body, EntityUid limb);
}
