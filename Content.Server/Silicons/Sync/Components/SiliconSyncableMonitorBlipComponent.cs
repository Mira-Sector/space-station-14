using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Sync.Components;

[RegisterComponent]
public sealed partial class SiliconSyncableMonitorBlipComponent : Component
{
    [DataField]
    public ProtoId<NavMapBlipPrototype> Blip;
}
