using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
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

        SubscribeLocalEvent<WoundRecieverComponent, DamageModifyEvent>(OnDamage);
    }

    private void OnDamage(EntityUid uid, WoundRecieverComponent component, DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        // only one wound per limb
        if (HasComp<WoundComponent>(uid))
            return;

        // TODO: select based on damage and other factors
        var woundId = _random.Pick(component.SelectableWounds);

        if (!_protoManager.TryIndex(woundId, out WoundPrototype? wound))
        {
            Log.Debug($"{woundId} is not a valid wound prototype.");
            return;
        }

        EntityManager.AddComponents(uid, wound.Components, wound.RemoveExisting);

        if (!TryComp<BodyPartComponent>(uid, out var partComp) || partComp.Body == null)
            return;

        var woundBody = EnsureComp<WoundBodyComponent>(partComp.Body.Value);
        woundBody.Limbs.Add(uid);
        Dirty(partComp.Body.Value, woundBody);
    }
}
