using Robust.Shared.GameStates;
using Content.Shared.Telescience.Components;

namespace Content.Shared.Explosion.Components;
/// <summary>
/// Teleports all non-anchored entities within range to target destination
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TeleportOnTriggerComponent : Component
{
    /// <summary>
    /// TeleportFrom Entity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? TeleportFrom;

    /// <summary>
    /// TeleportTo Entity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? TeleportTo;

    /// <summary>
    /// Teleporter-Specific Info to guide how teleportation will play out
    /// if not present, a new one is generated. Useful for if this function is used outside of teleporters
    /// </summary>
    [DataField]
    public TeleframeComponent Teleporter = new();

    /// <summary>
    /// Uid of the Teleporter if there is one, just used to sent an event home when it's all done
    /// </summary>
    public EntityUid? TeleporterUid;
}
