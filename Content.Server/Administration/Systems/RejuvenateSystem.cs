using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Rejuvenate;

namespace Content.Server.Administration.Systems;

public sealed class RejuvenateSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    public void PerformRejuvenate(EntityUid target)
    {
        if (TryComp<BodyComponent>(target, out var bodyComp))
        {
            var parts = _body.GetBodyChildren(target, bodyComp);

            foreach ((var currentPart, var _) in parts)
            {
                RaiseLocalEvent(currentPart, new RejuvenateEvent());
            }
        }
        else
        {
            RaiseLocalEvent(target, new RejuvenateEvent());
        }
    }
}
