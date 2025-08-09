using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Pain.Effects;

public abstract partial class SharedEmote : SurgeryPainEffect
{
    [DataField("emote", required: true)]
    public ProtoId<EmotePrototype> EmoteId;
}
