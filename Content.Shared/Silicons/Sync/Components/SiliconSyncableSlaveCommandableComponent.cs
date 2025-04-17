using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SiliconSyncableSlaveCommandableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier PlanningSprite;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier MovingSprite;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "command");

    [DataField, AutoNetworkedField]
    public LocId Tooltip = "ai-command";
}
