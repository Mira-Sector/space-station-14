namespace Content.Shared.Telescience.Events;

///<summary>
/// Event raised on entities that are to experience a teleport incident
/// </summary>
[ByRefEvent]
public record struct TelescienceUserIncidentEvent(float Score, float IncidentMult);
