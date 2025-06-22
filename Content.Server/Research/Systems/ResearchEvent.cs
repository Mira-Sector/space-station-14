


namespace Content.Server.Research.Systems;

public sealed class ResearchEvent : HandledEntityEventArgs
{
    /// <summary>
    /// Can be raised on the purchase of a research
    /// </summary>
    public EntityUid Location { get; }
    public string? Discipline { get; }

    public ResearchEvent(EntityUid location, string? discipline = null)
    {
        Location = location;
        Discipline = discipline;
    }
}
