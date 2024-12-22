using Content.Shared.Body.Components;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors]
public partial interface ISurgeryAction
{
    void PerformAction(EntityUid body, EntityUid limb, EntityUid? user, BodyComponent bodyComp, IEntityManager entityManager);
}
