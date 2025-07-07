using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Components;

[RegisterComponent]
public sealed partial class OrganRequiredForRespirationContainerComponent : Component
{
    [ViewVariables]
    public HashSet<ProtoId<OrganPrototype>> OrganTypes = [];
}
