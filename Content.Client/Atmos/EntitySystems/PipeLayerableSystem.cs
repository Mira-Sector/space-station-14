using Content.Client.Atmos.UI;
using Content.Client.Items;
using Content.Client.SubFloor;
using Content.Shared.Atmos.Piping.Layerable;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class PipeLayerableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<PipeLayerableComponent>(entity => new PipeLayerableControl(entity.Comp));

        SubscribeLocalEvent<PipeLayerableComponent, TrayCanRevealEvent>(OnCanReveal);
    }

    private void OnCanReveal(Entity<PipeLayerableComponent> ent, ref TrayCanRevealEvent args)
    {
        if (!args.Tray.Comp.RevealedLayers.Contains(ent.Comp.Layer))
            args.Cancel();
    }
}
