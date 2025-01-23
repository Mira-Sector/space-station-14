using Content.Shared.Body.Part;
using Robust.Shared.Prototypes;
using Content.Shared.Surgery.Components;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryRecieverComponent, ComponentInit>(OnLimbInit);
    }

    private void OnLimbInit(EntityUid uid, SurgeryRecieverComponent component, ComponentInit args)
    {
        component.Graph = MergeGraphs(component.AvailableSurgeries);

        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        EnsureComp<SurgeryRecieverBodyComponent>(body, out var surgeryBodyComp);
        surgeryBodyComp.Limbs.Add(uid);
    }
}
