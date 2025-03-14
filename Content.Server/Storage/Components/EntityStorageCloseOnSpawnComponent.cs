namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed partial class EntityStorageCloseOnSpawnComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.5f);

    [ViewVariables]
    public TimeSpan CloseAt;
}
