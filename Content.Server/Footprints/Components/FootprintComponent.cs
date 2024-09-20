namespace Content.Server.Footprints.Components;

/// <summary>
/// Added to footprints to track forensics
/// </summary>
[RegisterComponent]
public sealed partial class FootprintComponent : Component
{
    [ViewVariables]
    public TimeSpan CreationTime;
}
