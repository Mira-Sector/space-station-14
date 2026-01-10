using System.Linq;
using Content.Shared.Store;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Content.Server.Mind;
using Content.Shared.Mind;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's access.
/// Supports only AccessReader
/// </summary>
public sealed partial class BuyerAccessCondition : ListingCondition
{
    [DataField("access", required: false)]
    public string? Access = null;

    public override bool Condition(ListingConditionArgs args)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var ent = args.EntityManager;

        if (!ent.TryGetComponent<MindComponent>(args.Buyer, out var mind)
            || mind.CurrentEntity is null) return false;
        var buyer = mind.CurrentEntity.Value;

        var _accessReader = ent.System<AccessReaderSystem>();

        if (args.StoreEntity == null
            || !ent.TryGetComponent<AccessReaderComponent>(args.StoreEntity, out var accessReader))
            return true;

        if (Access != null)
        {
            var accesses = _accessReader.FindAccessTags(buyer);
            if (accesses.Any(a => a.ToString() == Access))
                return true;
        }

        else if (_accessReader.IsAllowed(buyer, args.StoreEntity.Value, accessReader)
                || ent.HasComponent<EmaggedComponent>(args.StoreEntity.Value))
            return true;

        return false;
    }
}
