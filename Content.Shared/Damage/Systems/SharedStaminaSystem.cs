using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Crawling;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Rounding;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public abstract partial class SharedStaminaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly CrawlingSystem _crawling = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    /// <summary>
    /// How much of a buffer is there between the stun duration and when stuns can be re-applied.
    /// </summary>
    private static readonly TimeSpan StamCritBufferTime = TimeSpan.FromSeconds(3f);

    public float UniversalStaminaDamageModifier { get; private set; } = 1f;

    public override void Initialize()
    {
        base.Initialize();

        InitializeModifier();
        InitializeResistance();

        SubscribeLocalEvent<StaminaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StaminaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StaminaComponent, AfterAutoHandleStateEvent>(OnStamHandleState);
        SubscribeLocalEvent<StaminaComponent, DisarmedEvent>(OnDisarmed);
        SubscribeLocalEvent<StaminaComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<StaminaDamageOnEmbedComponent, EmbedEvent>(OnProjectileEmbed);

        SubscribeLocalEvent<StaminaDamageOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<StaminaDamageOnCollideComponent, ThrowDoHitEvent>(OnThrowHit);

        SubscribeLocalEvent<StaminaDamageOnHitComponent, MeleeHitEvent>(OnMeleeHit);

        Subs.CVar(_config, CCVars.PlaytestStaminaDamageModifier, value => UniversalStaminaDamageModifier = value, true);
    }

    private void OnStamHandleState(EntityUid uid, StaminaComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.State == StunnedState.Critical
            && component.StaminaDamage > component.CritThreshold)
        {
            EnterStamCrit(uid, component, false);
        }
        else if (component.State == StunnedState.Crawling
            && component.StaminaDamage > component.CritThreshold)
        {
            EnterStamCrit(uid, component, true);
        }

        if (component.StaminaDamage > 0f)
            EnsureComp<ActiveStaminaComponent>(uid);
    }

    private void OnShutdown(EntityUid uid, StaminaComponent component, ComponentShutdown args)
    {
        if (MetaData(uid).EntityLifeStage < EntityLifeStage.Terminating)
            RemCompDeferred<ActiveStaminaComponent>(uid);

        _alerts.ClearAlert(uid, component.StaminaAlert);
    }

    private void OnStartup(EntityUid uid, StaminaComponent component, ComponentStartup args)
    {
        SetStaminaAlert(uid, component);
    }

    [PublicAPI]
    public float GetStaminaDamage(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0f;

        var curTime = _timing.CurTime;
        var pauseTime = _metadata.GetPauseTime(uid);
        return MathF.Max(0f, component.StaminaDamage - MathF.Max(0f, (float)(curTime - (component.NextUpdate + pauseTime)).TotalSeconds * component.Decay));
    }

    private void OnRejuvenate(EntityUid uid, StaminaComponent component, RejuvenateEvent args)
    {
        if (component.StaminaDamage >= component.CritThreshold)
            ExitStamCrit(uid, component);

        component.State = StunnedState.None;
        component.StaminaDamage = 0;
        AdjustSlowdown(uid);
        RemComp<ActiveStaminaComponent>(uid);
        SetStaminaAlert(uid, component);
        Dirty(uid, component);
    }

    private void OnDisarmed(EntityUid uid, StaminaComponent component, ref DisarmedEvent args)
    {
        if (args.Handled)
            return;

        if (component.State != StunnedState.None)
        {
            args.Handled = true;
            return;
        }

        var damage = args.PushProbability * component.CritThreshold;
        TakeStaminaDamage(uid, damage, component, source: args.Source);

        args.PopupPrefix = "disarm-action-shove-";
        args.IsStunned = component.State == StunnedState.Crawling;

        args.Handled = true;
    }

    private void OnMeleeHit(EntityUid uid, StaminaDamageOnHitComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            component.Damage <= 0f)
        {
            return;
        }

        var ev = new StaminaDamageOnHitAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        var stamQuery = GetEntityQuery<StaminaComponent>();
        var toHit = new List<(EntityUid Entity, StaminaComponent Component)>();

        // Split stamina damage between all eligible targets.
        foreach (var ent in args.HitEntities)
        {
            if (!stamQuery.TryGetComponent(ent, out var stam))
                continue;

            toHit.Add((ent, stam));
        }

        var hitEvent = new StaminaMeleeHitEvent(toHit);
        RaiseLocalEvent(uid, hitEvent);

        if (hitEvent.Handled)
            return;

        var damage = component.Damage;

        damage *= hitEvent.Multiplier;

        damage += hitEvent.FlatModifier;

        foreach (var (ent, comp) in toHit)
            TakeStaminaDamage(ent, damage / toHit.Count, comp, args.User, args.Weapon, component.Soft, sound: component.Sound);
    }

    private void OnProjectileHit(EntityUid uid, StaminaDamageOnCollideComponent component, ref ProjectileHitEvent args)
    {
        OnCollide(uid, component, args.Target);
    }

    private void OnProjectileEmbed(EntityUid uid, StaminaDamageOnEmbedComponent component, ref EmbedEvent args)
    {
        if (!TryComp<StaminaComponent>(args.Embedded, out var stamina))
            return;

        TakeStaminaDamage(args.Embedded, component.Damage, stamina, uid, soft: component.Soft);
    }

    private void OnThrowHit(EntityUid uid, StaminaDamageOnCollideComponent component, ThrowDoHitEvent args)
    {
        OnCollide(uid, component, args.Target);
    }

    private void OnCollide(EntityUid uid, StaminaDamageOnCollideComponent component, EntityUid target)
    {
        // you can't inflict stamina damage on things with no stamina component
        // this prevents stun batons from using up charges when throwing it at lockers or lights
        if (!HasComp<StaminaComponent>(target))
            return;

        var ev = new StaminaDamageOnHitAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        TakeStaminaDamage(target, component.Damage, source: uid, soft: component.Soft, sound: component.Sound);
    }

    private void SetStaminaAlert(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.CritThreshold - component.StaminaDamage), component.CritThreshold, 7);
        _alerts.ShowAlert(uid, component.StaminaAlert, (short) severity);
    }

    /// <summary>
    /// Tries to take stamina damage without raising the entity over the crit threshold.
    /// </summary>
    public bool TryTakeStamina(EntityUid uid, float value, StaminaComponent? component = null, EntityUid? source = null, EntityUid? with = null, bool soft = true)
    {
        // Something that has no Stamina component automatically passes stamina checks
        if (!Resolve(uid, ref component, false))
            return true;

        if (component.StaminaDamage + value > component.CritThreshold &&
            component.State == StunnedState.Critical)
            return false;

        // start dealing hard stam now
        if (component.StaminaDamage + value > component.CritThreshold &&
            component.State == StunnedState.Crawling)
            soft = false;

        TakeStaminaDamage(uid, value, component, source, with, visual: false, soft);
        return true;
    }

    public void TakeStaminaDamage(EntityUid uid, float value, StaminaComponent? component = null,
        EntityUid? source = null, EntityUid? with = null, bool visual = true, bool soft = true, SoundSpecifier? sound = null, bool ignoreResist = false)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var ev = new BeforeStaminaDamageEvent(value);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        // Allow stamina resistance to be applied.
        if (!ignoreResist)
        {
            value = ev.Value;
        }

        value = UniversalStaminaDamageModifier * value;
        var isPositive = value > 0;

        // we want to still allow subtracting stamina so they recover
        if (component.State == StunnedState.Critical && isPositive)
            return;

        var softModified = false;

        // if softstam is reached deal hard stun instead
        // we still want to deal hard stun even when they are crit
        if (soft && component.State != StunnedState.None && isPositive)
        {
            soft = false;
            softModified = true;
        }

        if (isPositive && TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState != MobState.Alive)
        {
            soft = false;
            softModified = true;
        }

        var oldDamage = component.StaminaDamage;
        component.StaminaDamage = MathF.Max(0f, component.StaminaDamage + value);

        // Reset the decay cooldown upon taking damage.
        if (isPositive)
        {
            var nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(component.Cooldown);

            if (component.NextUpdate < nextUpdate)
                component.NextUpdate = nextUpdate;
        }

        AdjustSlowdown(uid);

        SetStaminaAlert(uid, component);

        // Checking if the stamina damage has decreased to zero after exiting the stamcrit
        if (component.AfterCritical && oldDamage > component.StaminaDamage && component.StaminaDamage <= 0f)
            component.AfterCritical = false; // Since the recovery from the crit has been completed, we are no longer 'after crit'

        if (component.StaminaDamage > component.CritThreshold && component.State != StunnedState.Critical)
        {
            EnterStamCrit(uid, component, soft);
        }
        else if (component.StaminaDamage < component.CritThreshold && component.State != StunnedState.None
        && !softModified) //wont end up putting user into crit as first hit after crawling will always pass ending immedietly
        {
            ExitStamCrit(uid, component);
        }

        EnsureComp<ActiveStaminaComponent>(uid);
        Dirty(uid, component);

        if (value <= 0)
            return;

        if (source != null)
        {
            _adminLogger.Add(LogType.Stamina, $"{ToPrettyString(source.Value):user} caused {value} stamina damage to {ToPrettyString(uid):target}{(with != null ? $" using {ToPrettyString(with.Value):using}" : "")}");
        }
        else
        {
            _adminLogger.Add(LogType.Stamina, $"{ToPrettyString(uid):target} took {value} stamina damage");
        }

        if (visual)
            _color.RaiseEffect(Color.Aqua, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));

        if (_net.IsServer)
            _audio.PlayPvs(sound, uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var stamQuery = GetEntityQuery<StaminaComponent>();
        var query = EntityQueryEnumerator<ActiveStaminaComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out _))
        {
            // Just in case we have active but not stamina we'll check and account for it.
            if (!stamQuery.TryGetComponent(uid, out var comp) ||
                comp.State == StunnedState.None && comp.StaminaDamage <= 0)
            {
                RemComp<ActiveStaminaComponent>(uid);
                continue;
            }

            // Shouldn't need to consider paused time as we're only iterating non-paused stamina components.
            var nextUpdate = comp.NextUpdate;

            if (nextUpdate > curTime)
                continue;

            var decay = comp.AfterCritical ? -comp.Decay * comp.AfterCritDecayMultiplier : -comp.Decay; // Recover faster after crit

            var soft = comp.State != StunnedState.None && (comp.StaminaDamage + decay) < comp.CritThreshold;

            // Handle exiting critical condition and restoring stamina damage
            if (soft)
            {
                switch (comp.State)
                {
                    case StunnedState.Critical:
                        ExitStamCrit(uid, comp);
                        break;

                    case StunnedState.Crawling:
                        TakeStaminaDamage(uid, decay, comp, soft: true);
                        break;
                }
            }

            comp.NextUpdate += TimeSpan.FromSeconds(1f);

            TakeStaminaDamage(uid, decay, comp, soft: soft);
            Dirty(uid, comp);
        }
    }

    public void EnterStamCrit(EntityUid uid, StaminaComponent? component = null, bool soft = true)
    {
        if (!Resolve(uid, ref component) || component.State == StunnedState.Critical)
            return;

        // To make the difference between a stun and a stamcrit clear
        // TODO: Mask?

        if (soft && TryComp<CrawlerComponent>(uid, out var crawlerComp))
        {
            if (!HasComp<CrawlingComponent>(uid))
                _crawling.SetCrawling(uid, crawlerComp, true, force: true);

            component.State = StunnedState.Crawling;
            component.StaminaDamage = 0f;

            _stunSystem.TryKnockdown(uid, component.StunTime * 2, true);
        }
        else
        {
            component.State = StunnedState.Critical;
            _stunSystem.TryParalyze(uid, component.StunTime, true);
        }

        // Give them buffer before being able to be re-stunned
        component.NextUpdate = _timing.CurTime + component.StunTime + StamCritBufferTime;
        EnsureComp<ActiveStaminaComponent>(uid);
        Dirty(uid, component);
        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} entered stamina crit");
    }

    public void ExitStamCrit(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.State == StunnedState.None)
            return;

        component.State = StunnedState.None;
        component.StaminaDamage = 0f;
        component.AfterCritical = true;  // Set to true to indicate that stamina will be restored after exiting stamcrit
        component.NextUpdate = _timing.CurTime;

        if (TryComp<CrawlerComponent>(uid, out var crawlerComp) && HasComp<CrawlingComponent>(uid))
            _crawling.SetCrawling(uid, crawlerComp, false, force: true);

        SetStaminaAlert(uid, component);
        Dirty(uid, component);
        _adminLogger.Add(LogType.Stamina, LogImpact.Low, $"{ToPrettyString(uid):user} recovered from stamina crit");
    }

    /// <summary>
    /// Adjusts the movement speed of an entity based on its current <see cref="StaminaComponent.StaminaDamage"/> value.
    /// If the entity has a <see cref="SlowOnDamageComponent"/>, its custom damage-to-speed thresholds are used,
    /// otherwise, a default set of thresholds is applied.
    /// The method determines the closest applicable damage threshold below the crit limit and applies the corresponding
    /// speed modifier using the stun system. If no threshold is met then the entity's speed is restored to normal.
    /// </summary>
    /// <param name="ent">Entity to update</param>
    private void AdjustSlowdown(Entity<StaminaComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var closest = FixedPoint2.Zero;

        // Iterate through the dictionary in the similar way as in Damage.SlowOnDamageSystem.OnRefreshMovespeed
        foreach (var thres in ent.Comp.StunModifierThresholds)
        {
            var key = thres.Key.Float();

            if (ent.Comp.StaminaDamage >= key && key > closest && closest < ent.Comp.CritThreshold)
                closest = thres.Key;
        }

        _stunSystem.UpdateStunModifiers(ent, ent.Comp.StunModifierThresholds[closest]);
    }
}
