using Content.Shared.MedicalScanner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.HealthAnalyzer.UI
{
    [UsedImplicitly]
    public sealed class HealthAnalyzerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private BaseHealthAnalyzerWindow? _window;

        public HealthAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (message is not HealthAnalyzerScannedUserMessage cast)
                return;

            if (_window == null)
            {
                if (!EntMan.TryGetEntity(cast.TargetEntity, out var target))
                    return;

                _window = EntMan.HasComponent<HealthAnalyzerBodyComponent>(target) ? this.CreateWindow<HealthAnalyzerBodyWindow>() : this.CreateWindow<HealthAnalyzerWindow>();
                _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

                if (_window == null)
                    return;
            }

            _window.Populate(cast);
        }
    }
}
