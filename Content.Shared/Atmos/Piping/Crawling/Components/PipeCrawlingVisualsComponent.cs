using Content.Shared.Atmos.Piping.Crawling.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPipeCrawlingSystem))]
public sealed partial class PipeCrawlingVisualsComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Revealers = [];
}
