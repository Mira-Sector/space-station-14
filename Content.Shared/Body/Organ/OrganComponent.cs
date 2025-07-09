using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodySystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class OrganComponent : Component
{
    /// <summary>
    /// Relevant body this organ is attached to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Body;

    [DataField, AutoNetworkedField]
    public EntityUid? BodyPart;

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<OrganPrototype> OrganType;
}
