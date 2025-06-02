using Content.Shared.Modules.ModSuit;

namespace Content.Client.Modules.ModSuit;

public sealed partial class ModSuitSystem : SharedModSuitSystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeDeployable();
        InitializeSealable();
    }
}
