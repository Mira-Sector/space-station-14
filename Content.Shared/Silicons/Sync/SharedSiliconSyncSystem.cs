using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.Sync.Components;
using Content.Shared.Silicons.Sync.Events;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Silicons.Sync;

public abstract partial class SharedSiliconSyncSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedSiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SiliconSyncableMasterComponent, SiliconSyncMasterSlaveAddedEvent>(OnSlaveAdded);
        SubscribeLocalEvent<SiliconSyncableMasterComponent, SiliconSyncMasterSlaveLostEvent>(OnSlaveLost);

        SubscribeLocalEvent<SiliconSyncableSlaveComponent, IonStormLawsEvent>(OnSlaveIonStormed);
        SubscribeLocalEvent<SiliconSyncableSlaveComponent, SiliconEmaggedEvent>(OnSlaveEmagged);

        SubscribeLocalEvent<SiliconSyncSlaveMasterMessage>(OnRadialSlaveMaster);

        SubscribeLocalEvent<SiliconSyncableMasterLawComponent, SiliconLawsUpdatedEvent>(OnMasterLawsUpdated);
        SubscribeLocalEvent<SiliconSyncableSlaveLawComponent, SiliconSyncSlaveLawCanUpdateEvent>(OnSlaveLawsCanUpdate);

        SubscribeLocalEvent<SiliconSyncableSlaveAiRadialComponent, StationAiSyncSlaveEvent>(OnAiSlave);
    }

    private void OnSlaveAdded(Entity<SiliconSyncableMasterComponent> ent, ref SiliconSyncMasterSlaveAddedEvent args)
    {
        if (ent.Comp.Slaves.Add(args.Slave))
            Dirty(ent);
    }

    private void OnSlaveLost(Entity<SiliconSyncableMasterComponent> ent, ref SiliconSyncMasterSlaveLostEvent args)
    {
        if (ent.Comp.Slaves.Remove(args.Slave))
            Dirty(ent);
    }

    private void OnSlaveIonStormed(Entity<SiliconSyncableSlaveComponent> ent, ref IonStormLawsEvent args)
    {
        SetMaster(ent.Owner, null); // give them a bit more fun
    }

    private void OnSlaveEmagged(Entity<SiliconSyncableSlaveComponent> ent, ref SiliconEmaggedEvent args)
    {
        SetMaster(ent.Owner, null); // so we dont bulldoze our work
        ent.Comp.Enabled = false;
        Dirty(ent);
    }

    private void OnRadialSlaveMaster(SiliconSyncSlaveMasterMessage args)
    {
        SetMaster(GetEntity(args.Entity), GetEntity(args.Master));
    }

    private void OnMasterLawsUpdated(Entity<SiliconSyncableMasterLawComponent> ent, ref SiliconLawsUpdatedEvent args)
    {
        if (!TryGetSlaves(ent.Owner, out var slaves))
            return;

        foreach (var slave in slaves)
        {
            var ev = new SiliconSyncSlaveLawCanUpdateEvent(ent, args.Laws);
            RaiseLocalEvent(slave, ev);

            if (ev.Cancelled)
                continue;

            _siliconLaw.SetLaws(ev.Laws.Laws, slave);
        }
    }

    private void OnSlaveLawsCanUpdate(Entity<SiliconSyncableSlaveLawComponent> ent, ref SiliconSyncSlaveLawCanUpdateEvent args)
    {
        if (!ent.Comp.Enabled)
            args.Cancel();
    }

    private void OnAiSlave(Entity<SiliconSyncableSlaveAiRadialComponent> ent, ref StationAiSyncSlaveEvent args)
    {
        SetMaster(ent.Owner, args.User);
    }

    public void ShowAvailableMasters(Entity<SiliconSyncableSlaveComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!TryComp<ActorComponent>(user, out var actorComp))
            return;

        var masters = GetAvailableMasters((ent.Owner, ent.Comp));

        if (!masters.Any())
            return;

        if (masters.Count() == 1)
        {
            SetMaster(ent, masters.First());
            return;
        }

        var state = new SiliconSlaveRadialBoundUserInterfaceState();

        foreach (var master in masters)
        {
            var ev = new SiliconSyncMasterGetIconEvent();
            RaiseLocalEvent(master, ev);

            if (ev.Cancelled)
                continue;

            state.Masters.Add(GetNetEntity(master), ev.Icon);
        }

        _userInterface.TryToggleUi(ent.Owner, SiliconSyncUiKey.Key, actorComp.PlayerSession);
        _userInterface.SetUiState(ent.Owner, SiliconSyncUiKey.Key, state);
    }

    public IEnumerable<Entity<SiliconSyncableMasterComponent>> GetAvailableMasters(Entity<SiliconSyncableSlaveComponent> ent)
    {
        var slaveMap = Transform(ent).MapUid;

        var query = EntityQueryEnumerator<SiliconSyncableMasterComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (Transform(uid).MapUid != slaveMap)
                continue;

            yield return (uid, component);
        }
    }

    public bool TryGetMaster(Entity<SiliconSyncableSlaveComponent?> ent, [NotNullWhen(true)] out EntityUid? master)
    {
        master = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        master = ent.Comp.Master;
        return master != null;
    }

    public bool TryGetSlaves(Entity<SiliconSyncableMasterComponent?> ent, out HashSet<EntityUid> slaves)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
        {
            slaves = new();
            return false;
        }

        slaves = ent.Comp.Slaves;
        return true;
    }

    public void SetMaster(Entity<SiliconSyncableSlaveComponent?> ent, EntityUid? master)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!ent.Comp.Enabled)
            return;

        var slaveEv = new SiliconSyncSlaveMasterUpdatedEvent(master, ent.Comp.Master);
        RaiseLocalEvent(ent, slaveEv);

        if (ent.Comp.Master is {} oldMaster)
        {
            var oldMasterEv = new SiliconSyncMasterSlaveLostEvent(ent);
            RaiseLocalEvent(oldMaster, oldMasterEv);
        }

        if (master is {} newMaster)
        {
            var newMasterEv = new SiliconSyncMasterSlaveAddedEvent(ent);
            RaiseLocalEvent(newMaster, newMasterEv);
        }

        ent.Comp.Master = master;
        Dirty(ent);
    }

    public void UpdateSlaveLaws(Entity<SiliconSyncableSlaveComponent?, SiliconSyncableSlaveLawComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return;

        if (ent.Comp1.Master is not {} master)
            return;

        var masterLaws = _siliconLaw.GetLaws(master);

        var ev = new SiliconSyncSlaveLawCanUpdateEvent(master, masterLaws);
        RaiseLocalEvent(ent.Owner, ev);

        if (ev.Cancelled)
            return;

        _siliconLaw.SetLaws(ev.Laws.Laws, ent.Owner);
    }
}
