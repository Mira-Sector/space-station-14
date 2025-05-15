using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Sync.Events;

public sealed partial class SiliconSyncGetNavBlipEvent : EntityEventArgs
{
    public ProtoId<NavMapBlipPrototype>? Blip;
}
