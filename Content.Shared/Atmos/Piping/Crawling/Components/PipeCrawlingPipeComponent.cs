using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPipeCrawlingSystem))]
public sealed partial class PipeCrawlingPipeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<AtmosPipeLayer, Dictionary<Direction, NetEntity>> ConnectedPipes = [];

    [ViewVariables]
    public Container Container;

    /// <summary>
    /// Can someone inside the pipe escape if its unancored
    /// </summary>
    [DataField]
    public bool IsSealed;

    [DataField]
    public SoundSpecifier? MovingSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg", AudioParams.Default.WithVolume(-5f));

    [DataField]
    public float MovingSoundProb = 0.05f;
}
