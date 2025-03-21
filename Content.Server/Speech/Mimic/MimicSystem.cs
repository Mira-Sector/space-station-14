using Content.Server.Administration.Logs;
using Content.Server.Speech.Components;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Threading;

namespace Content.Server.Speech.Mimic;

public sealed partial class MimicSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly MimicManager _mimic = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const double MimicJobTime = 0.02;

    private readonly JobQueue _mimicQueue = new();
    private readonly List<(MimicLoadDataJob Job, CancellationTokenSource CancelToken)> _mimicJobs = new();

    private Dictionary<EntProtoId, Dictionary<string, float>> _toUpdate = new();

    public override void Initialize()
    {
        base.Initialize();

        _mimic.UpdateLearned += UpdateLearned;

        SubscribeLocalEvent<MimicLearnerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MimicLearnerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MimicLearnerComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<MimicLearnerComponent, ListenEvent>(OnListen);
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
                _mimicJobs.Remove((job, cancelToken));
        }
    }

    private void OnInit(Entity<MimicLearnerComponent> ent, ref ComponentInit args)
    {
        EnsureComp<ActiveListenerComponent>(ent);

        if (MetaData(ent).EntityPrototype is not {} entityPrototype)
            return;

        var cancelToken = new CancellationTokenSource();
        var job = new MimicLoadDataJob(MimicJobTime, entityPrototype, ent, _mimic, cancelToken.Token);

        _mimicJobs.Add((job, cancelToken));
        _mimicQueue.EnqueueJob(job);
    }

    private void OnShutdown(Entity<MimicLearnerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var (job, cancelToken) in _mimicJobs.ToArray())
        {
            if (job.Caller == ent.Owner)
            {
                cancelToken.Cancel();
                _mimicJobs.Remove((job, cancelToken));
            }
        }

        if (MetaData(ent).EntityPrototype is not {} entityPrototype)
            return;

        _mimic.RefreshSinglePrototype(entityPrototype);
        _mimic.SavePrototype(entityPrototype);
    }

    private void OnAttemptListen(Entity<MimicLearnerComponent> ent, ref ListenAttemptEvent args)
    {
        if (!TryComp<MobStateComponent>(ent, out var mobStateComp))
            return;

        if (_mobState.IsAlive(ent, mobStateComp))
            return;

        args.Cancel();
    }

    private void OnListen(Entity<MimicLearnerComponent> ent, ref ListenEvent args)
    {
        if (HasComp<MimicLearnerComponent>(args.Source))
            return;

        if (!_random.Prob(ent.Comp.LearningChance))
            return;

        if (MetaData(ent).EntityPrototype is not {} entityPrototype)
            return;

        if (_mimic.TryGetPhraseProb(entityPrototype, args.Message, out var _))
        {
            _mimic.AddProbToPhrase(entityPrototype, args.Message, ent.Comp.PhraseProb);
            return;
        }

        if (_toUpdate.TryGetValue(entityPrototype, out var phrases))
        {
            if (phrases.ContainsKey(args.Message))
                return;

            phrases.Add(args.Message, ent.Comp.PhraseProb);
        }
        else
        {
            phrases = new();
            phrases.Add(args.Message, ent.Comp.PhraseProb);
            _toUpdate.Add(entityPrototype, phrases);
        }

        _admin.Add(LogType.MimicLearned, LogImpact.Medium, $"{ToPrettyString(args.Source)} caused {entityPrototype.ID} to learn the phrase: {args.Message}");
    }
}
