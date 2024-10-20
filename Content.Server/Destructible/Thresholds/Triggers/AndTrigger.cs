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

        public bool Reached(DamageableComponent damageable, DestructibleSystem system, DamageChangedEvent args)
        {
            foreach (var trigger in Triggers)
            {
                if (!trigger.Reached(damageable, system, args))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
