using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio;
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

        SubscribeLocalEvent<VocalOrganComponent, OrganAddedEvent>(OnOrganAdded);
        SubscribeLocalEvent<VocalOrganComponent, OrganRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<VocalOrganComponent, BodyOrganRelayedEvent<GetEmoteSoundsEvent>>(OnOrganGetEmotes);
        SubscribeLocalEvent<VocalOrganBodyPartComponent, BodyPartAddedEvent>(OnOrganBodyPartAdded);
        SubscribeLocalEvent<VocalOrganBodyPartComponent, BodyPartRemovedEvent>(OnOrganBodyPartRemoved);

        SubscribeLocalEvent<VocalBodyPartComponent, BodyPartAddedEvent>(OnBodyPartAdded);
        SubscribeLocalEvent<VocalBodyPartComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);
        SubscribeLocalEvent<VocalBodyPartComponent, BodyLimbRelayedEvent<GetEmoteSoundsEvent>>(OnBodyPartGetEmotes);
    }

    private void OnMapInit(EntityUid uid, VocalComponent component, MapInitEvent args)
    {
        // try to add scream action when vocal comp added
        _actions.AddAction(uid, ref component.ScreamActionEntity, component.ScreamAction);
        UpdateSounds((uid, component));
    }

    private void OnShutdown(EntityUid uid, VocalComponent component, ComponentShutdown args)
    {
        // remove scream action when component removed
        if (component.ScreamActionEntity != null)
        {
            _actions.RemoveAction(uid, component.ScreamActionEntity);
        }
    }

    private void OnSexChanged(EntityUid uid, VocalComponent component, SexChangedEvent args)
    {
        UpdateSounds((uid, component));
    }

    private void OnEmote(EntityUid uid, VocalComponent component, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        // snowflake case for wilhelm scream easter egg
        if (args.Emote.ID == component.ScreamId)
        {
            args.Handled = TryPlayScreamSound(uid, component);
            return;
        }

        // just play regular sound based on emote proto
        args.Handled = _chat.TryPlayEmoteSound(uid, component.EmoteSounds, args.Emote);
    }

    private void OnScreamAction(EntityUid uid, VocalComponent component, ScreamActionEvent args)
    {
        if (args.Handled)
            return;

        _chat.TryEmoteWithChat(uid, component.ScreamId);
        args.Handled = true;
    }

    private bool TryPlayScreamSound(EntityUid uid, VocalComponent component)
    {
        if (_random.Prob(component.WilhelmProbability))
        {
            _audio.PlayPvs(component.Wilhelm, uid, component.Wilhelm.Params);
            return true;
        }

        return _chat.TryPlayEmoteSound(uid, component.EmoteSounds, component.ScreamId);
    }

    private void OnOrganAdded(Entity<VocalOrganComponent> ent, ref OrganAddedEvent args)
    {
        if (!TryComp<BodyPartComponent>(args.Part, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        EnsureComp<VocalOrganBodyPartComponent>(args.Part).Organs += 1;
        EnsureComp<VocalComponent>(body, out var vocalComp);
        UpdateSounds((body, vocalComp));
    }

    private void OnOrganRemoved(Entity<VocalOrganComponent> ent, ref OrganRemovedEvent args)
    {
        if (!TryComp<BodyPartComponent>(args.OldPart, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        var organs = EntityManager.GetComponent<VocalOrganBodyPartComponent>(args.OldPart).Organs -= 1;

        if (organs <= 0)
            RemComp<VocalOrganBodyPartComponent>(args.OldPart);

        UpdateSounds(body);
    }

    private void OnOrganBodyPartAdded(Entity<VocalOrganBodyPartComponent> ent, ref BodyPartAddedEvent args)
    {
        if (!TryComp<BodyPartComponent>(ent, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        EnsureComp<VocalComponent>(body, out var vocalComp);
        UpdateSounds((body, vocalComp));
    }

    private void OnOrganBodyPartRemoved(Entity<VocalOrganBodyPartComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (!TryComp<BodyPartComponent>(ent, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        UpdateSounds(body);
    }

    private void OnOrganGetEmotes(Entity<VocalOrganComponent> ent, ref BodyOrganRelayedEvent<GetEmoteSoundsEvent> args)
    {
        if (!ent.Comp.Sounds.TryGetValue(GetSex(args.Body), out var sounds))
            return;

        MergeSounds(sounds, args.Args.Sounds);
    }

    private void OnBodyPartAdded(Entity<VocalBodyPartComponent> ent, ref BodyPartAddedEvent args)
    {
        if (!TryComp<BodyPartComponent>(ent, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        EnsureComp<VocalComponent>(body, out var vocalComp);
        UpdateSounds((body, vocalComp));
    }

    private void OnBodyPartRemoved(Entity<VocalBodyPartComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (!TryComp<BodyPartComponent>(ent, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        UpdateSounds(body);
    }

    private void OnBodyPartGetEmotes(Entity<VocalBodyPartComponent> ent, ref BodyLimbRelayedEvent<GetEmoteSoundsEvent> args)
    {
        if (!ent.Comp.Sounds.TryGetValue(GetSex(args.Body), out var sounds))
            return;

        MergeSounds(sounds, args.Args.Sounds);
    }

    public void UpdateSounds(Entity<VocalComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.EmoteSounds = new();

        var sex = GetSex(ent);

        if (ent.Comp.Sounds != null)
        {
            if (!ent.Comp.Sounds.TryGetValue(sex, out var protoId))
                return;

            if (!_proto.TryIndex(protoId, out var sexSounds))
                return;

            ent.Comp.EmoteSounds = sexSounds;
        }

        var ev = new GetEmoteSoundsEvent(ent.Comp.EmoteSounds);
        RaiseLocalEvent(ent, ev);

        ent.Comp.EmoteSounds = ev.Sounds;
        Dirty(ent);
    }

    private Sex GetSex(EntityUid uid)
    {
        return CompOrNull<HumanoidAppearanceComponent>(uid)?.Sex ?? Sex.Unsexed;
    }

    internal void MergeSounds(ProtoId<EmoteSoundsPrototype> emoteSoundsId, EmoteSounds existingSounds)
    {
        if (!_proto.TryIndex(emoteSoundsId, out var emoteSounds))
            return;

        if (emoteSounds.FallbackSound != null)
            existingSounds.FallbackSound = emoteSounds.FallbackSound;

        foreach (var (emote, sound) in emoteSounds.Sounds)
        {
            if (!existingSounds.Sounds.TryAdd(emote, sound))
                existingSounds.Sounds[emote] = sound;
        }
    }
}
