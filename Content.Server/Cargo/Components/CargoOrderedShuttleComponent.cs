using Robust.Shared.Map;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
public sealed partial class CargoOrderedShuttleComponent : Component
{
    [ViewVariables]
    public MapId SourceMap;
}
