using Content.Shared.Damage;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when any of its triggers have activated.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class OrTrigger : IThresholdTrigger
    {
        [DataField("triggers")]
        public List<IThresholdTrigger> Triggers { get; private set; } = new();

        public bool Reached(DestructibleSystem system, DamageSpecifier totalDamage, bool isPositive, DamageSpecifier? deltaDamage, EntityUid? origin = null)
        {
            foreach (var trigger in Triggers)
            {
                if (!trigger.Reached(system, totalDamage, isPositive, deltaDamage, origin))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
