using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiCanShuntComponent : Component
{
        [ViewVariables]
        public BaseContainer? Container { get; set; }

        [ViewVariables]
        public BaseContainer? ShuntedContainer { get; set; }

        [ViewVariables]
        public bool DrawFoV { get; set; }
}
