using Content.Server.Speech;
using Content.Shared.Body.Components;

namespace Content.Server.Body.Systems;

public partial class BodySystem
{
    private void InitializeRelays()
    {
        SubscribeLocalEvent<BodyComponent, AccentGetEvent>(RelayToLimbs);

        SubscribeLocalEvent<BodyComponent, AccentGetEvent>(RelayToOrgans);
    }
}
