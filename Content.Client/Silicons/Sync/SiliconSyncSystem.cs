using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.Sync;
using Content.Shared.Silicons.Sync.Components;
using Content.Shared.Silicons.Sync.Events;

namespace Content.Client.Silicons.Sync;

public sealed partial class SiliconSyncSystem : SharedSiliconSyncSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconSyncableSlaveAiRadialComponent, GetStationAiRadialEvent>(OnGetRadial);
    }

    private void OnGetRadial(Entity<SiliconSyncableSlaveAiRadialComponent> ent, ref GetStationAiRadialEvent args)
    {
        if (!TryComp<SiliconSyncableSlaveComponent>(ent, out var slaveComp) || !slaveComp.Enabled || slaveComp.Master != null)
            return;

        var radial = new StationAiRadial()
        {
            Sprite = ent.Comp.Icon,
            Tooltip = Loc.GetString(ent.Comp.Tooltip),
            Event = new StationAiSyncSlaveEvent()
        };

        args.Actions.Add(radial);
    }
}
