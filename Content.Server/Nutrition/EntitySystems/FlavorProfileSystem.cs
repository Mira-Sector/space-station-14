using Content.Server.Nutrition.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Nutrition;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
///     Deals with flavor profiles when you eat something.
/// </summary>
public sealed class FlavorProfileSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    private const string BackupFlavorMessage = "flavor-profile-unknown";
    private const string NoTasteFlavorMessage = "flavor-profile-no-taste";

    [ValidatePrototypeId<OrganPrototype>]
    private const string FlavorOrgan = "Tongue";

    private int FlavorLimit => _configManager.GetCVar(CCVars.FlavorLimit);

    public string GetLocalizedFlavorsMessage(EntityUid uid, EntityUid user, Solution solution,
        FlavorProfileComponent? flavorProfile = null)
    {
        if (!Resolve(uid, ref flavorProfile, false))
        {
            return Loc.GetString(BackupFlavorMessage);
        }

        if (!CanTasteFlavor(user))
            return Loc.GetString(NoTasteFlavorMessage);

        var flavors = new HashSet<string>(flavorProfile.Flavors);
        flavors.UnionWith(GetFlavorsFromReagents(solution, FlavorLimit - flavors.Count, flavorProfile.IgnoreReagents));

        var ev = new FlavorProfileModificationEvent(user, flavors);
        RaiseLocalEvent(ev);
        RaiseLocalEvent(uid, ev);
        RaiseLocalEvent(user, ev);

        return FlavorsToFlavorMessage(flavors);
    }

    public string GetLocalizedFlavorsMessage(EntityUid user, Solution solution)
    {
        if (!CanTasteFlavor(user))
            return Loc.GetString(NoTasteFlavorMessage);

        var flavors = GetFlavorsFromReagents(solution, FlavorLimit);
        var ev = new FlavorProfileModificationEvent(user, flavors);
        RaiseLocalEvent(user, ev, true);

        return FlavorsToFlavorMessage(flavors);
    }

    private bool CanTasteFlavor(Entity<BodyComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return true;

        var organs = _bodySystem.GetBodyOrgans(ent.Owner, ent.Comp);

        foreach (var (_, organComp) in organs)
        {
            if (organComp.OrganType == FlavorOrgan)
                return true;
        }

        return false;
    }

    private string FlavorsToFlavorMessage(HashSet<string> flavorSet)
    {
        var flavors = new List<FlavorPrototype>();
        foreach (var flavor in flavorSet)
        {
            if (string.IsNullOrEmpty(flavor) || !_prototypeManager.TryIndex<FlavorPrototype>(flavor, out var flavorPrototype))
            {
                continue;
            }

            flavors.Add(flavorPrototype);
        }

        flavors.Sort((a, b) => a.FlavorType.CompareTo(b.FlavorType));

        if (flavors.Count == 1 && !string.IsNullOrEmpty(flavors[0].FlavorDescription))
        {
            return Loc.GetString("flavor-profile", ("flavor", Loc.GetString(flavors[0].FlavorDescription)));
        }

        if (flavors.Count > 1)
        {
            var lastFlavor = Loc.GetString(flavors[^1].FlavorDescription);
            var allFlavors = string.Join(", ", flavors.GetRange(0, flavors.Count - 1).Select(i => Loc.GetString(i.FlavorDescription)));
            return Loc.GetString("flavor-profile-multiple", ("flavors", allFlavors), ("lastFlavor", lastFlavor));
        }

        return Loc.GetString(BackupFlavorMessage);
    }

    private HashSet<string> GetFlavorsFromReagents(Solution solution, int desiredAmount, HashSet<string>? toIgnore = null)
    {
        var flavors = new HashSet<string>();
        foreach (var (reagent, quantity) in solution.GetReagentPrototypes(_prototypeManager))
        {
            if (toIgnore != null && toIgnore.Contains(reagent.ID))
            {
                continue;
            }

            if (flavors.Count == desiredAmount)
            {
                break;
            }

            // don't care if the quantity is negligible
            if (quantity < reagent.FlavorMinimum)
            {
                continue;
            }

            if (reagent.Flavor != null)
                flavors.Add(reagent.Flavor);
        }

        return flavors;
    }
}

public sealed class FlavorProfileModificationEvent : EntityEventArgs
{
    public FlavorProfileModificationEvent(EntityUid user, HashSet<string> flavors)
    {
        User = user;
        Flavors = flavors;
    }

    public EntityUid User { get; }
    public HashSet<string> Flavors { get; }
}
