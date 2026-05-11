using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CreditsScreen : ListNavigationGameScreen
    {
        private readonly global::CreditsScreen _screen;

        public CreditsScreen(global::CreditsScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        protected override void PopulateList()
        {
            GameObjectElement credits = new GameObjectElement(
                _screen != null ? _screen.gameObject : null,
                () => Message.Localized("ui", "CREDITS.TITLE"),
                status: () => Message.Localized("ui", "CREDITS.SKIP"));
            AddElement(credits, _screen != null ? _screen.gameObject : null);
        }

        protected override string BuildSignature()
        {
            return _screen != null && _screen.gameObject.activeInHierarchy ? "active" : "inactive";
        }
    }
}
