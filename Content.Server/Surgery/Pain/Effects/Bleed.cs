using Content.Server.Body.Systems;
using Content.Shared.Surgery.Pain.Effects;

namespace Content.Server.Surgery.Pain.Effects;

public sealed partial class Bleed : SharedBleed
{
    public override void DoEffect(IEntityManager entity, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        if (body == null)
            return;

        var bloodstream = entity.System<BloodstreamSystem>();
        bloodstream.TryModifyBloodLevel(body.Value, -Amount);
        bloodstream.TryModifyBleedAmount(body.Value, Amount);
    }
}
