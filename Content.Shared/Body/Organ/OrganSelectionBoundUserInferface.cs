using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
public enum OrganSelectionUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class OrganSelectionBoundUserInterfaceState : BoundUserInterfaceState
{
    public Dictionary<ProtoId<OrganPrototype>, NetEntity?> Organs;

    public OrganSelectionBoundUserInterfaceState(Dictionary<ProtoId<OrganPrototype>, NetEntity?> organs)
    {
        Organs = organs;
    }
}

[Serializable, NetSerializable]
public sealed class OrganSelectionButtonPressedMessage : BoundUserInterfaceMessage
{
    public ProtoId<OrganPrototype> OrganPrototype;
    public NetEntity? OrganId;

    public OrganSelectionButtonPressedMessage(ProtoId<OrganPrototype> organPrototype, NetEntity? organId)
    {
        OrganPrototype = organPrototype;
        OrganId = organId;
    }
}
