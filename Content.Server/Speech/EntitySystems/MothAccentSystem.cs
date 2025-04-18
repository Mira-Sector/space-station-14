using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Body.Part;

namespace Content.Server.Speech.EntitySystems;

public sealed class MothAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerBuzz = new Regex("z{1,3}");
    private static readonly Regex RegexUpperBuzz = new Regex("Z{1,3}");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothAccentComponent, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<MothAccentComponent, BodyLimbRelayedEvent<AccentGetEvent>>((u, c, a) => OnAccent(u, c, a.Args));
        SubscribeLocalEvent<MothAccentComponent, BodyOrganRelayedEvent<AccentGetEvent>>((u, c, a) => OnAccent(u, c, a.Args));
    }

    private void OnAccent(EntityUid uid, MothAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // buzzz
        message = RegexLowerBuzz.Replace(message, "zzz");
        // buZZZ
        message = RegexUpperBuzz.Replace(message, "ZZZ");

        args.Message = message;
    }
}
