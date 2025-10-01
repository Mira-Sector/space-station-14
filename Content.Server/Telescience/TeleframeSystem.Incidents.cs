using Content.Server.Lightning;
using Content.Shared.Telescience;
using Content.Shared.Telescience.Systems;

namespace Content.Server.Telescience;

public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    [Dependency] private readonly LightningSystem _lightning = default!;

    ///<summary>
    /// Just fucking explode, lightning bolt number equal to incident multiplier
    ///</summary>
    /// <remarks>
    /// Remove this and replace with an incident prototype
    /// </remarks>
    protected override void TeleframeIncidentExplode(Entity<TeleframeIncidentLiableComponent> ent)
    {
        // TODO: remove magic number
        _lightning.ShootRandomLightnings(ent.Owner, ent.Comp.IncidentMultiplier * 3, (int)Math.Ceiling(ent.Comp.IncidentMultiplier));
    }
}
