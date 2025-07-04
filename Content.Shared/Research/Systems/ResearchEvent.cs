namespace Content.Shared.Research.Systems;

/// <summary>
/// Can be raised on the purchase of a research
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class ResearchEvent : EntityEventArgs
{
    /// <summary>
    /// EntityUiD representing entity on the station the research occurred on. Typically the research server.
    /// </summary>
    public EntityUid Location;
}
/// <summary>
/// Can be raised on the purchase of a research where funding is delivered
/// </summary>
public sealed partial class ResearchFundingEvent : ResearchEvent
{
    /// <summary>
    /// Base Announcement message 
    /// </summary>
    [DataField(required: true)]
    public string Message = "station-event-funding-research-announcement";

    /// <summary>
    /// Discipline-specific announcement data
    /// </summary>    
    [DataField(required: true)]
    public string Discipline = "station-event-funding-research-null";

    /// <summary>
    /// How much funding the station gets
    /// </summary>   
    [DataField(required: true)]
    public int Payment;
}
