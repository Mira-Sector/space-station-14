using Content.Server.Speech.Components;
using Content.Shared.Body.Part;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class BackwardsAccentSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<BackwardsAccentComponent, AccentGetEvent>(OnAccent);
            SubscribeLocalEvent<BackwardsAccentComponent, BodyLimbRelayedEvent<AccentGetEvent>>((u, c, a) => OnAccent(u, c, a.Args));
            SubscribeLocalEvent<BackwardsAccentComponent, BodyOrganRelayedEvent<AccentGetEvent>>((u, c, a) => OnAccent(u, c, a.Args));
        }

        public string Accentuate(string message)
        {
            var arr = message.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        private void OnAccent(EntityUid uid, BackwardsAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
