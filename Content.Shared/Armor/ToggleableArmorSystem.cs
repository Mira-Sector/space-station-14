using Content.Shared.Body.Part;
using JetBrains.Annotations;

namespace Content.Shared.Armor;

public sealed partial class ToggleableArmorSystem : EntitySystem
{
    [PublicAPI]
    public void EnableArmorPart(Entity<ArmorComponent?, ToggleableArmorComponent?> ent, BodyPartType part)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return;

        if (!ent.Comp2.DisabledModifiers.TryGetValue(part, out var disabledModifier))
            return;

        ent.Comp2.DisabledModifiers.Remove(part);
        Dirty(ent.Owner, ent.Comp2);

        foreach (var (parts, modifier) in ent.Comp1.Modifiers)
        {
            // do we already have the modifier
            if (modifier != disabledModifier)
                continue;

            // does the parts list already affect the target limb
            if (parts.Contains(part))
                return;

            parts.Add(part);
            Dirty(ent.Owner, ent.Comp1);
            return;
        }

        // no matching modifier found
        // add a new one
        ent.Comp1.Modifiers.Add([part], disabledModifier);
        Dirty(ent.Owner, ent.Comp1);
    }

    [PublicAPI]
    public void DisableArmorPart(Entity<ArmorComponent?, ToggleableArmorComponent?> ent, BodyPartType part)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return;

        foreach (var (parts, modifier) in ent.Comp1.Modifiers)
        {
            if (!parts.Contains(part))
                continue;

            // overwrite any existing data
            ent.Comp2.DisabledModifiers.Remove(part);

            ent.Comp2.DisabledModifiers.Add(part, modifier);
            ent.Comp1.Modifiers.Remove(parts);
            Dirty(ent.Owner, ent.Comp1);
            Dirty(ent.Owner, ent.Comp1);
        }
    }

    [PublicAPI]
    public void ToggleArmorPart(Entity<ArmorComponent?, ToggleableArmorComponent?> ent, BodyPartType part)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2))
            return;

        if (ent.Comp2.DisabledModifiers.ContainsKey(part))
            EnableArmorPart((ent.Owner, null, ent.Comp2), part);
        else
            DisableArmorPart((ent.Owner, null, ent.Comp2), part);
    }
}
