using Content.Shared.Armor;
using Content.Shared.Cargo;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Server.Armor;

/// <inheritdoc/>
public sealed class ArmorSystem : SharedArmorSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, PriceCalculationEvent>(GetArmorPrice);
    }

    private void GetArmorPrice(EntityUid uid, ArmorComponent component, ref PriceCalculationEvent args)
    {
        if (!TryGetModifier(component, null, out var modifier))
            return;

        foreach (var coefficient in modifier.Modifier.Coefficients)
        {
            var damageType = _protoManager.Index<DamageTypePrototype>(coefficient.Key);
            args.Price += component.PriceMultiplier * damageType.ArmorPriceCoefficient * 100 * (1 - coefficient.Value);
        }

        foreach (var flat in modifier.Modifier.FlatReduction)
        {
            var damageType = _protoManager.Index<DamageTypePrototype>(flat.Key);
            args.Price += component.PriceMultiplier * damageType.ArmorPriceFlat * flat.Value;
        }
    }
}
