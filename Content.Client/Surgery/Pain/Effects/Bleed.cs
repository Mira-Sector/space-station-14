using Content.Shared.Surgery.Pain.Effects;

namespace Content.Client.Surgery.Pain.Effects;

public sealed partial class Bleed : SharedBleed
{
    public override void DoEffect(IEntityManager entity, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
    }
}
