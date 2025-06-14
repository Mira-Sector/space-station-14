using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Content.Shared.Atmos.Piping.Crawling.Systems;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPipeCrawlingSystem))]
public sealed partial class PipeCrawlingEnterPointComponent : Component
{
    [DataField]
    public bool CanEnter = true;

    [DataField]
    public bool CanExit = true;

    [ViewVariables, AutoNetworkedField]
    public bool Enterable = false;

    [ViewVariables, AutoNetworkedField]
    public bool Exitable = false;

    [DataField]
    public TimeSpan DoAfterTime;
}

[Serializable, NetSerializable]
public sealed partial class PipeCrawlingEnterDoAfterEvent : SimpleDoAfterEvent;
