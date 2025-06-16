using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause, Access(typeof(SharedPipeCrawlingSystem))]
public sealed partial class PipeCrawlingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid CurrentPipe;

    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextMove;

    [ViewVariables, AutoNetworkedField]
    public AtmosPipeLayer CurrentLayer;

    [ViewVariables, AutoNetworkedField]
    public Direction Direction;

    [DataField]
    public EntProtoId LayerActionId = "ActionPipeCrawlingLayer";

    [ViewVariables]
    public EntityUid? LayerAction;

    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> PipeNet = [];
}
