using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio.Components;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Content.Server.Speech.Mimic;

public sealed partial class MimicSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MimicManager _mimic = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const double MimicJobTime = 0.02;

    private readonly JobQueue _mimicQueue = new();
    private readonly Dictionary<EntProtoId, (MimicLoadDataJob Job, CancellationTokenSource CancelToken)> _mimicJobs = new();

    private Dictionary<EntProtoId, Dictionary<string, float?>> _toUpdate = new();
    private Dictionary<EntProtoId, Dictionary<string, float>> _cachedPhrases = new();

    public override void Initialize()
    {
        base.Initialize();

        _mimic.UpdateLearned += UpdateLearned;

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<MimicLearnerComponent, ComponentInit>(OnLearnerInit);
        SubscribeLocalEvent<MimicLearnerComponent, ComponentShutdown>(OnLearnerShutdown);
        SubscribeLocalEvent<MimicLearnerComponent, ListenAttemptEvent>(OnLearnerAttemptListen);
        SubscribeLocalEvent<MimicLearnerComponent, ListenEvent>(OnLearnerListen);
        SubscribeLocalEvent<MimicLearnerComponent, RadioReceivedEvent>(OnLearnerRadio);

        SubscribeLocalEvent<MimicSpeakerComponent, ComponentInit>(OnSpeakerInit);
        SubscribeLocalEvent<MimicSpeakerComponent, EntitySpokeEvent>(OnSpeakerSpoke);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _mimic.UpdateLearned -= UpdateLearned;
    }

    private void UpdateLearned(EntProtoId prototype, Dictionary<string, float?> phrases)
    {
        if (!_toUpdate.TryGetValue(prototype, out var toAdd))
            return;

        foreach (var (phrase, prob) in toAdd)
        {
            if (phrases.ContainsKey(phrase))
                continue;

            phrases.Add(phrase, prob);
        }

        _toUpdate.Remove(prototype);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _mimicQueue.Process();

        foreach (var (prototype, (job, cancelToken)) in _mimicJobs.ToArray())
        {
            if (job.Status == JobStatus.Finished)
            {
                _mimicJobs.Remove(prototype);

                if (!job.Result)
                {
                    Log.Warning($"Failed to load mimic phrases for {job.Prototype}. Retrying");
                    LoadMimicData(job.Caller);
                    continue;
                }

                var phrasesToAdd = _mimic.GetPhraseProbs(prototype);

                if (_cachedPhrases.TryGetValue(prototype, out var phrases))
                {
                    phrases.Union(phrasesToAdd);
                }
                else
                {
                    _cachedPhrases.Add(prototype, phrasesToAdd);
                }
            }
        }

        var speakerQuery = EntityQueryEnumerator<MimicSpeakerComponent>();
        while (speakerQuery.MoveNext(out var uid, out var speakerComp))
        {
            if (speakerComp.NextMessage > _timing.CurTime)
                continue;

            speakerComp.NextMessage += NextMessageDelay(speakerComp);

            if (TryComp<MobStateComponent>(uid, out var mobStateComp) && !_mobState.IsAlive(uid, mobStateComp))
                continue;

            if (MetaData(uid).EntityPrototype is not {} entityPrototype)
                continue;

            Debug.Assert(_cachedPhrases.ContainsKey(entityPrototype));

            if (speakerComp.NextMessages.Count <= 0)
            {
                var probMultiplier = speakerComp.MidPointProb <= _cachedPhrases.Count() ? 1f : speakerComp.MidPointProb - _cachedPhrases.Count();

                // not too much randomness
                var cachedMessages = _cachedPhrases[entityPrototype];
                _random.Shuffle<KeyValuePair<string, float>>(cachedMessages.ToList());

                for (var i = 1; i <= speakerComp.CachedMessages; i++)
                {
                    string? phraseAdded = null;
                    foreach (var (phrase, prob) in cachedMessages)
                    {
                        if (!_random.Prob(Math.Min(prob * probMultiplier, 1f)))
                            continue;

                        speakerComp.NextMessages.Add(phrase);
                        phraseAdded = phrase;
                        break;
                    }

                    if (phraseAdded == null)
                        continue;

                    // so we dont pick the same phrase multiple times in a row
                    cachedMessages.Remove(phraseAdded);

                    // not enough to add a new phrase
                    if (cachedMessages.Count <= 0)
                        break;
                }

                // still fucked?
                if (speakerComp.NextMessages.Count <= 0)
                    continue;
            }

            var message = _random.Pick(speakerComp.NextMessages);
            _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, false);

            speakerComp.NextMessages.Remove(message);

            if (_random.Prob(speakerComp.LongTermForgetChance))
            {
                UpdateLongTerm(entityPrototype, message, -speakerComp.LongTermForgetProb);
            }

            if (_random.Prob(speakerComp.CurrentRoundForgetChance))
            {
                UpdateCache(entityPrototype, message, -speakerComp.CurrentRoundForgetProb);
            }
        }
    }

    private void OnLearnerInit(Entity<MimicLearnerComponent> ent, ref ComponentInit args)
    {
        EnsureComp<ActiveListenerComponent>(ent);
        LoadMimicData(ent);
    }

    private void LoadMimicData(EntityUid uid)
    {
        if (MetaData(uid).EntityPrototype is not {} entityPrototype)
            return;

        if (_cachedPhrases.ContainsKey(entityPrototype) || _mimicJobs.ContainsKey(entityPrototype))
            return;

        var cancelToken = new CancellationTokenSource();
        var job = new MimicLoadDataJob(MimicJobTime, entityPrototype, uid, _mimic, cancelToken.Token);

        _mimicJobs.Add(entityPrototype, (job, cancelToken));
        _mimicQueue.EnqueueJob(job);
    }

    private void OnLearnerShutdown(Entity<MimicLearnerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var (prototype, (job, cancelToken)) in _mimicJobs.ToArray())
        {
            if (job.Caller == ent.Owner)
            {
                cancelToken.Cancel();
                _mimicJobs.Remove(prototype);
            }
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        foreach (var (prototype, (job, cancelToken)) in _mimicJobs.ToArray())
        {
            cancelToken.Cancel();
            _mimicJobs.Remove(prototype);
        }

        foreach (var (prototype, phrases) in _toUpdate)
            _mimic.RefreshSinglePrototype(prototype);

        _mimic.Save();
    }

    private void OnLearnerAttemptListen(Entity<MimicLearnerComponent> ent, ref ListenAttemptEvent args)
    {
        if (!TryComp<MobStateComponent>(ent, out var mobStateComp))
            return;

        if (_mobState.IsAlive(ent, mobStateComp))
            return;

        args.Cancel();
    }

    private void OnLearnerListen(Entity<MimicLearnerComponent> ent, ref ListenEvent args)
    {
        if (HasComp<MimicLearnerComponent>(args.Source))
            return;

        LearnMessage(ent, args.Message, args.Source);
    }

    private void OnLearnerRadio(Entity<MimicLearnerComponent> ent, ref RadioReceivedEvent args)
    {
        if (ent.Owner == args.RadioSource || ent.Owner == args.Radio)
            return;

        if (HasComp<MimicLearnerComponent>(args.MessageSource) || HasComp<MimicLearnerComponent>(args.Radio))
            return;

        LearnMessage(ent, args.Message, args.MessageSource);
    }

    private void LearnMessage(Entity<MimicLearnerComponent> ent, string message, EntityUid source)
    {
        if (MetaData(ent).EntityPrototype is not {} entityPrototype)
            return;

        var probMultiplier = ent.Comp.MidPointProb <= _cachedPhrases.Count() ? 1f : ent.Comp.MidPointProb - _cachedPhrases.Count();

        if (_random.Prob(Math.Min(ent.Comp.LongTermLearningChance * probMultiplier, 1f)))
        {
            UpdateLongTerm(entityPrototype, message, Math.Min(ent.Comp.LongTermPhraseProb * probMultiplier, 1f));
            SendAdminLog();
        }

        if (_random.Prob(Math.Min(ent.Comp.CurrentRoundLearningChance * probMultiplier, 1f)))
        {
            UpdateCache(entityPrototype, message, Math.Min(ent.Comp.CurrentRoundPhraseProb * probMultiplier, 1f));
            SendAdminLog();
        }

        void SendAdminLog()
        {
            _admin.Add(LogType.MimicLearned, LogImpact.Medium, $"{ToPrettyString(source)} caused {entityPrototype.ID} to learn the phrase: {message}");
        }
    }

    private void OnSpeakerInit(Entity<MimicSpeakerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextMessage = _timing.CurTime + NextMessageDelay(ent.Comp);
        LoadMimicData(ent);
    }

    private void OnSpeakerSpoke(Entity<MimicSpeakerComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.SpeciesChannel != null || args.RadioChannel != null)
            return;

        if (TryComp<WearingHeadsetComponent>(ent, out var wearingHeadsetComp) && TryComp<EncryptionKeyHolderComponent>(wearingHeadsetComp.Headset, out var keysComp))
        {
            foreach (var channel in keysComp.Channels)
                _radio.SendRadioMessage(ent, args.Message, channel, wearingHeadsetComp.Headset);
        }
    }

    private TimeSpan NextMessageDelay(MimicSpeakerComponent component)
    {
        return _random.Next(component.MinDelay, component.MaxDelay);
    }

    private void UpdateLongTerm(EntProtoId entityPrototype, string phrase, float probability)
    {
        if (_toUpdate.TryGetValue(entityPrototype, out var phrases))
        {
            if (phrases.TryGetValue(phrase, out var prob))
            {
                prob += probability;

                if (prob <= 0)
                    prob = null; // weve forgotten it :(
            }
            else if (probability > 0)
            {
                phrases.Add(phrase, probability);
            }
        }
        else if (probability > 0)
        {
            phrases = new();
            phrases.Add(phrase, probability);
            _toUpdate.Add(entityPrototype, phrases);
        }
    }

    private void UpdateCache(EntProtoId entityPrototype, string phrase, float probability)
    {
        if (_cachedPhrases.TryGetValue(entityPrototype, out var phrases))
        {
            if (phrases.TryGetValue(phrase, out var prob))
            {
                prob += probability;

                if (prob <= 0)
                    phrases.Remove(phrase);
            }
            else if (probability > 0)
            {
                phrases.Add(phrase, probability);
            }
        }
        else if (probability > 0)
        {
            phrases = new();
            phrases.Add(phrase, probability);
            _cachedPhrases.Add(entityPrototype, phrases);
        }
    }
}
