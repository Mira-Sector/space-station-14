using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SiliconSyncableSlaveAiRadialComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "sync");

    [DataField, AutoNetworkedField]
    public LocId Tooltip = "ai-sync";
}
