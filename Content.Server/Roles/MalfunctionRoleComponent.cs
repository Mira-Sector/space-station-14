using Content.Shared.Silicons.Laws;
using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind role entities to tag that they are a malfunctioning AI
/// </summary>
[RegisterComponent]
public sealed partial class MalfunctionRoleComponent : BaseMindRoleComponent
{
    [DataField]
    public List<SiliconLaw> Laws = new();
}
