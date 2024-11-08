using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.Interaction;
using Content.Shared.Wounds.Components;

namespace Content.Shared.Wounds.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WoundRecieverComponent, DamageModifyEvent>(OnDamage);
        SubscribeLocalEvent<WoundBodyComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnDamage(EntityUid uid, WoundRecieverComponent component, DamageModifyEvent args)
    {
        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp) || bodyPartComp.Body == null)
            return;

        EnsureComp<WoundComponent>(uid);
        EnsureComp<WoundBodyComponent>(bodyPartComp.Body.Value);
    }

    private void OnAfterInteract(EntityUid uid, WoundBodyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.CanReach || args.Target == null)
            return;

        if (!TryComp<BodyComponent>(uid, out var bodyComp))
            return;

        if (!TryComp<DamagePartSelectorComponent>(args.User, out var selectorComp))
            return;

        var parts = _body.GetBodyChildren(uid, bodyComp);

        foreach (var (_, partComp) in parts)
        {
            if (partComp.PartType != selectorComp.SelectedPart.Type)
                continue;

            if (partComp.Symmetry != selectorComp.SelectedPart.Side)
                continue;
        }
    }
}
