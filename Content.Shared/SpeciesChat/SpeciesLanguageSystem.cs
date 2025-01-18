using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.SpeciesChat;

public sealed partial class SpeciesLanguageSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomSpeciesLanguageComponent, ComponentInit>(OnRandomInit);
    }

    private void OnRandomInit(EntityUid uid, RandomSpeciesLanguageComponent component, ComponentInit args)
    {
        EnsureComp<SpeciesLanguageComponent>(uid, out var languagesComp);

        if (component.SpokenLanguages.Count <= component.SpokenLanguagesAmount)
        {
            languagesComp.SpokenLanguages.Concat(component.SpokenLanguages);
        }
        else
        {
            for (var count = 0; count < component.SpokenLanguagesAmount; count++)
            {
                languagesComp.SpokenLanguages.Add(_random.PickAndTake(component.SpokenLanguages.ToList()));
            }
        }

        if (component.UnderstoodLanguages.Count <= component.UnderstoodLanguagesAmount)
        {
            languagesComp.UnderstoodLanguages.Concat(component.UnderstoodLanguages);
        }
        else
        {
            for (var count = 0; count < component.UnderstoodLanguagesAmount; count++)
            {
                languagesComp.UnderstoodLanguages.Add(_random.PickAndTake(component.UnderstoodLanguages.ToList()));
            }
        }
    }
}
