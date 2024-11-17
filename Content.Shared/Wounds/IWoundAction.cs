using Content.Shared.Body.Components;

namespace Content.Shared.Wounds;

[ImplicitDataDefinitionForInheritors]
public partial interface IWoundAction
{
    void PerformAction(EntityUid body, EntityUid limb, EntityUid? user, BodyComponent bodyComp, IEntityManager entityManager);
}
