using Content.Shared.Alert;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Explosion;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Crawling;

public sealed partial class CrawlingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlerComponent, CrawlStandupDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CrawlerComponent, StandAttemptEvent>(OnStandUp);
        SubscribeLocalEvent<CrawlerComponent, DownAttemptEvent>(OnFall);
        SubscribeLocalEvent<CrawlerComponent, BuckledEvent>(OnBuckled);
        SubscribeLocalEvent<CrawlerComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
        SubscribeLocalEvent<CrawlerComponent, CrawlingAlertEvent>(OnCrawlingAlertEvent);
        SubscribeLocalEvent<CrawlerComponent, CrawlingKeybindEvent>(ToggleCrawling);

        SubscribeLocalEvent<CrawlingComponent, ComponentInit>(OnCrawlSlowdownInit);
        SubscribeLocalEvent<CrawlingComponent, ComponentShutdown>(OnCrawlSlowRemove);
        SubscribeLocalEvent<CrawlingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<CrawlingComponent, InteractHandEvent>(OnCrawlInteract);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleCrawling, InputCmdHandler.FromDelegate(ToggleCrawlingKeybind, handle: false))
            .Register<CrawlingSystem>();
    }

    private bool IsSoftStunned(EntityUid uid)
    {
        if (!TryComp<StaminaComponent>(uid, out var stamComp))
            return false;

        if (stamComp.State != StunnedState.None)
            return true;

        return false;
    }

    private void ToggleCrawlingKeybind(ICommonSession? session)
    {
        if (session?.AttachedEntity == null)
            return;

        var ev = new CrawlingKeybindEvent();
        RaiseLocalEvent(session.AttachedEntity.Value, ev);
    }

    private void ToggleCrawling(EntityUid uid, CrawlerComponent component, CrawlingKeybindEvent args)
    {
        if (args.Cancelled)
            return;

        if (IsSoftStunned(uid) && _standing.IsDown(uid))
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<BuckleComponent>(uid, out var buckleComp) && buckleComp.Buckled)
        {
            args.Cancelled = true;
            return;
        }

        SetCrawling(uid, component, !_standing.IsDown(uid));
    }

    public bool SetCrawling(EntityUid uid, CrawlerComponent component, bool state, EntityUid? user = null, bool force = false)
    {
        var buckled = TryComp<BuckleComponent>(uid, out var buckleComp) && buckleComp.Buckled;

        if (state)
        {
            // prevent others shoving them into crit
            // force crawling is handled with disarm intent
            if (user != null)
                return false;

            if (buckled)
            {
                if (force)
                    _buckle.Unbuckle((uid, buckleComp), uid);
                else
                    return false;
            }

            _standing.Down(uid, dropHeldItems: false);
            return true;
        }

        if (TryComp<MobStateComponent>(uid, out var stateComp) &&
            stateComp.CurrentState != Mobs.MobState.Alive)
        {
            return false;
        }

        bool userIsNull = user == null;

        if (user == null)
        {
            user = uid;
        }

        if (buckled)
        {
            if (force)
                _buckle.Unbuckle((uid, buckleComp), uid);
            else
                return false;
        }

        if (HasComp<ActiveStaminaComponent>(uid) &&
            TryComp<StaminaComponent>(uid, out var staminaComponent))
        {
            if (userIsNull)
            {
                if (staminaComponent.State != StunnedState.None && // prevent getting up when full stunned not just crawling
                    staminaComponent.StaminaDamage > staminaComponent.CritThreshold)
                {
                    return false;
                }
            }
            else
            {
                if (staminaComponent.State != StunnedState.Crawling)
                    return false;

                RemComp<KnockedDownComponent>(uid);
                _stamina.ExitStamCrit(uid, staminaComponent, user, true);
                return true;
            }
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user.Value, component.StandUpTime, new CrawlStandupDoAfterEvent(),
        uid, used: uid)
        {
            BreakOnMove = !userIsNull,
            NeedHand = !userIsNull,
            BreakOnDamage = true
        });

        return true;
    }

    private void OnCrawlingAlertEvent(EntityUid uid, CrawlerComponent component, CrawlingAlertEvent args)
    {
        var ev = new CrawlingKeybindEvent();
        RaiseLocalEvent(args.User, ev);
    }

    private void OnDoAfter(EntityUid uid, CrawlerComponent component, CrawlStandupDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _standing.Stand(uid);
    }

    private void OnStandUp(EntityUid uid, CrawlerComponent component, StandAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp<MobStateComponent>(uid, out var mobStateComp) && mobStateComp.CurrentState != Mobs.MobState.Alive)
            args.Cancel();

        RemCompDeferred<CrawlingComponent>(uid);
        _alerts.ClearAlert(uid, component.CtawlingAlert);
    }

    private void OnFall(EntityUid uid, CrawlerComponent component, DownAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        _alerts.ShowAlert(uid, component.CtawlingAlert);

        if (!HasComp<CrawlingComponent>(uid))
            AddComp<CrawlingComponent>(uid);
        //TODO: add hiding under table
    }

    private void OnBuckled(EntityUid uid, CrawlerComponent component, ref BuckledEvent args)
    {
        RemCompDeferred<CrawlingComponent>(uid);
        _alerts.ClearAlert(uid, component.CtawlingAlert);
    }

    private void OnGetExplosionResistance(EntityUid uid, CrawlerComponent component, ref GetExplosionResistanceEvent args)
    {
        // fall on explosion damage and lower explosion damage of crawling
        if (_standing.IsDown(uid))
            args.DamageCoefficient *= component.DownedDamageCoefficient;
        else
            _standing.Down(uid, dropHeldItems: false);
    }

    private void OnCrawlSlowdownInit(EntityUid uid, CrawlingComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnCrawlSlowRemove(EntityUid uid, CrawlingComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMovespeed(EntityUid uid, CrawlingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnCrawlInteract(EntityUid uid, CrawlingComponent component, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<CanRemoveCrawlingComponent>(args.User))
            return;

        if (!TryComp<CrawlerComponent>(uid, out var crawlerComp))
            return;

        args.Handled = SetCrawling(uid, crawlerComp, false, args.User);
    }
}

[Serializable, NetSerializable]
public sealed partial class CrawlStandupDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class CrawlingAlertEvent : BaseAlertEvent;

[Serializable, NetSerializable]
public sealed partial class CrawlingKeybindEvent
{
    public bool Cancelled;
}
