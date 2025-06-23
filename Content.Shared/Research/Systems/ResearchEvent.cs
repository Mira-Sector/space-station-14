


using Serilog;

namespace Content.Shared.Research.Systems;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ResearchEvent : HandledEntityEventArgs
{
    /// <summary>
    /// Can be raised on the purchase of a research
    /// </summary>
    //public readonly EntityUid Location;
}

public sealed partial class ResearchFundingEvent : ResearchEvent
{
    /// <summary>
    /// Can be raised on the purchase of a research where funding is delivered
    /// </summary>
    [DataField(required: true)]
    public string? Discipline;

    [DataField(required: true)]
    public int Payment;

    public ResearchFundingEvent(int payment, string? discipline = null)
    {
        //Location = location;
        Discipline = discipline;
        Payment = payment;

        Log.Debug("Event Data Received");
    }
}
