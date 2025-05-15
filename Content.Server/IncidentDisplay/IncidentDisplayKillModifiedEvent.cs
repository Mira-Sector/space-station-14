using Content.Shared.IncidentDisplay;

namespace Content.Server.IncidentDisplay;

public sealed partial class IncidentDisplayKillModifiedEvent : EntityEventArgs
{
    public IncidentDisplayType Type;
    public int Modified;
    public int Total;

    public IncidentDisplayKillModifiedEvent(IncidentDisplayType type, int modified, int total)
    {
        Type = type;
        Modified = modified;
        Total = total;
    }
}
