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
    public Dictionary<OrganType, NetEntity> Organs;

    public OrganSelectionBoundUserInterfaceState(Dictionary<OrganType, NetEntity> organs)
    {
        Organs = organs;
    }
}
