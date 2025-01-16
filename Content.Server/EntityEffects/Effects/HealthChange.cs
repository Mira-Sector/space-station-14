using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text.Json.Serialization;

namespace Content.Server.EntityEffects.Effects
{
    /// <summary>
    /// Default metabolism used for medicine reagents.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class HealthChange : EntityEffect
    {
        /// <summary>
        /// Damage to apply every cycle. Damage Ignores resistances.
        /// </summary>
        [DataField(required: true)]
        [JsonPropertyName("damage")]
        public DamageSpecifier Damage = default!;

        /// <summary>
        ///     Should this effect scale the damage by the amount of chemical in the solution?
        ///     Useful for touch reactions, like styptic powder or acid.
        ///     Only usable if the EntityEffectBaseArgs is an EntityEffectReagentArgs.
        /// </summary>
        [DataField]
        [JsonPropertyName("scaleByQuantity")]
        public bool ScaleByQuantity;

        [DataField]
        [JsonPropertyName("ignoreResistances")]
        public bool IgnoreResistances = true;

        [DataField]
        [JsonPropertyName("targetIsOrigin")]
        public bool TargetIsOrigin = false;

        protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var damages = new List<string>();
            var heals = false;
            var deals = false;

            var damageSpec = new DamageSpecifier(Damage);

            foreach (var group in prototype.EnumeratePrototypes<DamageGroupPrototype>())
            {
                if (!damageSpec.TryGetDamageInGroup(group, out var amount))
                    continue;

                var relevantTypes = damageSpec.DamageDict
                    .Where(x => x.Value != FixedPoint2.Zero && group.DamageTypes.Contains(x.Key)).ToList();

                if (relevantTypes.Count != group.DamageTypes.Count)
                    continue;

                var sum = FixedPoint2.Zero;
                foreach (var type in group.DamageTypes)
                {
                    sum += damageSpec.DamageDict.GetValueOrDefault(type);
                }

                // if the total sum of all the types equal the damage amount,
                // assume that they're evenly distributed.
                if (sum != amount)
                    continue;

                var sign = FixedPoint2.Sign(amount);

                if (sign < 0)
                    heals = true;
                if (sign > 0)
                    deals = true;

                damages.Add(
                    Loc.GetString("health-change-display",
                        ("kind", group.LocalizedName),
                        ("amount", MathF.Abs(amount.Float())),
                        ("deltasign", sign)
                    ));

                foreach (var type in group.DamageTypes)
                {
                    damageSpec.DamageDict.Remove(type);
                }
            }

            foreach (var (kind, amount) in damageSpec.DamageDict)
            {
                var sign = FixedPoint2.Sign(amount);

                if (sign < 0)
                    heals = true;
                if (sign > 0)
                    deals = true;

                damages.Add(
                    Loc.GetString("health-change-display",
                        ("kind", prototype.Index<DamageTypePrototype>(kind).LocalizedName),
                        ("amount", MathF.Abs(amount.Float())),
                        ("deltasign", sign)
                    ));
            }

            var healsordeals = heals ? (deals ? "both" : "heals") : (deals ? "deals" : "none");

            return Loc.GetString("reagent-effect-guidebook-health-change",
                ("chance", Probability),
                ("changes", ContentLocalizationManager.FormatList(damages)),
                ("healsordeals", healsordeals));
        }

