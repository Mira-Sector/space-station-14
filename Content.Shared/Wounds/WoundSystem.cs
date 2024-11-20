using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.Wounds.Components;
using Content.Shared.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Wounds.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundBodyComponent, DamageModifyEvent>(OnDamage);
    }

    private void OnDamage(EntityUid uid, WoundBodyComponent component, DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!TryComp<BodyComponent>(uid, out var bodyComp))
            return;

        if (!TryComp<DamagePartSelectorComponent>(args.Origin, out var selectorComp))
            return;

        var parts = _body.GetBodyChildren(uid, bodyComp);

        foreach (var (partUid, partComp) in parts)
        {
            if (partComp.PartType != selectorComp.SelectedPart.Type)
                continue;

            if (partComp.Symmetry != selectorComp.SelectedPart.Side)
                continue;

            if (!TryComp<WoundRecieverComponent>(partUid, out var woundRecieverComp))
                continue;

            // only one wound per limb
            if (HasComp<WoundComponent>(partUid))
                return;

            // TODO: select based on damage and other factors
            var woundId = _random.Pick(woundRecieverComp.SelectableWounds);

            if (!_protoManager.TryIndex(woundId, out WoundPrototype? wound))
            {
                Log.Debug($"{woundId} is not a valid wound prototype.");
                return;
            }

            EntityManager.AddComponents(uid, wound.Components, wound.RemoveExisting);

            return;
        }
    }
}
