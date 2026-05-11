using System;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Compendium;
using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumEnemyButton : ProxyCompendiumGameButton
    {
        private readonly global::CharacterButtonUI _button;
        private readonly Func<CompendiumEnemyPresentationSource> _selectedSource;

        public ProxyCompendiumEnemyButton(
            global::CharacterButtonUI button,
            Func<CompendiumEnemyPresentationSource> selectedSource)
            : base(button?.Button)
        {
            _button = button;
            _selectedSource = selectedSource;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.RawCleaned(_button?.GetCurrentDisplayName());
        }

        public override Message GetStatusString()
        {
            if (_button == null)
            {
                return null;
            }

            if (!_button.IsDiscovered)
            {
                return Message.Localized("messages", "state.locked");
            }

            return _button.IsChosen ? Message.Localized("messages", "state.selected") : null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            if (_button == null || !_button.IsChosen)
            {
                return "ui";
            }

            PresentationBuffer<CompendiumEnemyPresentationSource> buffer =
                buffers?.GetBuffer("compendium_enemy") as PresentationBuffer<CompendiumEnemyPresentationSource>;
            CompendiumEnemyPresentationSource source = _selectedSource?.Invoke();
            if (buffer == null || source == null)
            {
                return "ui";
            }

            buffer.Bind(source);
            buffers.EnableBuffer("compendium_enemy", true);
            return "compendium_enemy";
        }
    }
}
