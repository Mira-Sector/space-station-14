using Content.Shared.Bed.Sleep;
using Content.Shared.Crawling;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Interaction;

public sealed class InteractionPopupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractionPopupComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<InteractionPopupComponent, ActivateInWorldEvent>(OnActivateInWorld);

        SubscribeLocalEvent<AlternativeInteractionPopupComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeActivate);
    }

    private void OnActivateInWorld(EntityUid uid, InteractionPopupComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (!component.OnActivate)
            return;

        SharedInteract(uid, args.Target, args.User, false, args);
    }

    private void OnAlternativeActivate(EntityUid uid, AlternativeInteractionPopupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString(component.VerbTitle),
            Act = () =>
            {
                SharedInteract(uid, args.Target, args.User, true);
            }
        };
        args.Verbs.Add(verb);
    }

    private void OnInteractHand(EntityUid uid, InteractionPopupComponent component, InteractHandEvent args)
    {
        SharedInteract(uid, args.Target, args.User, false, args);
    }

    private void SharedInteract(
        EntityUid uid,
        EntityUid target,
        EntityUid user,
        bool isAlternative,
        HandledEntityEventArgs? args = null)

    {
        if (user == target)
            return;

        if (args != null && args.Handled)
            return;

        //Handling does nothing and this thing annoyingly plays way too often.
        // HUH? What does this comment even mean?

        if (HasComp<SleepingComponent>(uid))
            return;

        if (HasComp<CrawlingComponent>(uid) && HasComp<CanRemoveCrawlingComponent>(user))
            return;

        if (TryComp<MobStateComponent>(uid, out var state)
            && !_mobStateSystem.IsAlive(uid, state))
        {
            return;
        }

        TimeSpan interactDelay;
        string? interactSuccessString;
        string? interactFailureString;
        SoundSpecifier? interactSuccessSound;
        SoundSpecifier? interactFailureSound;
        EntProtoId? interactSuccessSpawn;
        EntProtoId? interactFailureSpawn;
        float successChance;
        string? messagePerceivedByTarget;
        string? messagePerceivedByOthers;
        bool soundPerceivedByOthers;
        TimeSpan lastInteractTime;

        if (isAlternative)
        {
            if (!TryComp<AlternativeInteractionPopupComponent>(uid, out var altinteractComp))
                return;

            interactDelay = altinteractComp.InteractDelay;
            interactSuccessString = altinteractComp.InteractSuccessString;
            interactFailureString = altinteractComp.InteractFailureString;
            interactSuccessSound = altinteractComp.InteractSuccessSound;
            interactFailureSound = altinteractComp.InteractFailureSound;
            interactSuccessSpawn = altinteractComp.InteractSuccessSpawn;
            interactFailureSpawn = altinteractComp.InteractFailureSpawn;
            successChance = altinteractComp.SuccessChance;
            messagePerceivedByTarget = altinteractComp.MessagePerceivedByTarget;
            messagePerceivedByOthers = altinteractComp.MessagePerceivedByOthers;
            soundPerceivedByOthers = altinteractComp.SoundPerceivedByOthers;
            lastInteractTime = altinteractComp.LastInteractTime;
        }
        else
        {
            if (!TryComp<InteractionPopupComponent>(uid, out var interactComp))
                return;

            interactDelay = interactComp.InteractDelay;
            interactSuccessString = interactComp.InteractSuccessString;
            interactFailureString = interactComp.InteractFailureString;
            interactSuccessSound = interactComp.InteractSuccessSound;
            interactFailureSound = interactComp.InteractFailureSound;
            interactSuccessSpawn = interactComp.InteractSuccessSpawn;
            interactFailureSpawn = interactComp.InteractFailureSpawn;
            successChance = interactComp.SuccessChance;
            messagePerceivedByTarget = interactComp.MessagePerceivedByTarget;
            messagePerceivedByOthers = interactComp.MessagePerceivedByOthers;
            soundPerceivedByOthers = interactComp.SoundPerceivedByOthers;
            lastInteractTime = interactComp.LastInteractTime;
        }

        if (args != null)
            args.Handled = true;

        var curTime = _gameTiming.CurTime;

        if (curTime < lastInteractTime + interactDelay)
            return;

        lastInteractTime = curTime;

        if (isAlternative)
        {
            if (!TryComp<AlternativeInteractionPopupComponent>(uid, out var altinteractComp))
                return;

            altinteractComp.LastInteractTime = lastInteractTime;
        }
        else
        {
            if (!TryComp<InteractionPopupComponent>(uid, out var interactComp))
                return;

            interactComp.LastInteractTime = lastInteractTime;
        }

        // TODO: Should be an attempt event
        // TODO: Need to handle pausing with an accumulator.

        var msg = ""; // Stores the text to be shown in the popup message
        SoundSpecifier? sfx = null; // Stores the filepath of the sound to be played

        var predict = successChance is 0 or 1
                      && interactSuccessSpawn == null
                      && interactFailureSpawn == null;

        if (_netMan.IsClient && !predict)
            return;

        if (_random.Prob(successChance))
        {
            if (interactSuccessString != null)
                msg = Loc.GetString(interactSuccessString, ("target", Identity.Entity(uid, EntityManager))); // Success message (localized).

            if (interactSuccessSound != null)
                sfx = interactSuccessSound;

            if (interactSuccessSpawn != null)
                Spawn(interactSuccessSpawn, _transform.GetMapCoordinates(uid));

            var ev = new InteractionSuccessEvent(user);
            RaiseLocalEvent(target, ref ev);
        }
        else
        {
            if (interactFailureString != null)
                msg = Loc.GetString(interactFailureString, ("target", Identity.Entity(uid, EntityManager))); // Failure message (localized).

            if (interactFailureSound != null)
                sfx = interactFailureSound;

            if (interactFailureSpawn != null)
                Spawn(interactFailureSpawn, _transform.GetMapCoordinates(uid));

            var ev = new InteractionFailureEvent(user);
            RaiseLocalEvent(target, ref ev);
        }

        if (!string.IsNullOrEmpty(messagePerceivedByTarget))
        {
            var msgTarget = Loc.GetString(messagePerceivedByTarget,
                ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(uid, EntityManager)));
            _popupSystem.PopupEntity(msgTarget, uid, uid);
        }

        if (!string.IsNullOrEmpty(messagePerceivedByOthers))
        {
            var msgOthers = Loc.GetString(messagePerceivedByOthers,
                ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(uid, EntityManager)));

            var filter = Filter.PvsExcept(user, entityManager: EntityManager);

            if (!string.IsNullOrEmpty(messagePerceivedByTarget))
                filter.RemovePlayerByAttachedEntity(uid);

            _popupSystem.PopupEntity(msgOthers, uid, filter, true);
        }

        if (!predict)
        {
            _popupSystem.PopupEntity(msg, uid, user);

            if (soundPerceivedByOthers)
                _audio.PlayPvs(sfx, target);
            else
                _audio.PlayEntity(sfx, Filter.Entities(user, target), target, false);
            return;
        }

        _popupSystem.PopupClient(msg, uid, user);

        if (sfx == null)
            return;

        if (soundPerceivedByOthers)
        {
            _audio.PlayPredicted(sfx, target, user);
            return;
        }

        if (_netMan.IsClient)
        {
            if (_gameTiming.IsFirstTimePredicted)
                _audio.PlayEntity(sfx, Filter.Local(), target, true);
        }
        else
        {
            _audio.PlayEntity(sfx, Filter.Empty().FromEntities(target), target, false);
        }
    }
}
