using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SubFloor;

public abstract class SharedTrayScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public const float SubfloorRevealAlpha = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, ComponentGetState>(OnTrayScannerGetState);
        SubscribeLocalEvent<TrayScannerComponent, ComponentHandleState>(OnTrayScannerHandleState);
        SubscribeLocalEvent<TrayScannerComponent, ActivateInWorldEvent>(OnTrayScannerActivate);
        SubscribeLocalEvent<TrayScannerComponent, ComponentInit>(OnTrayScannerInit);
        SubscribeLocalEvent<TrayScannerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnTrayScannerInit(EntityUid uid, TrayScannerComponent scanner, ref ComponentInit args)
    {
        if (scanner.EnabledEntity)
            SetScannerEnabled(uid, scanner.Enabled, scanner);

        foreach (var layer in scanner.ToggleableLayers)
            scanner.RevealedLayers.Add(layer);

        Dirty(uid, scanner);
    }

    private void OnGetVerbs(EntityUid uid, TrayScannerComponent scanner, GetVerbsEvent<Verb> args)
    {
        if (!scanner.CanToggleLayers)
            return;

        foreach (var layer in scanner.ToggleableLayers)
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pipe-layerable-status", ("layer", layer)),
                Category = VerbCategory.TrayLayer,
                Priority = layer,
                Act = () =>
                {
                    if (scanner.RevealedLayers.Contains(layer))
                    {
                        scanner.RevealedLayers.Remove(layer);
                    }
                    else
                    {
                        scanner.RevealedLayers.Add(layer);
                    }

                    Dirty(uid, scanner);
                }
            };

            if (scanner.RevealedLayers.Contains(layer))
            {
                verb.TextStyleClass = Verbs.AlternativeVerb.DefaultTextStyleClass;
                verb.Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Changelog/minus.svg.192dpi.png"));
            }
            else
            {
                verb.TextStyleClass = Verbs.Verb.DefaultTextStyleClass;
                verb.Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Changelog/plus.svg.192dpi.png"));
            }

            args.Verbs.Add(verb);
        }
    }

    private void OnTrayScannerActivate(EntityUid uid, TrayScannerComponent scanner, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        SetScannerEnabled(uid, !scanner.Enabled, scanner);
        args.Handled = true;
    }

    private void SetScannerEnabled(EntityUid uid, bool enabled, TrayScannerComponent? scanner = null)
    {
        if (!Resolve(uid, ref scanner) || scanner.Enabled == enabled)
            return;

        scanner.Enabled = enabled;
        Dirty(uid, scanner);

        // We don't remove from _activeScanners on disabled, because the update function will handle that, as well as
        // managing the revealed subfloor entities

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, TrayScannerVisual.Visual, scanner.Enabled ? TrayScannerVisual.On : TrayScannerVisual.Off, appearance);
        }
    }

    private void OnTrayScannerGetState(EntityUid uid, TrayScannerComponent scanner, ref ComponentGetState args)
    {
        args.State = new TrayScannerState(scanner.Enabled, scanner.Range, scanner.RevealedLayers);
    }

    private void OnTrayScannerHandleState(EntityUid uid, TrayScannerComponent scanner, ref ComponentHandleState args)
    {
        if (args.Current is not TrayScannerState state)
            return;

        scanner.Range = state.Range;
        scanner.RevealedLayers = state.RevealedLayers;
        SetScannerEnabled(uid, state.Enabled, scanner);
    }
}

[Serializable, NetSerializable]
public enum TrayScannerVisual : sbyte
{
    Visual,
    On,
    Off
}
