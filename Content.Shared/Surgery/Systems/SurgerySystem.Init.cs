using Content.Shared.Surgery.Components;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem
{
    private void GraphInit()
    {
        SubscribeLocalEvent<SurgeryReceiverComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SurgeryReceiverComponent component, ComponentInit args)
    {
        // been overwritten
        if (component.Graph != null)
            return;

        UpdateGraph(uid, component);
    }

    public void UpdateGraph(EntityUid uid, SurgeryReceiverComponent component)
    {
    }
}
