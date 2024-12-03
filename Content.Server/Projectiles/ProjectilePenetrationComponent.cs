namespace Content.Server.Projectiles;

[RegisterComponent]
public sealed partial class ProjectilePenetrationComponent : Component
{
    [DataField(required: true)]
    public uint Ammount;

    [ViewVariables]
    public HashSet<EntityUid> CollidedEntities = new();
}
