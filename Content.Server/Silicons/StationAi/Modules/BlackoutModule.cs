using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.StationAi.Modules;
using Robust.Shared.Random;
using Content.Shared.Charges.Components;

namespace Content.Server.Silicons.StationAi.Modules;

public sealed class BlackoutModuleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCanHackComponent, StationAiBlackoutEvent>(OnAction);
    }

    private void OnAction(EntityUid uid, StationAiCanHackComponent component, StationAiBlackoutEvent args)
    {
        if (args.Handled)
            return;

        var aiXForm = Transform(uid);
        var query = EntityQueryEnumerator<ApcComponent>();

        while (query.MoveNext(out var apcUid, out var apcComp))
        {
            var apcXForm = Transform(apcUid);

            if (aiXForm.MapUid != apcXForm.MapUid)
                continue;

            if (!apcComp.MainBreakerEnabled)
                continue;

            args.Handled = true;

            // done per apc so no lag
            if (!_random.Prob(args.Chance))
                continue;

            if (apcComp.Net is not ApcNet apcNet)
                continue;

            foreach (var light in apcNet.AllReceivers)
            {
                if (!TryComp<PoweredLightComponent>(light.Owner, out var lightComp))
                    continue;

                if (!lightComp.On)
                    continue;

                _poweredLight.TryDestroyBulb(light.Owner, lightComp);
            }
        }

        if (!args.Handled)
            return;

        if (TryComp<LimitedChargesComponent>(args.Action.Owner, out var charges) && charges.LastCharges > 0)
            return;

        EntityManager.DeleteEntity(args.Action);
    }
}
