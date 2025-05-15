using Content.Server.Silicons.Sync.Components;
using Content.Server.Silicons.Sync.Events;
using Content.Server.Station.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Prototypes;
using Content.Shared.Silicons.Sync;
using Content.Shared.Silicons.Sync.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Silicons.Sync;

public partial class SiliconSyncSystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    internal Dictionary<EntityUid, Dictionary<NetEntity, HashSet<NetEntity>>> ConsoleSlaves = [];
    internal Dictionary<NetEntity, ProtoId<NavMapBlipPrototype>> SlaveBlips = [];

    internal static readonly ProtoId<NavMapBlipPrototype> DefaultBlip = "SyncDefault";

    private void InitializeMonitor()
    {
        SubscribeLocalEvent<SiliconSyncableMonitoringConsoleComponent, BoundUIOpenedEvent>(OnConsoleOpened);
        SubscribeLocalEvent<SiliconSyncableMonitoringConsoleComponent, BoundUIClosedEvent>(OnConsoleClosed);

        SubscribeLocalEvent<SiliconSyncableMonitorBlipComponent, SiliconSyncGetNavBlipEvent>(OnBlipGetBlip);
    }

    private void UpdateMonitor(float frameTime)
    {
        var query = EntityQueryEnumerator<SiliconSyncableMonitoringConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (console.NextUpdate > Timing.CurTime)
                continue;

            console.NextUpdate += console.UpdateInterval;

            if (!console.Users.Any())
                continue;

            var slaves = GetSlaves(uid);

            if (!ConsoleSlaves.TryAdd(uid, slaves))
                ConsoleSlaves[uid] = slaves;

            UpdateUserInterface((uid, console));
        }
    }

    private void OnConsoleOpened(Entity<SiliconSyncableMonitoringConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        ent.Comp.Users.Add(args.Actor);
        UpdateUserInterface(ent);
    }

    private void OnConsoleClosed(Entity<SiliconSyncableMonitoringConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Users.Remove(args.Actor);
    }

    private void OnBlipGetBlip(Entity<SiliconSyncableMonitorBlipComponent> ent, ref SiliconSyncGetNavBlipEvent args)
    {
        args.Blip = ent.Comp.Blip;
    }

    internal Dictionary<NetEntity, HashSet<NetEntity>> GetSlaves(EntityUid console)
    {
        Dictionary<NetEntity, HashSet<NetEntity>> masterSlaves = [];

        // so the ai only shows its own slaves
        // no spying on other ais
        if (TryGetSlaves(console, out var slaves))
        {
            HashSet<NetEntity> netSlaves = [];
            foreach (var slave in slaves)
            {
                var netSlave = GetNetEntity(slave);
                netSlaves.Add(netSlave);

                SlaveBlips[netSlave] = GetNavMapBlip(slave);
            }

            masterSlaves.Add(GetNetEntity(console), netSlaves);
            return masterSlaves;
        }

        if ((_station.GetOwningStation(console) ?? Transform(console).GridUid) is not { } station)
            return masterSlaves;

        var query = EntityQueryEnumerator<SiliconSyncableMasterComponent>();
        while (query.MoveNext(out var masterUid, out var masterComp))
        {
            if (_station.GetOwningStation(masterUid) != station)
                continue;

            if (!TryGetSlaves((masterUid, masterComp), out slaves))
                continue;

            HashSet<NetEntity> netSlaves = [];
            foreach (var slave in slaves)
            {
                var netSlave = GetNetEntity(slave);
                netSlaves.Add(netSlave);

                SlaveBlips[netSlave] = GetNavMapBlip(slave);
            }

            masterSlaves.Add(GetNetEntity(masterUid), netSlaves);
        }

        return masterSlaves;
    }

    internal ProtoId<NavMapBlipPrototype> GetNavMapBlip(EntityUid slave)
    {
        var ev = new SiliconSyncGetNavBlipEvent();
        RaiseLocalEvent(slave);

        return ev.Blip ?? DefaultBlip;
    }

    internal void UpdateUserInterface(Entity<SiliconSyncableMonitoringConsoleComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, SiliconSyncMonitoringUiKey.Key))
            return;

        if (!ConsoleSlaves.TryGetValue(ent.Owner, out var slaves))
            return;

        var xform = Transform(ent.Owner);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        _ui.SetUiState(ent.Owner, SiliconSyncMonitoringUiKey.Key, new SiliconSyncMonitoringState(slaves, SlaveBlips));
    }
}
