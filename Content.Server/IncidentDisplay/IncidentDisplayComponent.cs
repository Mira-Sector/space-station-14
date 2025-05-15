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

    /// <summary>
    /// What incident types can we cycle through
    /// </summary>
    [DataField]
    public SortedSet<IncidentDisplayType> SelectableTypes = new();

    /// <summary>
    /// The incident type the screen is currently displaying
    /// </summary>
    [ViewVariables]
    public IncidentDisplayType CurrentType;

    /// <summary>
    /// If a incident type had a recent kill or revival
    /// </summary>
    [ViewVariables]
    public Dictionary<IncidentDisplayType, IncidentDisplayRelative> TypeRelative = new();

    /// <summary>
    /// What color to use depending on the screens state
    /// </summary>
    [DataField]
    public Dictionary<IncidentDisplayScreenVisuals, Color?> ScreenColor = new();

    /// <summary>
    /// What color to use if we just had a recent kill
    /// </summary>
    /// <remarks>
    /// If we dont define a color in <see cref=RelativeColor> uses <see cref=ScreenColor> as a fall back
    /// </remarks>
    [DataField]
    public Dictionary<IncidentDisplayRelative, Color?> RelativeColor = new();
}
