using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Speech;
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

        SubscribeLocalEvent<BodyComponent, SexChangedEvent>(RelayToLimbsAndOrgans);
        SubscribeLocalEvent<BodyComponent, ScreamActionEvent>(RelayToLimbsAndOrgans);

        SubscribeLocalEvent<BodyPartComponent, DamageModifyEvent>(RelayToBody);
        SubscribeLocalEvent<BodyPartComponent, DamageChangedEvent>(RelayToBody);

        SubscribeLocalEvent<OrganComponent, DamageChangedEvent>(RelayToBody);
    }

    #region Single

    public void RelayToBody<T>(EntityUid uid, BodyPartComponent component, T args) where T : class
    {
        if (component.Body == null)
            return;

        var ev = new LimbBodyRelayedEvent<T>(args, uid);
        RaiseLocalEvent(component.Body.Value, ref ev);
    }

    public void RelayToBody<T>(EntityUid uid, BodyPartComponent component, ref T args) where T : struct
    {
        if (component.Body == null)
            return;

        var ev = new LimbBodyRelayedEvent<T>(args, uid);
        RaiseLocalEvent(component.Body.Value, ref ev);
    }

    public void RelayToLimbs<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        var ev = new BodyLimbRelayedEvent<T>(args, uid);

        foreach (var (limb, _) in GetBodyChildren(uid, component))
        {
            RaiseLocalEvent(limb, ref ev);
        }
    }

    public void RelayToLimbs<T>(EntityUid uid, BodyComponent component, ref T args) where T : struct
    {
        var ev = new BodyLimbRelayedEvent<T>(args, uid);

        foreach (var (limb, _) in GetBodyChildren(uid, component))
        {
            RaiseLocalEvent(limb, ref ev);
        }
    }

    public void RelayToOrgans<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        var ev = new BodyOrganRelayedEvent<T>(args, uid);

        foreach (var (organ, _) in GetBodyOrgans(uid, component))
        {
            RaiseLocalEvent(organ, ref ev);
        }
    }

    public void RelayToOrgans<T>(EntityUid uid, BodyComponent component, ref T args) where T : struct
    {
        var ev = new BodyOrganRelayedEvent<T>(args, uid);

        foreach (var (organ, _) in GetBodyOrgans(uid, component))
        {
            RaiseLocalEvent(organ, ref ev);
        }
    }

    public void RelayToOrgans<T>(EntityUid uid, BodyPartComponent component, T args) where T : class
    {
        var ev = new LimbOrganRelayedEvent<T>(args, uid);

        foreach (var (organ, _) in GetPartOrgans(uid, component))
        {
            RaiseLocalEvent(organ, ref ev);
        }
    }

    public void RelayToOrgans<T>(EntityUid uid, BodyPartComponent component, ref T args) where T : struct
    {
        var ev = new LimbOrganRelayedEvent<T>(args, uid);

        foreach (var (organ, _) in GetPartOrgans(uid, component))
        {
            RaiseLocalEvent(organ, ref ev);
        }
    }

    public void RelayToLimb<T>(EntityUid uid, OrganComponent component, T args) where T : class
    {
        if (component.BodyPart is not {} bodyPart)
            return;

        var ev = new OrganLimbRelayedEvent<T>(args, uid);

        RaiseLocalEvent(bodyPart, ref ev);
    }

    public void RelayToLimb<T>(EntityUid uid, OrganComponent component, ref T args) where T : struct
    {
        if (component.BodyPart is not {} bodyPart)
            return;

        var ev = new OrganLimbRelayedEvent<T>(args, uid);

        RaiseLocalEvent(bodyPart, ref ev);
    }

    public void RelayToBody<T>(EntityUid uid, OrganComponent component, T args) where T : class
    {
        if (component.BodyPart is not {} bodyPart)
            return;

        var ev = new BodyOrganRelayedEvent<T>(args, uid);

        RaiseLocalEvent(bodyPart, ref ev);
    }

    public void RelayToBody<T>(EntityUid uid, OrganComponent component, ref T args) where T : struct
    {
        if (component.BodyPart is not {} bodyPart)
            return;

        var ev = new BodyOrganRelayedEvent<T>(args, uid);

        RaiseLocalEvent(bodyPart, ref ev);
    }

    #endregion

    #region Multiple

    public void RelayToLimbsAndOrgans<T>(EntityUid uid, BodyComponent component, T args) where T : class
    {
        RelayToLimbs(uid, component, args);
        RelayToOrgans(uid, component, args);
    }

    public void RelayToLimbsAndOrgans<T>(EntityUid uid, BodyComponent component, ref T args) where T : struct
    {
        RelayToLimbs(uid, component, ref args);
        RelayToOrgans(uid, component, ref args);
    }

    #endregion
}
