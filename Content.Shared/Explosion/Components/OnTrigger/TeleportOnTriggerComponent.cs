using Robust.Shared.GameStates;

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
    /// Randomness of Teleportation arrival
    /// </summary>
    public float TeleportScatterRange = 0.75f;

    [DataField]
    public float TeleportRadius = 1.5f;

    [DataField]
    public float TeleportIncidentChance = 0.15f;
}
