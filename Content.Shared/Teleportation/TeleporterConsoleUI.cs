using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation;

[Serializable, NetSerializable]
public enum TeleporterConsoleUiKey : byte
{
    Key
}

/// <summary>
/// Sends message to request that the linked teleporter is activated if it exists.
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleporterActivateMessage(Vector2 coords, bool send) : BoundUserInterfaceMessage
{
    public Vector2 Coords = coords;
    public bool Send = send;
}

public sealed class TeleporterActivateBeaconMessage(NetEntity link, bool send) : BoundUserInterfaceMessage
{
    public NetEntity Link = link;
    public bool Send = send;
}
