using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
}

[Serializable, NetSerializable]
public sealed partial class PipeEnterDoAfterEvent : SimpleDoAfterEvent;
