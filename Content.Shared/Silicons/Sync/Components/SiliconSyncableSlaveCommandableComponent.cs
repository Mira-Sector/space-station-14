using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class SiliconSyncableSlaveCommandableComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Master;

    [DataField(required: true), AutoNetworkedField]
    public Dictionary<SiliconSyncCommandingPathType, SpriteSpecifier> PathSprites;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier EnableIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "command_on");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier DisableIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "command_off");

    [DataField, AutoNetworkedField]
    public LocId EnableTooltip = "ai-command-on";

    [DataField, AutoNetworkedField]
    public LocId DisableTooltip = "ai-command-off";
}
