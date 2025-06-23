using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.Systems;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ResearchEvent : EntityEventArgs
{
    /// <summary>
    /// Can be raised on the purchase of a research
    /// </summary>
    public readonly EntityUid Location;
}

public sealed partial class ResearchFundingEvent : ResearchEvent
{
    /// <summary>
    /// Can be raised on the purchase of a research where funding is delivered
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TechDisciplinePrototype>? Discipline;

    [DataField(required: true)]
    public int Payment;
}
