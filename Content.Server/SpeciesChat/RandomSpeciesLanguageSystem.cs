using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.SpeciesChat;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.SpeciesChat;

public sealed partial class RandomSpeciesLanguageSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomSpeciesLanguageComponent, ComponentInit>(OnRandomInit);
    }

    private void OnRandomInit(EntityUid uid, RandomSpeciesLanguageComponent component, ComponentInit args)
    {
        EnsureComp<SpeciesLanguageComponent>(uid, out var languagesComp);

        HashSet<string> spokenLangauges = new();

        if (component.SpokenLanguages.Count <= component.SpokenLanguagesAmount)
        {
            spokenLangauges = component.SpokenLanguages;
        }
        else
        {
            var selectableLanguages = component.SpokenLanguages.Except(languagesComp.SpokenLanguages);
            for (var count = 0; count < component.SpokenLanguagesAmount; count++)
            {
                if (selectableLanguages.Count() <= 0)
                    break;

                spokenLangauges.Add(_random.PickAndTake(selectableLanguages.ToList()));
            }
        }

        HashSet<string> understoodLanguages = new();

        if (component.UnderstoodLanguages.Count <= component.UnderstoodLanguagesAmount)
        {
            understoodLanguages = component.UnderstoodLanguages;
        }
        else
        {
            var selectableLanguages = component.UnderstoodLanguages.Except(languagesComp.UnderstoodLanguages);
            for (var count = 0; count < component.UnderstoodLanguagesAmount; count++)
            {
                if (selectableLanguages.Count() <= 0)
                    break;

                understoodLanguages.Add(_random.PickAndTake(selectableLanguages.ToList()));
            }
        }

        if (spokenLangauges.Count <= 0 && understoodLanguages.Count <= 0)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        SendMessage(Loc.GetString("chat-species-fluff-intro"), actor);

        if (spokenLangauges.Count > 0)
        {
            languagesComp.SpokenLanguages.UnionWith(spokenLangauges);
            SendMessage(Loc.GetString("chat-species-fluff-spoken"), actor);
            SendLanguages(spokenLangauges.ToList(), actor);
        }

        if (understoodLanguages.Count > 0)
        {
            languagesComp.SpokenLanguages.UnionWith(understoodLanguages);
            SendMessage(Loc.GetString("chat-species-fluff-understood"), actor);
            SendLanguages(understoodLanguages.ToList(), actor);
        }

        void SendLanguages(List<string> languages, ActorComponent actor)
        {
            var msg = string.Empty;

            for (var i = 1; i <= languages.Count; i++)
            {
                if (!_prototype.TryIndex<SpeciesChannelPrototype>(languages[i - 1], out var language))
                    continue;

                msg += Loc.GetString(language.Name);

                if (i < languages.Count)
                    msg += ", ";
            }

            SendMessage(msg, actor);
        }

        void SendMessage(string msg, ActorComponent actor)
        {
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chat.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.LightCoral);
        }
    }
}
