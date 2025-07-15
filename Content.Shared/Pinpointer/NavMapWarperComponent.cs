using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedNavMapSystem))]
public sealed partial class NavMapWarperComponent : Component
{
    [DataField]
    public TimeSpan WarpDelay = TimeSpan.FromSeconds(2);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextWarp;
}
