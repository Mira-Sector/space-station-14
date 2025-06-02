using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Localizations;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Armor;

/// <summary>
///     This handles logic relating to <see cref="ArmorComponent" />
/// </summary>
public abstract class SharedArmorSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(OnCoefficientQuery);
        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<ArmorComponent, BorgModuleRelayedEvent<DamageModifyEvent>>(OnBorgDamageModify);
        SubscribeLocalEvent<ArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
    }

    /// <summary>
    /// Get the total Damage reduction value of all equipment caught by the relay.
    /// </summary>
    /// <param name="ent">The item that's being relayed to</param>
    /// <param name="args">The event, contains the running count of armor percentage as a coefficient</param>
    private void OnCoefficientQuery(Entity<ArmorComponent> ent, ref InventoryRelayedEvent<CoefficientQueryEvent> args)
    {
        foreach (var parts in ent.Comp.Modifiers.Keys)
        {
            if (!parts.Contains(ent.Comp.BasePart))
                continue;

            foreach (var armorCoefficient in ent.Comp.Modifiers[parts].Coefficients)
            {
                args.Args.DamageModifiers.Coefficients[armorCoefficient.Key] = args.Args.DamageModifiers.Coefficients.TryGetValue(armorCoefficient.Key, out var coefficient) ? coefficient * armorCoefficient.Value : armorCoefficient.Value;
            }
        }
    }

    private void OnDamageModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (!TryGetModifier(component, args.Args.BodyPart, out var part))
            return;

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers[part]);
    }

    private void OnBorgDamageModify(EntityUid uid, ArmorComponent component,
        ref BorgModuleRelayedEvent<DamageModifyEvent> args)
    {
        if (!TryGetModifier(component, args.Args.BodyPart, out var part))
            return;

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers[part]);
    }

    private static bool TryGetModifier(ArmorComponent component, BodyPartType? part, [NotNullWhen(true)] out HashSet<BodyPartType>? modifier)
    {
        part ??= component.BasePart;

        foreach (var key in component.Modifiers.Keys)
        {
            if (!key.Contains(part.Value))
                continue;

            modifier = key;
            return true;
        }

        modifier = null;
        return false;
    }

    private void OnArmorVerbExamine(EntityUid uid, ArmorComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !component.ShowArmorOnExamine)
            return;

        var examineMarkup = GetArmorExamine(component.Modifiers);

        var ev = new ArmorExamineEvent(examineMarkup);
        RaiseLocalEvent(uid, ref ev);

        _examine.AddDetailedExamineVerb(args, component, examineMarkup,
            Loc.GetString("armor-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("armor-examinable-verb-message"));
    }

    private FormattedMessage GetArmorExamine(Dictionary<HashSet<BodyPartType>, DamageModifierSet> armorModifiers)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("armor-examine"));

        foreach (var (parts, modifier) in armorModifiers)
        {
            msg.PushNewline();

            var partsLoc = ContentLocalizationManager.FormatList(parts
                .Select(p => Loc.GetString("armor-part-name", ("part", p)))
                .ToList());

            msg.AddMarkupOrThrow(Loc.GetString("armor-part-wrap", ("message", partsLoc)));

            foreach (var coefficientArmor in modifier.Coefficients)
            {
                msg.PushNewline();

                var armorType = Loc.GetString("armor-damage-type-" + coefficientArmor.Key.ToLower());
                msg.AddMarkupOrThrow(Loc.GetString("armor-coefficient-value",
                    ("type", armorType),
                    ("value", MathF.Round((1f - coefficientArmor.Value) * 100, 1))
                ));
            }

            foreach (var flatArmor in modifier.FlatReduction)
            {
                msg.PushNewline();

                var armorType = Loc.GetString("armor-damage-type-" + flatArmor.Key.ToLower());
                msg.AddMarkupOrThrow(Loc.GetString("armor-reduction-value",
                    ("type", armorType),
                    ("value", flatArmor.Value)
                ));
            }
        }

        msg.PushNewline();

        return msg;
    }
}
