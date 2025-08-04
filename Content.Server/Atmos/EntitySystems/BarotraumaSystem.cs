using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Modules.ModSuit.Events;
using Robust.Shared.Containers;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed class BarotraumaSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly SharedBodySystem _bodySystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        private const float UpdateTimer = 1f;
        private float _timer;

        private EntityQuery<OptionalPressureProtectionComponent> _optionalProtectionQuery;

        public override void Initialize()
        {
            SubscribeLocalEvent<PressureProtectionComponent, GotEquippedEvent>(OnPressureProtectionEquipped);
            SubscribeLocalEvent<PressureProtectionComponent, GotUnequippedEvent>(OnPressureProtectionUnequipped);
            SubscribeLocalEvent<PressureProtectionComponent, ModSuitSealedEvent>(OnPressureProtectionModSuitSeal);
            SubscribeLocalEvent<PressureProtectionComponent, ModSuitUnsealedEvent>(OnPressureProtectionModSuitSeal);
            SubscribeLocalEvent<PressureProtectionComponent, ComponentInit>(OnUpdateResistance);
            SubscribeLocalEvent<PressureProtectionComponent, ComponentRemove>(OnUpdateResistance);

            SubscribeLocalEvent<PressureImmunityComponent, ComponentInit>(OnPressureImmuneInit);
            SubscribeLocalEvent<PressureImmunityComponent, ComponentRemove>(OnPressureImmuneRemove);

            _optionalProtectionQuery = GetEntityQuery<OptionalPressureProtectionComponent>();
        }

        private void OnPressureImmuneInit(EntityUid uid, PressureImmunityComponent pressureImmunity, ComponentInit args)
        {
            if (TryComp<BarotraumaComponent>(uid, out var barotrauma))
            {
                barotrauma.HasImmunity = true;
            }
        }

        private void OnPressureImmuneRemove(EntityUid uid, PressureImmunityComponent pressureImmunity, ComponentRemove args)
        {
            if (TryComp<BarotraumaComponent>(uid, out var barotrauma))
            {
                barotrauma.HasImmunity = false;
            }
        }

        /// <summary>
        /// Generic method for updating resistance on component Lifestage events
        /// </summary>
        private void OnUpdateResistance(EntityUid uid, PressureProtectionComponent pressureProtection, EntityEventArgs args)
        {
            if (TryComp<BarotraumaComponent>(uid, out var barotrauma))
                UpdateCachedResistances(uid, barotrauma);
        }

        private void OnPressureProtectionEquipped(EntityUid uid, PressureProtectionComponent pressureProtection, GotEquippedEvent args)
        {
            if (TryComp<BarotraumaComponent>(args.Equipee, out var barotrauma) && ContainsSlot(barotrauma, args.Slot))
                UpdateCachedResistances(args.Equipee, barotrauma);
        }

        private void OnPressureProtectionUnequipped(EntityUid uid, PressureProtectionComponent pressureProtection, GotUnequippedEvent args)
        {
            if (TryComp<BarotraumaComponent>(args.Equipee, out var barotrauma) && ContainsSlot(barotrauma, args.Slot))
                UpdateCachedResistances(args.Equipee, barotrauma);
        }

        private void OnPressureProtectionModSuitSeal(EntityUid uid, PressureProtectionComponent pressureProtection, BaseModSuitSealEvent args)
        {
            if (!TryComp<BarotraumaComponent>(args.Wearer, out var barotrauma))
                return;

            UpdateCachedResistances(args.Wearer!.Value, barotrauma);
        }

        /// <summary>
        /// Computes the pressure resistance for the entity coming from the equipment and any innate resistance.
        /// The ProtectionSlots field of the Barotrauma component specifies which parts must be protected for the protection to have any effect.
        /// </summary>
        private void UpdateCachedResistances(EntityUid uid, BarotraumaComponent barotrauma)
        {
            Dictionary<string, EntityUid?> slots = [];
            slots.EnsureCapacity(barotrauma.ProtectionSlots.Count + barotrauma.OptionalProtectionSlots.Count);

            foreach (var slot in barotrauma.ProtectionSlots)
                slots[slot] = null;

            var inv = CompOrNull<InventoryComponent>(uid);
            var contMan = CompOrNull<ContainerManagerComponent>(uid);

            if (inv != null && contMan != null)
            {
                foreach (var optionalSlot in barotrauma.OptionalProtectionSlots)
                {
                    if (!_inventorySystem.TryGetSlotEntity(uid, optionalSlot, out var optionalSlotEnt, inv, contMan))
                        continue;

                    if (_optionalProtectionQuery.HasComp(optionalSlotEnt))
                        slots[optionalSlot] = optionalSlotEnt;
                }
            }

            if (slots.Any())
            {
                if (inv == null || contMan == null)
                    return;

                var hPModifier = float.MinValue;
                var hPMultiplier = float.MinValue;
                var lPModifier = float.MaxValue;
                var lPMultiplier = float.MaxValue;

                foreach (var (slot, slotEnt) in slots)
                {
                    var equipment = slotEnt;

                    if (equipment == null)
                    {
                        if (!_inventorySystem.TryGetSlotEntity(uid, slot, out equipment, inv, contMan))
                        {
                            hPModifier = 0f;
                            hPMultiplier = 1f;
                            lPModifier = 0f;
                            lPMultiplier = 1f;
                            break;
                        }
                    }

                    if (!TryGetPressureProtectionValues(equipment.Value,
                            out var itemHighMultiplier,
                            out var itemHighModifier,
                            out var itemLowMultiplier,
                            out var itemLowModifier))
                    {
                        // Missing protection, skin is exposed.
                        hPModifier = 0f;
                        hPMultiplier = 1f;
                        lPModifier = 0f;
                        lPMultiplier = 1f;
                        break;
                    }

                    // The entity is as protected as its weakest part protection
                    hPModifier = Math.Max(hPModifier, itemHighModifier.Value);
                    hPMultiplier = Math.Max(hPMultiplier, itemHighMultiplier.Value);
                    lPModifier = Math.Min(lPModifier, itemLowModifier.Value);
                    lPMultiplier = Math.Min(lPMultiplier, itemLowMultiplier.Value);
                }

                barotrauma.HighPressureModifier = hPModifier;
                barotrauma.HighPressureMultiplier = hPMultiplier;
                barotrauma.LowPressureModifier = lPModifier;
                barotrauma.LowPressureMultiplier = lPMultiplier;
            }

            // any innate pressure resistance ?
            if (TryGetPressureProtectionValues(uid,
                    out var highMultiplier,
                    out var highModifier,
                    out var lowMultiplier,
                    out var lowModifier))
            {
                barotrauma.HighPressureModifier += highModifier.Value;
                barotrauma.HighPressureMultiplier *= highMultiplier.Value;
                barotrauma.LowPressureModifier += lowModifier.Value;
                barotrauma.LowPressureMultiplier *= lowMultiplier.Value;
            }
        }

        /// <summary>
        /// Returns adjusted pressure after having applied resistances from equipment and innate (if any), to check against a low pressure hazard threshold
        /// </summary>
        public float GetFeltLowPressure(EntityUid uid, BarotraumaComponent barotrauma, float environmentPressure)
        {
            if (barotrauma.HasImmunity)
                return Atmospherics.OneAtmosphere;

            var modified = (environmentPressure + barotrauma.LowPressureModifier) * barotrauma.LowPressureMultiplier;
            return Math.Min(modified, Atmospherics.OneAtmosphere);
        }

        /// <summary>
        /// Returns adjusted pressure after having applied resistances from equipment and innate (if any), to check against a high pressure hazard threshold
        /// </summary>
        public float GetFeltHighPressure(EntityUid uid, BarotraumaComponent barotrauma, float environmentPressure)
        {
            if (barotrauma.HasImmunity)
                return Atmospherics.OneAtmosphere;

            var modified = (environmentPressure + barotrauma.HighPressureModifier) * barotrauma.HighPressureMultiplier;
            return Math.Max(modified, Atmospherics.OneAtmosphere);
        }

        private static bool ContainsSlot(BarotraumaComponent component, string slot)
        {
            if (component.ProtectionSlots.Contains(slot))
                return true;

            return component.OptionalProtectionSlots.Contains(slot);
        }

        public bool TryGetPressureProtectionValues(
            Entity<PressureProtectionComponent?> ent,
            [NotNullWhen(true)] out float? highMultiplier,
            [NotNullWhen(true)] out float? highModifier,
            [NotNullWhen(true)] out float? lowMultiplier,
            [NotNullWhen(true)] out float? lowModifier)
        {
            highMultiplier = null;
            highModifier = null;
            lowMultiplier = null;
            lowModifier = null;
            if (!Resolve(ent, ref ent.Comp, false))
                return false;

            var comp = ent.Comp;
            var ev = new GetPressureProtectionValuesEvent
            {
                HighPressureMultiplier = comp.HighPressureMultiplier,
                HighPressureModifier = comp.HighPressureModifier,
                LowPressureMultiplier = comp.LowPressureMultiplier,
                LowPressureModifier = comp.LowPressureModifier
            };
            RaiseLocalEvent(ent, ref ev);
            highMultiplier = ev.HighPressureMultiplier;
            highModifier = ev.HighPressureModifier;
            lowMultiplier = ev.LowPressureMultiplier;
            lowModifier = ev.LowPressureModifier;
            return true;
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTimer)
                return;

            _timer -= UpdateTimer;

            var enumerator = EntityQueryEnumerator<BarotraumaComponent>();
            while (enumerator.MoveNext(out var uid, out var barotrauma))
            {
                var bodyDamage = _bodySystem.GetBodyDamage(uid);

                DamageSpecifier damage;

                if (bodyDamage != null)
                    damage = bodyDamage;
                else if (TryComp<DamageableComponent>(uid, out var damageable))
                    damage = damageable.Damage;
                else
                    continue;

                var totalDamage = FixedPoint2.Zero;
                foreach (var (barotraumaDamageType, _) in barotrauma.Damage.DamageDict)
                {
                    if (!damage.DamageDict.ContainsKey(barotraumaDamageType))
                        continue;

                    totalDamage += damage.DamageDict[barotraumaDamageType];
                }
                if (totalDamage >= barotrauma.MaxDamage)
                    continue;

                var pressure = 1f;

                if (_atmosphereSystem.GetContainingMixture(uid) is {} mixture)
                {
                    pressure = MathF.Max(mixture.Pressure, 1f);
                }

                pressure = pressure switch
                {
                    // Adjust pressure based on equipment. Works differently depending on if it's "high" or "low".
                    <= Atmospherics.WarningLowPressure => GetFeltLowPressure(uid, barotrauma, pressure),
                    >= Atmospherics.WarningHighPressure => GetFeltHighPressure(uid, barotrauma, pressure),
                    _ => pressure
                };

                if (pressure <= Atmospherics.HazardLowPressure)
                {
                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    _damageableSystem.TryChangeDamage(uid, barotrauma.Damage * Atmospherics.LowPressureDamage, true, false);

                    if (!barotrauma.TakingDamage)
                    {
                        barotrauma.TakingDamage = true;
                        _adminLogger.Add(LogType.Barotrauma, $"{ToPrettyString(uid):entity} started taking low pressure damage");
                    }

                    _alertsSystem.ShowAlert(uid, barotrauma.LowPressureAlert, 2);
                }
                else if (pressure >= Atmospherics.HazardHighPressure)
                {
                    var damageScale = MathF.Min(((pressure / Atmospherics.HazardHighPressure) - 1) * Atmospherics.PressureDamageCoefficient, Atmospherics.MaxHighPressureDamage);

                    // Deal damage and ignore resistances. Resistance to pressure damage should be done via pressure protection gear.
                    _damageableSystem.TryChangeDamage(uid, barotrauma.Damage * damageScale, true, false);

                    if (!barotrauma.TakingDamage)
                    {
                        barotrauma.TakingDamage = true;
                        _adminLogger.Add(LogType.Barotrauma, $"{ToPrettyString(uid):entity} started taking high pressure damage");
                    }

                    _alertsSystem.ShowAlert(uid, barotrauma.HighPressureAlert, 2);
                }
                else
                {
                    // Within safe pressure limits
                    if (barotrauma.TakingDamage)
                    {
                        barotrauma.TakingDamage = false;
                        _adminLogger.Add(LogType.Barotrauma, $"{ToPrettyString(uid):entity} stopped taking pressure damage");
                    }

                    // Set correct alert.
                    switch (pressure)
                    {
                        case <= Atmospherics.WarningLowPressure:
                            _alertsSystem.ShowAlert(uid, barotrauma.LowPressureAlert, 1);
                            break;
                        case >= Atmospherics.WarningHighPressure:
                            _alertsSystem.ShowAlert(uid, barotrauma.HighPressureAlert, 1);
                            break;
                        default:
                            _alertsSystem.ClearAlertCategory(uid, barotrauma.PressureAlertCategory);
                            break;
                    }
                }
            }
        }
    }
}
