using Content.Server.Speech.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Server.Speech.Mimic;

public sealed partial class MimicSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimicLearnerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MimicLearnerComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<MimicLearnerComponent, ListenEvent>(OnListen);
    }

    private void OnInit(Entity<MimicLearnerComponent> ent, ref ComponentInit args)
    {
        EnsureComp<ActiveListenerComponent>(ent);
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
    }
}
