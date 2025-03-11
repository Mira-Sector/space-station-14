using Content.Client.Items;
using Content.Client.Atmos.UI;
using Content.Shared.Atmos.Piping.Layerable;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class PipeLayerableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<PipeLayerableComponent>(entity => new PipeLayerableControl(entity.Comp));
    }
}
