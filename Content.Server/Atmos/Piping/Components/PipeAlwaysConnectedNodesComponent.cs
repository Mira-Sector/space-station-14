namespace Content.Server.Atmos.Piping.Components;

[RegisterComponent]
public sealed partial class PipeAlwaysConnectedNodesComponent : Component
{
    [DataField]
    public HashSet<string> Nodes = new();
}
