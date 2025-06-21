using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.Coughing;
using Robust.Shared.Random;

namespace Content.Server.Coughing;

public sealed partial class CoughingSystem : SharedCoughingSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoughOnRespireComponent, InhaledEvent>(OnInhale);
    }

    private void OnInhale(Entity<CoughOnRespireComponent> ent, ref InhaledEvent args)
    {
        var ev = new CoughGetChanceEvent();

        RaiseLocalEvent(ent, ev);

        if (ev.Cancelled)
            return;

        if (_random.Prob(ev.Chance))
            TryCoughBody(ent.Owner);
    }

    public override bool TryCough(Entity<CougherComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!base.TryCough(ent))
            return false;

        _chat.TryEmoteWithChat(ent, ent.Comp.CoughingEmote, hideLog: true);
        return true;
    }
}
