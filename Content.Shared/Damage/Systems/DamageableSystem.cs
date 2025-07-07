using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.CCVar;
using Content.Shared.Chemistry;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radiation.Events;
using Content.Shared.Rejuvenate;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Damage
{
    public sealed class DamageableSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly INetManager _netMan = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
        [Dependency] private readonly SharedBodySystem _body = default!;
        [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly SharedChemistryGuideDataSystem _chemistryGuideData = default!;

        private EntityQuery<AppearanceComponent> _appearanceQuery;
        private EntityQuery<BodyComponent> _bodyQuery;
        private EntityQuery<DamageableComponent> _damageableQuery;
        private EntityQuery<MindContainerComponent> _mindContainerQuery;

        public float UniversalAllDamageModifier { get; private set; } = 1f;
        public float UniversalAllHealModifier { get; private set; } = 1f;
        public float UniversalMeleeDamageModifier { get; private set; } = 1f;
        public float UniversalProjectileDamageModifier { get; private set; } = 1f;
        public float UniversalHitscanDamageModifier { get; private set; } = 1f;
        public float UniversalReagentDamageModifier { get; private set; } = 1f;
        public float UniversalReagentHealModifier { get; private set; } = 1f;
        public float UniversalExplosionDamageModifier { get; private set; } = 1f;
        public float UniversalThrownDamageModifier { get; private set; } = 1f;
        public float UniversalTopicalsHealModifier { get; private set; } = 1f;
        public float UniversalMobDamageModifier { get; private set; } = 1f;

        public override void Initialize()
        {
            SubscribeLocalEvent<DamageableComponent, ComponentInit>(DamageableInit);
            SubscribeLocalEvent<DamageableComponent, ComponentHandleState>(DamageableHandleState);
            SubscribeLocalEvent<DamageableComponent, ComponentGetState>(DamageableGetState);
            SubscribeLocalEvent<DamageableComponent, OnIrradiatedEvent>(OnIrradiated);
            SubscribeLocalEvent<DamageableComponent, RejuvenateEvent>(OnRejuvenate);

            _appearanceQuery = GetEntityQuery<AppearanceComponent>();
            _bodyQuery = GetEntityQuery<BodyComponent>();
            _damageableQuery = GetEntityQuery<DamageableComponent>();
            _mindContainerQuery = GetEntityQuery<MindContainerComponent>();

            // Damage modifier CVars are updated and stored here to be queried in other systems.
            // Note that certain modifiers requires reloading the guidebook.
            Subs.CVar(_config, CCVars.PlaytestAllDamageModifier, value =>
            {
                UniversalAllDamageModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            }, true);
            Subs.CVar(_config, CCVars.PlaytestAllHealModifier, value =>
            {
                UniversalAllHealModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            }, true);
            Subs.CVar(_config, CCVars.PlaytestProjectileDamageModifier, value => UniversalProjectileDamageModifier = value, true);
            Subs.CVar(_config, CCVars.PlaytestMeleeDamageModifier, value => UniversalMeleeDamageModifier = value, true);
            Subs.CVar(_config, CCVars.PlaytestProjectileDamageModifier, value => UniversalProjectileDamageModifier = value, true);
            Subs.CVar(_config, CCVars.PlaytestHitscanDamageModifier, value => UniversalHitscanDamageModifier = value, true);
            Subs.CVar(_config, CCVars.PlaytestReagentDamageModifier, value =>
            {
                UniversalReagentDamageModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            }, true);
            Subs.CVar(_config, CCVars.PlaytestReagentHealModifier, value =>
            {
                 UniversalReagentHealModifier = value;
                 _chemistryGuideData.ReloadAllReagentPrototypes();
            }, true);
            Subs.CVar(_config, CCVars.PlaytestExplosionDamageModifier, value => UniversalExplosionDamageModifier = value, true);
            Subs.CVar(_config, CCVars.PlaytestThrownDamageModifier, value => UniversalThrownDamageModifier = value, true);
            Subs.CVar(_config, CCVars.PlaytestTopicalsHealModifier, value => UniversalTopicalsHealModifier = value, true);
            Subs.CVar(_config, CCVars.PlaytestMobDamageModifier, value => UniversalMobDamageModifier = value, true);
        }

        /// <summary>
        ///     Initialize a damageable component
        /// </summary>
        private void DamageableInit(EntityUid uid, DamageableComponent component, ComponentInit _)
        {
            InitDamageTypes(component);
        }

        private void InitDamageTypes(DamageableComponent component)
        {
            if (component.DamageContainerID != null &&
                _prototypeManager.TryIndex<DamageContainerPrototype>(component.DamageContainerID,
                out var damageContainerPrototype))
            {
                // Initialize damage dictionary, using the types and groups from the damage
                // container prototype
                foreach (var type in damageContainerPrototype.SupportedTypes)
                {
                    component.Damage.DamageDict.TryAdd(type, FixedPoint2.Zero);
                }

                foreach (var groupId in damageContainerPrototype.SupportedGroups)
                {
                    var group = _prototypeManager.Index<DamageGroupPrototype>(groupId);
                    foreach (var type in group.DamageTypes)
                    {
                        component.Damage.DamageDict.TryAdd(type, FixedPoint2.Zero);
                    }
                }
            }
            else
            {
                // No DamageContainerPrototype was given. So we will allow the container to support all damage types
                foreach (var type in _prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
                {
                    component.Damage.DamageDict.TryAdd(type.ID, FixedPoint2.Zero);
                }
            }

            component.Damage.GetDamagePerGroup(_prototypeManager, component.DamagePerGroup);
            component.TotalDamage = component.Damage.GetTotal();
        }

        /// <summary>
        ///     Directly sets the damage specifier of a damageable component.
        /// </summary>
        /// <remarks>
        ///     Useful for some unfriendly folk. Also ensures that cached values are updated and that a damage changed
        ///     event is raised.
        /// </remarks>
        public void SetDamage(EntityUid uid, DamageableComponent damageable, DamageSpecifier damage, bool update = false, EntityUid? origin = null)
        {
            DamageSpecifier? delta = null;
            if (update)
                delta = damage - damageable.Damage;

            damageable.Damage = damage;
            DamageChanged(uid, damageable, delta, origin: origin);
        }

        /// <summary>
        ///     If the damage in a DamageableComponent was changed, this function should be called.
        /// </summary>
        /// <remarks>
        ///     This updates cached damage information, flags the component as dirty, and raises a damage changed event.
        ///     The damage changed event is used by other systems, such as damage thresholds.
        /// </remarks>
        public void DamageChanged(EntityUid uid, DamageableComponent component, DamageSpecifier? damageDelta = null,
            bool interruptsDoAfters = true, EntityUid? origin = null, EntityUid? body = null)
        {
            component.Damage.GetDamagePerGroup(_prototypeManager, component.DamagePerGroup);
            component.TotalDamage = component.Damage.GetTotal();
            Dirty(uid, component);

            if (_appearanceQuery.TryGetComponent(body ?? uid, out var appearance) && damageDelta != null)
            {
                var data = new DamageVisualizerGroupData(component.DamagePerGroup.Keys.ToList());
                _appearance.SetData(body ?? uid, DamageVisualizerKeys.DamageUpdateGroups, data, appearance);
            }
            RaiseLocalEvent(uid, new DamageChangedEvent(component, damageDelta, interruptsDoAfters, origin));
        }

        /// <summary>
        ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
        /// </summary>
        /// <remarks>
        ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
        ///     function just applies the container's resistances (unless otherwise specified) and then changes the
        ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
        /// </remarks>
        /// <returns>
        ///     Returns a <see cref="DamageSpecifier"/> with information about the actual damage changes. This will be
        ///     null if the user had no applicable components that can take damage.
        /// </returns>
        public DamageSpecifier? TryChangeDamage(EntityUid? uid, DamageSpecifier damage, bool ignoreResistances = false,
            bool interruptsDoAfters = true, DamageableComponent? damageable = null, BodyComponent? bodyComp = null, EntityUid? origin = null, bool splitLimbDamage = true)
        {
            if (!uid.HasValue)
            {
                return null;
            }

            if (damage.Empty)
            {
                return damage;
            }

            if (_damageableQuery.Resolve(uid.Value, ref damageable, false))
            {
                EntityUid? bodyUid = null;

                if (TryComp<BodyPartComponent>(uid, out var bodyPartComp))
                    bodyUid = bodyPartComp.Body;

                return ChangeDamage(uid.Value, damage, damageable, ignoreResistances, interruptsDoAfters, origin, body: bodyUid);
            }

            if (!_bodyQuery.Resolve(uid.Value, ref bodyComp, false))
            {
                return null;
            }

            var damageDict = TryChangeDamageBody(uid, damage, ignoreResistances, interruptsDoAfters, bodyComp, origin, splitLimbDamage);

            if (damageDict == null)
                return null;

            DamageSpecifier totalDamage = new();

            foreach (var (_, partDamage) in damageDict)
            {
                totalDamage += partDamage;
            }

            return totalDamage;
        }

        public Dictionary<EntityUid, DamageSpecifier>? TryChangeDamageBody(EntityUid? uid, DamageSpecifier damage, bool ignoreResistances = false,
            bool interruptsDoAfters = true, BodyComponent? body = null, EntityUid? origin = null, bool splitLimbDamage = true)
        {
            if (!uid.HasValue || !_bodyQuery.Resolve(uid.Value, ref body, false))
            {
                return null;
            }

            Dictionary<EntityUid, DamageSpecifier> damageDict = new ();

            if (damage.Empty)
            {
                return damageDict;
            }

            var parts = _body.GetBodyDamageable(uid.Value, body);

            var damagePerPart = damage / parts.Count;

            if (TryComp<DamagePartSelectorComponent>(origin, out var damageSelectorComp))
            {
                EntityUid? damagedBody = EntityUid.Invalid;

                foreach (var (part, damageable) in parts)
                {
                    if (!TryComp<BodyPartComponent>(part, out var partComp))
                        continue;

                    // cant deal damage so we need to flash the entity
                    if (partComp.OverallDamageScale == 0)
                        continue;

                    if (damagedBody != null)
                        damagedBody = partComp.Body;

                    if (damageSelectorComp.SelectedPart.Type != partComp.PartType || damageSelectorComp.SelectedPart.Side != partComp.Symmetry)
                        continue;

                    var newDamage = ChangeDamage(part, damage, damageable, ignoreResistances, interruptsDoAfters, origin, partComp.PartType, uid.Value);

                    if (newDamage == null)
                        continue;

                    damageDict.Add(part, newDamage);
                    damagedBody = null;
                }

                if (damagedBody != null && origin != null)
                    _color.RaiseEffect(Color.LimeGreen, new List<EntityUid>() { damagedBody.Value }, Filter.Pvs(damagedBody.Value, entityManager: EntityManager));

                return damageDict;
            }

            foreach (var (part, damageable) in parts)
            {
                BodyPartType? partType = TryComp<BodyPartComponent>(part, out var partComp) ? partComp.PartType : null;

                var limbDamage = damage;

                if (splitLimbDamage)
                    limbDamage = damagePerPart;

                var newDamage = ChangeDamage(part, limbDamage, damageable, ignoreResistances, interruptsDoAfters, origin, partType, uid.Value);

                if (newDamage == null)
                    continue;

                damageDict.Add(part, newDamage);
            }

            return damageDict;
        }

        internal DamageSpecifier? ChangeDamage(EntityUid uid, DamageSpecifier damage, DamageableComponent  damageable,
            bool ignoreResistances, bool interruptsDoAfters, EntityUid? origin, BodyPartType? partType = null, EntityUid? body = null)
        {
            var before = new BeforeDamageChangedEvent(damage, origin);
            RaiseLocalEvent(uid, ref before);

            if (before.Cancelled)
                return null;

            if (partType == null)
                return TryApplyDamage(uid, damage, ignoreResistances, interruptsDoAfters, damageable, origin);

            return TryApplyDamage(uid, damage, ignoreResistances, interruptsDoAfters, damageable, origin, partType, body);
        }

        internal DamageSpecifier? TryApplyDamage(EntityUid uid, DamageSpecifier damage, bool ignoreResistances,
            bool interruptsDoAfters, DamageableComponent damageable, EntityUid? origin, BodyPartType? part = null, EntityUid? body = null)
        {
            // Apply resistances
            if (!ignoreResistances)
            {
                if (damageable.DamageModifierSetId != null &&
                    _prototypeManager.TryIndex<DamageModifierSetPrototype>(damageable.DamageModifierSetId, out var modifierSet))
                {
                    // TODO DAMAGE PERFORMANCE
                    // use a local private field instead of creating a new dictionary here..
                    damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
                }

                var eventUid = body ?? uid;
                var ev = new DamageModifyEvent(damage, origin, part);
                RaiseLocalEvent(eventUid, ev);
                damage = ev.Damage;

                if (damage.Empty)
                {
                    return damage;
                }
            }

            damage = ApplyUniversalAllModifiers(damage);

            // TODO DAMAGE PERFORMANCE
            // Consider using a local private field instead of creating a new dictionary here.
            // Would need to check that nothing ever tries to cache the delta.
            var delta = new DamageSpecifier();
            delta.DamageDict.EnsureCapacity(damage.DamageDict.Count);

            var dict = damageable.Damage.DamageDict;
            foreach (var (type, value) in damage.DamageDict)
            {
                // CollectionsMarshal my beloved.
                if (!dict.TryGetValue(type, out var oldValue))
                    continue;

                var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
                if (newValue == oldValue)
                    continue;

                dict[type] = newValue;
                delta.DamageDict[type] = newValue - oldValue;
            }

            if (delta.DamageDict.Count > 0)
                DamageChanged(uid, damageable, delta, interruptsDoAfters, origin, body);

            return delta;
        }

        /// <summary>
        /// Applies the two univeral "All" modifiers, if set.
        /// Individual damage source modifiers are set in their respective code.
        /// </summary>
        /// <param name="damage">The damage to be changed.</param>
        public DamageSpecifier ApplyUniversalAllModifiers(DamageSpecifier damage)
        {
            // Checks for changes first since they're unlikely in normal play.
            if (UniversalAllDamageModifier == 1f && UniversalAllHealModifier == 1f)
                return damage;

            foreach (var (key, value) in damage.DamageDict)
            {
                if (value == 0)
                    continue;

                if (value > 0)
                {
                    damage.DamageDict[key] *= UniversalAllDamageModifier;
                    continue;
                }

                if (value < 0)
                {
                    damage.DamageDict[key] *= UniversalAllHealModifier;
                }
            }

            return damage;
        }

        /// <summary>
        ///     Sets all damage types supported by a <see cref="DamageableComponent"/> to the specified value.
        /// </summary>
        /// <remakrs>
        ///     Does nothing If the given damage value is negative.
        /// </remakrs>
        public void SetAllDamage(EntityUid uid, DamageableComponent component, FixedPoint2 newValue)
        {
            if (newValue < 0)
            {
                // invalid value
                return;
            }

            foreach (var type in component.Damage.DamageDict.Keys)
            {
                component.Damage.DamageDict[type] = newValue;
            }

            // Setting damage does not count as 'dealing' damage, even if it is set to a larger value, so we pass an
            // empty damage delta.
            DamageChanged(uid, component, new DamageSpecifier());
        }

        public void SetDamageModifierSetId(EntityUid uid, string? damageModifierSetId, DamageableComponent? comp = null)
        {
            if (!_damageableQuery.Resolve(uid, ref comp))
                return;

            comp.DamageModifierSetId = damageModifierSetId;
            Dirty(uid, comp);
        }

        private void DamageableGetState(EntityUid uid, DamageableComponent component, ref ComponentGetState args)
        {
            if (_netMan.IsServer)
            {
                args.State = new DamageableComponentState(component.Damage.DamageDict, component.DamageContainerID, component.DamageModifierSetId, component.HealthBarThreshold);
            }
            else
            {
                // avoid mispredicting damage on newly spawned entities.
                args.State = new DamageableComponentState(component.Damage.DamageDict.ShallowClone(), component.DamageContainerID, component.DamageModifierSetId, component.HealthBarThreshold);
            }
        }


        private void OnIrradiated(EntityUid uid, DamageableComponent component, OnIrradiatedEvent args)
        {
            Irradiate(uid, args.TotalRads, component.RadiationDamageTypeIDs);
        }

        public void Irradiate(EntityUid uid, FixedPoint2 damageValue, List<ProtoId<DamageTypePrototype>> radiationIDs)
        {
            // Radiation should really just be a damage group instead of a list of types.
            DamageSpecifier damage = new();
            foreach (var typeId in radiationIDs)
            {
                damage.DamageDict.Add(typeId, damageValue);
            }

            TryChangeDamage(uid, damage, interruptsDoAfters: false);
        }

        private void OnRejuvenate(EntityUid uid, DamageableComponent component, RejuvenateEvent args)
        {
            TryComp<MobThresholdsComponent>(uid, out var thresholds);
            _mobThreshold.SetAllowRevives(uid, true, thresholds); // do this so that the state changes when we set the damage
            SetAllDamage(uid, component, 0);
            _mobThreshold.SetAllowRevives(uid, false, thresholds);
        }

        private void DamageableHandleState(EntityUid uid, DamageableComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not DamageableComponentState state)
            {
                return;
            }

            component.DamageContainerID = state.DamageContainerId;
            component.DamageModifierSetId = state.ModifierSetId;
            component.HealthBarThreshold = state.HealthBarThreshold;

            // Has the damage actually changed?
            DamageSpecifier newDamage = new() { DamageDict = new(state.DamageDict) };
            var delta = component.Damage - newDamage;
            delta.TrimZeros();

            if (!delta.Empty)
            {
                EntityUid? body = null;

                if (TryComp<BodyPartComponent>(uid, out var partComp) && partComp.Body != null)
                    body = partComp.Body.Value;

                component.Damage = newDamage;
                DamageChanged(uid, component, delta, body: body);
            }
        }
    }

    /// <summary>
    ///     Raised before damage is done, so stuff can cancel it if necessary.
    /// </summary>
    [ByRefEvent]
    public record struct BeforeDamageChangedEvent(DamageSpecifier Damage, EntityUid? Origin = null, bool Cancelled = false);

    /// <summary>
    ///     Raised on an entity when damage is about to be dealt,
    ///     in case anything else needs to modify it other than the base
    ///     damageable component.
    ///
    ///     For example, armor.
    /// </summary>
    public sealed class DamageModifyEvent : EntityEventArgs, IInventoryRelayEvent
    {
        // Whenever locational damage is a thing, this should just check only that bit of armour.
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

        public readonly DamageSpecifier OriginalDamage;
        public DamageSpecifier Damage;
        public EntityUid? Origin;
        public BodyPartType? BodyPart;

        public DamageModifyEvent(DamageSpecifier damage, EntityUid? origin = null, BodyPartType? part = null)
        {
            OriginalDamage = damage;
            Damage = damage;
            Origin = origin;
            BodyPart = part;
        }
    }

    public sealed class DamageChangedEvent : EntityEventArgs
    {
        /// <summary>
        ///     This is the component whose damage was changed.
        /// </summary>
        /// <remarks>
        ///     Given that nearly every component that cares about a change in the damage, needs to know the
        ///     current damage values, directly passing this information prevents a lot of duplicate
        ///     Owner.TryGetComponent() calls.
        /// </remarks>
        public readonly DamageableComponent Damageable;

        /// <summary>
        ///     The amount by which the damage has changed. If the damage was set directly to some number, this will be
        ///     null.
        /// </summary>
        public readonly DamageSpecifier? DamageDelta;

        /// <summary>
        ///     Was any of the damage change dealing damage, or was it all healing?
        /// </summary>
        public readonly bool DamageIncreased;

        /// <summary>
        ///     Does this event interrupt DoAfters?
        ///     Note: As provided in the constructor, this *does not* account for DamageIncreased.
        ///     As written into the event, this *does* account for DamageIncreased.
        /// </summary>
        public readonly bool InterruptsDoAfters;

        /// <summary>
        ///     Contains the entity which caused the change in damage, if any was responsible.
        /// </summary>
        public readonly EntityUid? Origin;

        public DamageChangedEvent(DamageableComponent damageable, DamageSpecifier? damageDelta, bool interruptsDoAfters, EntityUid? origin)
        {
            Damageable = damageable;
            DamageDelta = damageDelta;
            Origin = origin;

            if (DamageDelta == null)
                return;

            foreach (var damageChange in DamageDelta.DamageDict.Values)
            {
                if (damageChange > 0)
                {
                    DamageIncreased = true;
                    break;
                }
            }
            InterruptsDoAfters = interruptsDoAfters && DamageIncreased;
        }
    }
}
