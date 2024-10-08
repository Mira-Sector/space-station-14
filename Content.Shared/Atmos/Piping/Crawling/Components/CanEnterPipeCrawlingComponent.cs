using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CanEnterPipeCrawlingComponent : Component
{
    [DataField]
    public float EnterPipeDoAfterTime = 2.5f;

    /// <summary>
    ///     How many seconds until the next move attempt to annother pipe
    /// </summary>
    /// <remarks>
    ///     If null uses the entities sprinting speed * 0.8
    /// </remarks>
    [DataField]
    public float? PipeMoveSpeed;
}
