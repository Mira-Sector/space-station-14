using Content.Shared.Damage;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when all of its triggers have activated.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class AndTrigger : IThresholdTrigger
    {
        [DataField("triggers")]
        public List<IThresholdTrigger> Triggers { get; set; } = new();

        public bool Reached(DestructibleSystem system, DamageSpecifier totalDamage, bool isPositive, DamageSpecifier? deltaDamage, EntityUid? origin = null)
        {
            foreach (var trigger in Triggers)
            {
                if (!trigger.Reached(system, totalDamage, isPositive, deltaDamage, origin))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
