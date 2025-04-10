using Content.Server.Speech.Components;
using Content.Shared.Body.Part;

namespace Content.Server.Speech.EntitySystems;

public sealed class MumbleAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MumbleAccentComponent, AccentGetEvent>(OnAccentGet);
        SubscribeLocalEvent<MumbleAccentComponent, BodyLimbRelayedEvent<AccentGetEvent>>((u, c, a) => OnAccentGet(u, c, a.Args));
        SubscribeLocalEvent<MumbleAccentComponent, BodyOrganRelayedEvent<AccentGetEvent>>((u, c, a) => OnAccentGet(u, c, a.Args));
    }

    public string Accentuate(string message, MumbleAccentComponent component)
    {
        return _replacement.ApplyReplacements(message, "mumble");
    }

    private void OnAccentGet(EntityUid uid, MumbleAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
