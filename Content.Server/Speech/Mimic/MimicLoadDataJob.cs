using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Prototypes;
using System.Threading.Tasks;
using System.Threading;

namespace Content.Server.Speech.Mimic;

public sealed class MimicLoadDataJob : Job<bool>
{
    private MimicManager _mimic;

    public EntProtoId Prototype;
    public EntityUid Caller;

    public MimicLoadDataJob(
        double maxTime,
        EntProtoId prototype,
        EntityUid caller,
        MimicManager mimic,
        CancellationToken cancellationToken = default) : base(maxTime, cancellationToken)
    {
        Prototype = prototype;
        Caller = caller;
        _mimic = mimic;
    }

    protected override async Task<bool> Process()
    {
        return await WaitAsyncTask<bool>(_mimic.LoadData(Prototype, Cancellation));
    }
}
