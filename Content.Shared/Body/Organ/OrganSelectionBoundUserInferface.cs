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
    public Dictionary<ProtoId<OrganPrototype>, NetEntity> Organs;

    public OrganSelectionBoundUserInterfaceState(Dictionary<ProtoId<OrganPrototype>, NetEntity> organs)
    {
        Organs = organs;
    }
}
