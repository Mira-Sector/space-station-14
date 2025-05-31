using Content.Shared.IncidentDisplay;
using Content.Shared.Whitelist;

namespace Content.Server.IncidentDisplay;

[RegisterComponent]
public sealed partial class IncidentDisplayIncrementerComponent : Component
{
    [DataField]
    public IncidentDisplayType IncidentType;

    [DataField]
    public EntityWhitelist? Whitelist;
}
