using Content.Shared.Body.Part;
using JetBrains.Annotations;
using System.Linq;

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

        foreach (var (parts, _) in ent.Comp1.Modifiers)
        {
            if (!parts.Contains(part))
                continue;

            parts.Remove(part);

            if (parts.Any())
                continue;

            ent.Comp1.Modifiers.Remove(parts);
        }

        ent.Comp1.Modifiers.Add([part], disabledModifier);
        ent.Comp2.DisabledModifiers.Remove(part);
        Dirty(ent.Owner, ent.Comp1);
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
}
