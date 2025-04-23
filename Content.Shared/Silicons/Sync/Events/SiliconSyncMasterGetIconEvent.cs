using Robust.Shared.Utility;

namespace Content.Shared.Silicons.Sync.Events;

public sealed partial class SiliconSyncMasterGetIconEvent : CancellableEntityEventArgs
{
    public SpriteSpecifier? Icon;
}
