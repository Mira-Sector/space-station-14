using Content.Shared.Roles;

namespace Content.Server.Roles;

[RegisterComponent]
public sealed partial class ObsessedRoleComponent : AntagonistRoleComponent
{
    /// <summary>
    /// The player that they are "obsessed" with and all their objectives revolve around them.
    /// </summary>
    [ViewVariables]
    public EntityUid? Obsession;
}
