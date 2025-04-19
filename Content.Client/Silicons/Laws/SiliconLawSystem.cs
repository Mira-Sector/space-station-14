using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Client.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    public override SiliconLawset GetLaws(EntityUid uid, SiliconLawBoundComponent? component = null)
    {
        var ev = new SiliconGetLawsEvent(GetNetEntity(uid));
        RaiseNetworkEvent(ev, uid);

        return ev.Laws ?? new SiliconLawset();
    }
}
