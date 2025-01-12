using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using JetBrains.Annotations;

namespace Content.Shared.Surgery.Completions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class RemoveLimb : ISurgeryAction
{
    public void PerformAction(EntityUid body, EntityUid limb, EntityUid? user, BodyComponent bodyComp, IEntityManager entityManager)
    {
        entityManager.EntitySysManager.GetEntitySystem<SharedBodySystem>().DetachPartToRoot(body, limb, bodyComp);
    }
}
