using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    private void InitializeTurret()
    {
        SubscribeLocalEvent<StationAiTurretComponent, ComponentRemove>(OnRemoved);
        SubscribeLocalEvent<StationAiTurretComponent, StationAiTurretEvent>(OnTurret);
    }

    private void OnRemoved(EntityUid uid, StationAiTurretComponent component, ComponentRemove args)
    {
        if (component.OldFactions == null)
        {
            RemComp<NpcFactionMemberComponent>(uid);
            return;
        }

        if (!TryComp<NpcFactionMemberComponent>(uid, out var npcFaction))
            return;

        _npcFaction.ClearFactions((uid, npcFaction));
        _npcFaction.AddFactions((uid, npcFaction), component.OldFactions);
    }

    private void OnTurret(EntityUid uid, StationAiTurretComponent component, StationAiTurretEvent args)
    {
        if (component.Modes.Count < 2)
            return;

        var newMode = component.Modes[args.Mode];

        if (component.OldFactions == null && TryComp<NpcFactionMemberComponent>(uid, out var npcFaction))
            component.OldFactions = npcFaction.Factions;

        EnsureComp<NpcFactionMemberComponent>(uid, out npcFaction);
        _npcFaction.ClearFactions((uid, npcFaction));

        if (newMode.Factions != null)
            _npcFaction.AddFactions((uid, npcFaction), newMode.Factions);
    }

    protected (StationAiTurret, int) GetNextMode(StationAiTurretComponent component)
    {
        var index = (component.CurrentMode + 1) % component.Modes.Count;
        return (component.Modes[index], index);
    }
}

[Serializable, NetSerializable]
public sealed class StationAiTurretEvent : BaseStationAiAction
{
    public int Mode;
}
