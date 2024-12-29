using Content.Server.Nuke;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.StationAi.Modules;

namespace Content.Server.Silicons.StationAi.Modules;

public sealed class NukeModuleSystem : EntitySystem
{
    [Dependency] private readonly NukeSystem _nuke = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCanHackComponent, StationAiNukeEvent>(OnAction);
    }

    private void OnAction(EntityUid uid, StationAiCanHackComponent component, StationAiNukeEvent args)
    {
        if (args.Handled)
            return;

        var aiXForm = Transform(uid);
        var query = EntityQueryEnumerator<NukeComponent>();

        while (query.MoveNext(out var nukeUid, out var nukeComp))
        {
            var nukeXForm = Transform(nukeUid);

            if (aiXForm.MapUid != nukeXForm.MapUid)
                continue;

            _nuke.ArmBomb(nukeUid, nukeComp);
            args.Handled = true;
        }

        // dont let them waste their 130 power
        if (!args.Handled)
            return;

        if (args.Action.Comp.Charges > 0)
            return;

        EntityManager.DeleteEntity(args.Action);
    }
}

