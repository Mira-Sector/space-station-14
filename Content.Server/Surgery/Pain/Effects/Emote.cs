using Content.Server.Chat.Systems;
using Content.Shared.Surgery.Pain.Effects;

namespace Content.Server.Surgery.Pain.Effects;

public sealed partial class Emote : SharedEmote
{
    public override void DoEffect(IEntityManager entity, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        var chatSys = entity.System<ChatSystem>();
        chatSys.TryEmoteWithoutChat(receiver, EmoteId);
    }
}
