using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class VocalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VocalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VocalComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VocalComponent, SexChangedEvent>(OnSexChanged);
        SubscribeLocalEvent<VocalComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<VocalComponent, ScreamActionEvent>(OnScreamAction);

        SubscribeLocalEvent<VocalOrganComponent, MapInitEvent>(OnOrganMapInit);
        SubscribeLocalEvent<VocalOrganComponent, ComponentShutdown>(OnOrganShutdown);
        SubscribeLocalEvent<VocalOrganComponent, OrganAddedToBodyEvent>(OnOrganAdded);
        SubscribeLocalEvent<VocalOrganComponent, OrganRemovedFromBodyEvent>(OnOrganRemoved);
        SubscribeLocalEvent<VocalOrganComponent, BodyOrganRelayedEvent<SexChangedEvent>>(OnOrganSexChanged);
        SubscribeLocalEvent<VocalOrganComponent, BodyOrganRelayedEvent<EmoteEvent>>(OnOrganEmote);
        //SubscribeLocalEvent<VocalOrganComponent, ScreamActionEvent>(OnOrganScreamAction);

        SubscribeLocalEvent<VocalBodyPartComponent, MapInitEvent>(OnPartMapInit);
        SubscribeLocalEvent<VocalBodyPartComponent, ComponentShutdown>(OnPartShutdown);
        SubscribeLocalEvent<VocalBodyPartComponent, BodyPartAddedToBodyEvent>(OnPartAdded);
        SubscribeLocalEvent<VocalBodyPartComponent, BodyPartRemovedFromBodyEvent>(OnPartRemoved);
        SubscribeLocalEvent<VocalBodyPartComponent, BodyLimbRelayedEvent<SexChangedEvent>>(OnPartSexChanged);
        SubscribeLocalEvent<VocalBodyPartComponent, BodyLimbRelayedEvent<EmoteEvent>>(OnPartEmote);
        //SubscribeLocalEvent<VocalBodyPartComponent, ScreamActionEvent>(OnPartScreamAction);
    }

    #region Generic

    private void OnMapInit(Entity<VocalComponent> ent, ref MapInitEvent args)
    {
        // try to add scream action when vocal comp added
        var screamAction = ent.Comp.ScreamActionEntity;
        _actions.AddAction(ent.Owner, ref screamAction, ent.Comp.ScreamAction);
        ent.Comp.ScreamActionEntity = screamAction;

        LoadSounds(ent.Comp, GetSex(ent.Owner));
    }

    private void OnShutdown(Entity<VocalComponent> ent, ref ComponentShutdown args)
    {
        // remove scream action when component removed
        if (ent.Comp.ScreamActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ScreamActionEntity);
    }

    private void OnSexChanged(Entity<VocalComponent> ent, ref SexChangedEvent args)
    {
        LoadSounds(ent.Comp, GetSex(ent.Owner));
    }

    private void OnEmote(Entity<VocalComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        PlayEmote(ent.Comp, ent.Owner, ref args);
    }

    private void OnScreamAction(Entity<VocalComponent> ent, ref ScreamActionEvent args)
    {
        if (args.Handled)
            return;

        _chat.TryEmoteWithChat(ent.Owner, ent.Comp.ScreamId);
        args.Handled = true;
    }

    #endregion

    #region Organ

    private void OnOrganMapInit(Entity<VocalOrganComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<OrganComponent>(ent.Owner, out var organComp) || organComp.Body is not { } body)
            return;

        LoadSounds(ent.Comp, GetSex(body));
    }

    private void OnOrganShutdown(Entity<VocalOrganComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<OrganComponent>(ent.Owner, out var organComp) || organComp.Body is not { } body)
            return;

        if (ent.Comp.ScreamActionEntity != null)
            _actions.RemoveAction(body, ent.Comp.ScreamActionEntity);
    }

    private void OnOrganAdded(Entity<VocalOrganComponent> ent, ref OrganAddedToBodyEvent args)
    {
        var screamAction = ent.Comp.ScreamActionEntity;
        _actions.AddAction(args.Body, ref screamAction, ent.Comp.ScreamAction);
        ent.Comp.ScreamActionEntity = screamAction;

        LoadSounds(ent.Comp, GetSex(args.Body));
    }

    private void OnOrganRemoved(Entity<VocalOrganComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        if (ent.Comp.ScreamActionEntity != null)
            _actions.RemoveAction(args.OldBody, ent.Comp.ScreamActionEntity);
    }

    private void OnOrganSexChanged(Entity<VocalOrganComponent> ent, ref BodyOrganRelayedEvent<SexChangedEvent> args)
    {
        LoadSounds(ent.Comp, GetSex(args.Body));
    }

    private void OnOrganEmote(Entity<VocalOrganComponent> ent, ref BodyOrganRelayedEvent<EmoteEvent> args)
    {
        if (args.Args.Handled)
            return;

        PlayEmote(ent.Comp, args.Body, ref args.Args);
    }

    #endregion

    #region Body Part

    private void OnPartMapInit(Entity<VocalBodyPartComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<BodyPartComponent>(ent.Owner, out var partComp) || partComp.Body is not { } body)
            return;

        LoadSounds(ent.Comp, GetSex(body));
    }

    private void OnPartShutdown(Entity<VocalBodyPartComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<BodyPartComponent>(ent.Owner, out var partComp) || partComp.Body is not { } body)
            return;

        if (ent.Comp.ScreamActionEntity != null)
            _actions.RemoveAction(body, ent.Comp.ScreamActionEntity);
    }

    private void OnPartAdded(Entity<VocalBodyPartComponent> ent, ref BodyPartAddedToBodyEvent args)
    {
        var screamAction = ent.Comp.ScreamActionEntity;
        _actions.AddAction(args.Body, ref screamAction, ent.Comp.ScreamAction);
        ent.Comp.ScreamActionEntity = screamAction;

        LoadSounds(ent.Comp, GetSex(args.Body));
    }

    private void OnPartRemoved(Entity<VocalBodyPartComponent> ent, ref BodyPartRemovedFromBodyEvent args)
    {
        if (ent.Comp.ScreamActionEntity != null)
            _actions.RemoveAction(args.Body, ent.Comp.ScreamActionEntity);
    }

    private void OnPartSexChanged(Entity<VocalBodyPartComponent> ent, ref BodyLimbRelayedEvent<SexChangedEvent> args)
    {
        LoadSounds(ent.Comp, GetSex(args.Body));
    }

    private void OnPartEmote(Entity<VocalBodyPartComponent> ent, ref BodyLimbRelayedEvent<EmoteEvent> args)
    {
        if (args.Args.Handled)
            return;

        PlayEmote(ent.Comp, args.Body, ref args.Args);
    }

    #endregion

    private void PlayEmote(IVocalComponent component, EntityUid performer, ref EmoteEvent args)
    {
        if (!args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        // snowflake case for wilhelm scream easter egg
        if (args.Emote.ID == component.ScreamId)
        {
            args.Handled = TryPlayScreamSound(component, performer);
            return;
        }

        // just play regular sound based on emote proto
        args.Handled = _chat.TryPlayEmoteSound(performer, component.EmoteSounds, args.Emote);
    }

    private bool TryPlayScreamSound(IVocalComponent component, EntityUid performer)
    {
        if (_random.Prob(component.WilhelmProbability))
        {
            _audio.PlayPvs(component.Wilhelm, performer, component.Wilhelm.Params);
            return true;
        }

        return _chat.TryPlayEmoteSound(performer, component.EmoteSounds, component.ScreamId);
    }

    public void LoadSounds(IVocalComponent component, Sex sex)
    {
        if (component.Sounds == null)
            return;

        if (!component.Sounds.TryGetValue(sex, out var protoId))
            return;

        _proto.TryIndex(protoId, out var sounds);
        component.EmoteSounds = sounds;
    }

    private Sex GetSex(EntityUid uid)
    {
        return CompOrNull<HumanoidAppearanceComponent>(uid)?.Sex ?? Sex.Unsexed;
    }
}
