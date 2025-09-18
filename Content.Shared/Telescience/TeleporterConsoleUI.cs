using System.Numerics;
using Robust.Shared.Serialization;
using Content.Shared.Teleportation.Components;

namespace Content.Shared.Telescience;

[Serializable, NetSerializable]
public enum TeleframeConsoleUiKey : byte
{
    Key
}

//Should really combine these two activate messages into a set of MapCoordinates, a send bool, and a Location name

/// <summary>
/// Sends message to request that the linked Teleframe is activated if it exists. Teleports to a custom location.
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleframeActivateMessage(Vector2 coords, bool send) : BoundUserInterfaceMessage
{
    public Vector2 Coords = coords;
    public bool Send = send;
}


/// <summary>
/// Sends message to request that the linked Teleframe is activated if it exists. Teleports to a beacon.
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleframeActivateBeaconMessage(TeleportPoint beacon, bool send) : BoundUserInterfaceMessage
{
    public TeleportPoint Beacon = beacon;
    public bool Send = send;
}
