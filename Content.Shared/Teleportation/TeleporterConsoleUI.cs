using System.Numerics;
using Robust.Shared.Serialization;
using Content.Shared.Teleportation.Components;

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

[Serializable, NetSerializable]
public sealed class TeleporterActivateBeaconMessage(TeleportPoint beacon, bool send) : BoundUserInterfaceMessage
{
    public TeleportPoint Beacon = beacon;
    public bool Send = send;
}
