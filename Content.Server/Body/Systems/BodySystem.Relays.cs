using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Shared.Body.Components;

namespace Content.Server.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeRelays()
    {
        SubscribeLocalEvent<BodyComponent, AccentGetEvent>(RelayToLimbsAndOrgans);
        SubscribeLocalEvent<BodyComponent, EmoteEvent>(RelayToLimbsAndOrgans);
    }
}
