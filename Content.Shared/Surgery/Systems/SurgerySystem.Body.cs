using Content.Shared.Body.Part;
using Content.Shared.Surgery.Components;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem
{
    private void InitializeBody()
    {
        SubscribeLocalEvent<SurgeryRecieverComponent, ComponentStartup>(OnLimbStartup);
    }

    private void OnLimbStartup(EntityUid uid, SurgeryRecieverComponent component, ComponentStartup args)
    {
        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        EnsureComp<SurgeryRecieverBodyComponent>(body, out var surgeryBodyComp);
        surgeryBodyComp.Limbs.Add(uid);
    }
}
