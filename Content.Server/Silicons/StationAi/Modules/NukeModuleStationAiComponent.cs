namespace Content.Server.Silicons.StationAi.Modules;

[RegisterComponent]
public sealed partial class NukeModuleStationAiComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Nukes = new();
}
