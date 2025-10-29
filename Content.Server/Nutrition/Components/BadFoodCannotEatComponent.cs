using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.Components;

[RegisterComponent]
public sealed partial class BadFoodCannotEatComponent : Component
{
    [ViewVariables]
    public HashSet<EntProtoId> CannotEat = [];
}