        public override void Effect(EntityEffectBaseArgs args)
        {
            var scale = FixedPoint2.New(1);

            if (args is EntityEffectReagentArgs reagentArgs)
            {
                scale = ScaleByQuantity ? reagentArgs.Quantity * reagentArgs.Scale : reagentArgs.Scale;
            }

            // so chems isnt busted
            if (args.EntityManager.TryGetComponent<BodyComponent>(args.TargetEntity, out var body))
            {
                var bodyDamageable = args.EntityManager.System<SharedBodySystem>().GetBodyDamageable(args.TargetEntity, body);

                // split the damage as the damage that is going to be healing goes down a lengthy codepath
                // which is responsable for making sure it only heals limbs that it applies to
                Dictionary<EntityUid, DamageSpecifier> positiveDamage = new();
                Dictionary<EntityUid, DamageSpecifier> negativeDamage = new();

                foreach (var (part, partDamage) in bodyDamageable)
                {
                    var damageDict = partDamage.Damage.DamageDict;

                    DamageSpecifier positiveLimbDamage = new();
                    DamageSpecifier negativeLimbDamage = new();

                    foreach (var (damageType, damageValue) in Damage.DamageDict)
                    {
                        if (damageValue == 0)
                            continue;

                        if (!damageDict.ContainsKey(damageType))
                            continue;

                        if (damageValue < 0)
                        {
                            // we cant heal the chemical
                            if (damageDict[damageType] <= 0)
                                continue;

                            positiveLimbDamage.DamageDict.Add(damageType, damageValue);
                        }
                        else
                        {
                            negativeLimbDamage.DamageDict.Add(damageType, damageValue);
                        }
                    }

                    positiveDamage.Add(part, positiveLimbDamage);
                    negativeDamage.Add(part, negativeLimbDamage);
                }

                foreach (var (part, _) in bodyDamageable)
                {
                    DamageEntity(part, DoHealing(part, positiveDamage[part], positiveDamage));
                    DamageEntity(part, DoDamage(negativeDamage[part], negativeDamage));
                }

                return;
            }

            DamageEntity(args.TargetEntity, Damage);

            void DamageEntity(EntityUid uid, DamageSpecifier damage)
            {
                args.EntityManager.System<DamageableSystem>().TryChangeDamage(
                    uid,
                    damage * scale,
                    IgnoreResistances,
                    interruptsDoAfters: false,
                    origin: TargetIsOrigin ? args.TargetEntity : null);
            }

            DamageSpecifier DoHealing(EntityUid targetPart, DamageSpecifier targetDamage, Dictionary<EntityUid, DamageSpecifier> damage)
            {
                HashSet<DamageSpecifier> matches = new();

                foreach (var (part, partDamage) in damage)
                {
                    if (part == targetPart)
                        continue;

                    DamageSpecifier matchingPartDamage = new();

                    foreach (var (damageType, damageValue) in partDamage.DamageDict)
                    {
                        if (!targetDamage.DamageDict.ContainsKey(damageType))
                            continue;

                        if (damageValue == 0)
                            continue;

                        matchingPartDamage.DamageDict[damageType] = damageValue;
                    }

                    matches.Add(matchingPartDamage);
                }

                DamageSpecifier totalDamage = new();
                Dictionary<string, (FixedPoint2, uint)> matchedTypes = new();

                foreach (var matchDamageSpecifier in matches)
                {
                    foreach (var (damageType, damageValue) in targetDamage.DamageDict)
                    {
                        // no match so just add what we wanted to if it already isnt there
                        if (!matchDamageSpecifier.DamageDict.ContainsKey(damageType))
                        {
                            if (!totalDamage.DamageDict.ContainsKey(damageType))
                                totalDamage.DamageDict.Add(damageType, damageValue);

                            continue;
                        }

                        if (!matchedTypes.ContainsKey(damageType))
                        {
                            matchedTypes.Add(damageType, (matchDamageSpecifier.DamageDict[damageType], 1));
                            continue;
                        }

                        var (currentDamage, count) = matchedTypes[damageType];

                        matchedTypes[damageType] = (currentDamage + matchDamageSpecifier.DamageDict[damageType], count + 1);
                    }
                }

                foreach (var (damageType, (damageValue, count)) in matchedTypes)
                {
                    if (totalDamage.DamageDict.ContainsKey(damageType))
                        totalDamage.DamageDict[damageType] += (damageValue / count);
                    else
                        totalDamage.DamageDict.Add(damageType, (damageValue / count));
                }

                return totalDamage;
            }

            DamageSpecifier DoDamage(DamageSpecifier targetDamage, Dictionary<EntityUid, DamageSpecifier> damage)
            {
                return targetDamage / damage.Count();
            }
        }
    }
}
