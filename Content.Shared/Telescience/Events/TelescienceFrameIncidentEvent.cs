namespace Content.Shared.Telescience.Events;

///<summary>
/// Event raised on the frame that are to experience a teleport incident
/// </summary>
[ByRefEvent]
public record struct TelescienceFrameIncidentEvent(float Score, float IncidentMult);
