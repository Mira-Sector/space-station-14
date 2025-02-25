using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Standing;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializeRelays()
    {
        SubscribeLocalEvent<BodyComponent, StandAttemptEvent>(RelayToLimbs);
        SubscribeLocalEvent<BodyComponent, StoodEvent>(RelayToLimbs);
        SubscribeLocalEvent<BodyComponent, DownAttemptEvent>(RelayToLimbs);
        SubscribeLocalEvent<BodyComponent, DownedEvent>(RelayToLimbs);

        SubscribeLocalEvent<BodyPartComponent, DamageModifyEvent>(RelayToBody);
        SubscribeLocalEvent<BodyPartComponent, DamageChangedEvent>(RelayToBody);
    }

    private void RelayToBody<T>(EntityUid uid, BodyPartComponent component, T args) where T : class
    {
        if (component.Body == null)
            return;

        var ev = new LimbBodyRelayedEvent<T>(args, uid);
        RaiseLocalEvent(component.Body.Value, ref ev);
    }

    private void RelayRefToBody<T>(EntityUid uid, BodyPartComponent component, ref T args) where T : struct
    {
        if (component.Body == null)
            return;

        var ev = new LimbBodyRelayedEvent<T>(args, uid);
        RaiseLocalEvent(component.Body.Value, ref ev);
    }

    private void RelayToLimbs<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        var ev = new BodyLimbRelayedEvent<T>(args, uid);

        foreach (var (limb, _) in GetBodyChildren(uid, component))
        {
            RaiseLocalEvent(limb, ref ev);
        }
    }

    private void RelayRefToLimbs<T>(EntityUid uid, BodyComponent component, ref T args) where T : struct
    {
        var ev = new BodyLimbRelayedEvent<T>(args, uid);

        foreach (var (limb, _) in GetBodyChildren(uid, component))
        {
            RaiseLocalEvent(limb, ref ev);
        }
    }
}
