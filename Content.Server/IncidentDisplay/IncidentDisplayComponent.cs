using Content.Shared.IncidentDisplay;

namespace Content.Server.IncidentDisplay;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class IncidentDisplayComponent : Component
{
    [ViewVariables]
    public bool Broken;

    [DataField]
    public TimeSpan AdvertiseLength;

    [ViewVariables, AutoPausedField]
    public TimeSpan AdvertisementEnd;

    [ViewVariables]
    public bool Advertising;

    [DataField]
    public TimeSpan TimePerType;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextType;

    [DataField]
    public SortedSet<IncidentDisplayType> SelectableTypes = new();

    [ViewVariables]
    public IncidentDisplayType CurrentType;

    [ViewVariables]
    public Dictionary<IncidentDisplayType, IncidentDisplayRelative> TypeRelative = new();

    [DataField]
    public Dictionary<IncidentDisplayScreenVisuals, Color?> ScreenColor = new();
}
