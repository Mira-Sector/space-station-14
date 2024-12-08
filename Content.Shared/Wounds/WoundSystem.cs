using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
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

        SubscribeLocalEvent<WoundRecieverComponent, LimbStateChangedEvent>(OnStateChanged);
    }

    private void OnStateChanged(EntityUid uid, WoundRecieverComponent component, LimbStateChangedEvent args)
    {
        // only one wound per limb
        if (args.OldState != WoundState.Healthy && args.NewState != WoundState.Damaged)
            return;

        if (component.CurrentWound != null)
            return;

        // TODO: select based on damage and other factors
        var woundId = _random.Pick(component.SelectableWounds);

        if (!_protoManager.TryIndex(woundId, out WoundPrototype? wound))
        {
            Log.Debug($"{woundId} is not a valid wound prototype.");
            return;
        }

        EntityManager.AddComponents(uid, wound.Components, wound.RemoveExisting);
        component.CurrentWound = wound;

        var woundBody = EnsureComp<WoundBodyComponent>(args.Body);
        woundBody.Limbs.Add(uid);
        Dirty(args.Body, woundBody);
    }
}
