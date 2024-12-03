using Content.Shared.Damage;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    public interface IThresholdTrigger
    {
        /// <summary>
        ///     Checks if this trigger has been reached.
        /// </summary>
        /// <returns>true if this trigger has been reached, false otherwise.</returns>
        bool Reached(DestructibleSystem system, DamageSpecifier totalDamage, bool isPositive, DamageSpecifier? deltaDamage, EntityUid? origin = null);
    }
}
