using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Telescience.Ui;

/// <summary>
/// Sends message to request that the linked Teleframe is activated if it exists.
/// EntityCoordinates are not Serializable so we make do
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleframeActivateMessage(MapCoordinates coords, string name, TeleframeActivationMode mode, bool rangeBypass = false) : BoundUserInterfaceMessage
{
    public MapCoordinates Coords = coords;
    public string Name = name;  // name of target, may be seperate from entity name
    public TeleframeActivationMode Mode = mode;
    public bool RangeBypass = rangeBypass; //whether to ignore range limits (for beacons)
}
