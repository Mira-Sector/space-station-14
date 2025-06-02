using Content.Shared.Modules;

namespace Content.Client.Modules;

public sealed partial class ModuleSystem : SharedModuleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeRelay();
    }
}
