using Robust.Shared.GameStates;

namespace Content.Shared.Telescience;

[RegisterComponent, NetworkedComponent]
public sealed partial class TeleframeIncidentLiableComponent : Component
{
    /// <summary>
    /// Chance of an Anomalous Incident occurring from a Teleportation event. Chance is per Teleported entity.
    /// </summary>
    [DataField]
    public float IncidentChance = 0.00f;

    /// <summary>
    /// Severity Multiplier of Anomalous incidents. High Severity increases the likelyhood of very significant events.
    /// </summary>
    [DataField]
    public float IncidentMultiplier = 1f;

    //potentially with upgrades, emagging could be considered an invisible "upgrade" that can't be gotten rid of and these will be moved to there.

    [DataField]
    public float EmagIncidentChance = 1f;

    [DataField]
    public float EmagIncidentMultiplier = 2f;
}
