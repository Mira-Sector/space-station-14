using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause, Access(typeof(SharedPipeCrawlingSystem))]
public sealed partial class PipeCrawlingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid CurrentPipe;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextMove;

    [ViewVariables, AutoNetworkedField]
    public AtmosPipeLayer CurrentLayer;
}
