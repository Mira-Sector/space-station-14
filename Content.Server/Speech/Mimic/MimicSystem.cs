using Content.Server.Speech.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Random;
using System.Threading;

namespace Content.Server.Speech.Mimic;

public sealed partial class MimicSystem : EntitySystem
{
    [Dependency] private readonly MimicManager _mimic = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const double MimicJobTime = 0.02;

    private readonly JobQueue _mimicQueue = new();
    private readonly List<(MimicLoadDataJob Job, CancellationTokenSource CancelToken)> _mimicJobs = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimicLearnerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MimicLearnerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<MimicLearnerComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<MimicLearnerComponent, ListenEvent>(OnListen);
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

    private void OnRemove(Entity<MimicLearnerComponent> ent, ref ComponentRemove args)
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

        _mimic.AddProbToPhrase(entityPrototype, args.Message, ent.Comp.PhraseProb);
    }
}
