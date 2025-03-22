using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
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
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const double MimicJobTime = 0.02;

    private readonly JobQueue _mimicQueue = new();
    private readonly List<(MimicLoadDataJob Job, CancellationTokenSource CancelToken)> _mimicJobs = new();

    private Dictionary<EntProtoId, Dictionary<string, float>> _toUpdate = new();
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

        SubscribeLocalEvent<MimicSpeakerComponent, ComponentInit>(OnSpeakerInit);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _mimic.UpdateLearned -= UpdateLearned;
    }

    private void UpdateLearned(EntProtoId prototype, Dictionary<string, float> phrases)
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

        foreach (var (job, cancelToken) in _mimicJobs.ToArray())
        {
            if (job.Status == JobStatus.Finished)
            {
                _mimicJobs.Remove((job, cancelToken));

                var phrasesToAdd = _mimic.GetPhraseProbs(job.Prototype);

                if (_cachedPhrases.TryGetValue(job.Prototype, out var phrases))
                {
                    phrases.Union(phrasesToAdd);
                }
                else
                {
                    _cachedPhrases.Add(job.Prototype, phrasesToAdd);
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
                // not too much randomness
                var cachedMessages = _cachedPhrases[entityPrototype];
                _random.Shuffle<KeyValuePair<string, float>>(cachedMessages.ToList());

                for (var i = 1; i <= speakerComp.CachedMessages; i++)
                {
                    string? phraseAdded = null;
                    foreach (var (phrase, prob) in cachedMessages)
                    {
                        if (!_random.Prob(prob))
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
            _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, true);
            speakerComp.NextMessages.Remove(message);
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

        if (_cachedPhrases.ContainsKey(entityPrototype))
            return;

        var cancelToken = new CancellationTokenSource();
        var job = new MimicLoadDataJob(MimicJobTime, entityPrototype, uid, _mimic, cancelToken.Token);

        _mimicJobs.Add((job, cancelToken));
        _mimicQueue.EnqueueJob(job);
    }

    private void OnLearnerShutdown(Entity<MimicLearnerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var (job, cancelToken) in _mimicJobs.ToArray())
        {
            if (job.Caller == ent.Owner)
            {
                cancelToken.Cancel();
                _mimicJobs.Remove((job, cancelToken));
            }
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        foreach (var (job, cancelToken) in _mimicJobs.ToArray())
        {
            cancelToken.Cancel();
            _mimicJobs.Remove((job, cancelToken));
        }

        foreach (var (prototype, phrases) in _toUpdate)
        {
            _mimic.RefreshSinglePrototype(prototype);
            _mimic.SavePrototype(prototype);
        }

        _toUpdate.Clear();
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

        if (!_random.Prob(ent.Comp.LearningChance))
            return;

        if (MetaData(ent).EntityPrototype is not {} entityPrototype)
            return;

        if (_toUpdate.TryGetValue(entityPrototype, out var phrases))
        {
            if (phrases.TryGetValue(args.Message, out var prob))
            {
                prob += ent.Comp.PhraseProb;
            }
            else
            {
                phrases.Add(args.Message, ent.Comp.PhraseProb);
            }
        }
        else
        {
            phrases = new();
            phrases.Add(args.Message, ent.Comp.PhraseProb);
            _toUpdate.Add(entityPrototype, phrases);
        }

        // also add it to our cache
        if (_cachedPhrases.TryGetValue(entityPrototype, out var cachedPhrases))
        {
            if (cachedPhrases.TryGetValue(args.Message, out var prob))
            {
                prob += ent.Comp.PhraseProb;
            }
            else
            {
                cachedPhrases.Add(args.Message, ent.Comp.PhraseProb);
            }
        }
        else
        {
            cachedPhrases = new();
            cachedPhrases.Add(args.Message, ent.Comp.PhraseProb);
            _cachedPhrases.Add(entityPrototype, phrases);
        }

        _admin.Add(LogType.MimicLearned, LogImpact.Medium, $"{ToPrettyString(args.Source)} caused {entityPrototype.ID} to learn the phrase: {args.Message}");
    }

    private void OnSpeakerInit(Entity<MimicSpeakerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextMessage = _timing.CurTime + NextMessageDelay(ent.Comp);
        LoadMimicData(ent);
    }

    private TimeSpan NextMessageDelay(MimicSpeakerComponent component)
    {
        return _random.Next(component.MinDelay, component.MaxDelay);
    }
}
