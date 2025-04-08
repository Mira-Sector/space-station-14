using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost;

public sealed class GhostVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostVisualsComponent, GhostedEvent>(OnGhosted);
    }

    private void OnGhosted(Entity<GhostVisualsComponent> ent, ref GhostedEvent args)
    {
        // we use the last body they were in first
        // if that fails we then use what they selected in the lobby
        var lastBody = args.Mind.Comp.CurrentEntity;
        var usingProfile = false;

        if (!TryComp<HumanoidAppearanceComponent>(lastBody, out var humanoidComp))
        {
            if (args.Mind.Comp.Session is not {} session)
                return;

            var profile = _ticker.GetPlayerProfile(session);

            if (!_prototype.TryIndex(profile.Species, out var species))
                return;

            lastBody = EntityManager.Spawn(species.Prototype);
            _humanoid.LoadProfile(lastBody.Value, profile);

            if (!TryComp<HumanoidAppearanceComponent>(lastBody, out humanoidComp))
                return;

            usingProfile = true;
        }

        // cant immediately add it else we crash
        var ghostHumanoid = new HumanoidAppearanceComponent();
        ghostHumanoid.Species = humanoidComp.Species;
        ghostHumanoid.Initial = humanoidComp.Initial;
        AddComp(ent, ghostHumanoid);

        ghostHumanoid.Gender = humanoidComp.Gender;
        ghostHumanoid.Age = humanoidComp.Age;
        _humanoid.SetSex(ent, humanoidComp.Sex, false, ghostHumanoid);

        ent.Comp.LayersModified.Clear();
        _appearance.SetData(ent.Owner, GhostVisuals.Species, ghostHumanoid.Species);

        if (ent.Comp.TransferColor)
        {
            _appearance.SetData(ent.Owner, GhostVisuals.Color, humanoidComp.SkinColor);

            ghostHumanoid.SkinColor = humanoidComp.SkinColor;
            ghostHumanoid.EyeColor = humanoidComp.EyeColor;
        }

        ghostHumanoid.MarkingSet.Clear();

        foreach (var markingCategory in ent.Comp.MarkingsToTransfer)
        {
            if (!humanoidComp.MarkingSet.Markings.TryGetValue(markingCategory, out var markings))
                continue;

            foreach (var markingId in markings)
            {
                if (!_prototype.TryIndex<MarkingPrototype>(markingId.MarkingId, out var marking))
                    continue;

                List<Color> newColors = new();

                foreach (var color in markingId.MarkingColors)
                    newColors.Add(color.WithAlpha(ent.Comp.MarkingsAlpha));

                _humanoid.AddMarking(ent, markingId.MarkingId, newColors);
                _appearance.SetData(ent, marking.BodyPart, true);

                if (ent.Comp.LayersModified.TryGetValue(marking.BodyPart, out var markingLayers))
                {
                    markingLayers.Add(markingId.MarkingId);
                }
                else
                {
                    markingLayers = new();
                    markingLayers.Add(markingId.MarkingId);
                    ent.Comp.LayersModified.Add(marking.BodyPart, markingLayers);
                }
            }
        }

        foreach (HumanoidVisualLayers layer in Enum.GetValues(typeof(HumanoidVisualLayers)))
        {
            if (ent.Comp.LayersModified.ContainsKey(layer))
                continue;

            ghostHumanoid.PermanentlyHidden.Add(layer);
            _appearance.RemoveData(ent, layer);
        }

        ghostHumanoid.HiddenLayers.Remove(HumanoidVisualLayers.Eyes);
        _appearance.SetData(ent, HumanoidVisualLayers.Eyes, true);

        Dirty(ent, ghostHumanoid);
        Dirty(ent);

        if (usingProfile)
            EntityManager.DeleteEntity(lastBody.Value);
    }
}
