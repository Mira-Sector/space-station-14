using Robust.Shared.Utility;

namespace Content.Client.Silicons.Sync.Components;

[RegisterComponent]
public sealed partial class SiliconSyncableSlaveCommandedVisualsComponent : Component
{
    [DataField(required: true)]
    public SpriteSpecifier PlanningSprite;

    [DataField(required: true)]
    public SpriteSpecifier MovingSprite;
}
