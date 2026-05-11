using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyModSettingsCategory : UIElement, IActivatableElement
    {
        private readonly CategorySetting _category;

        public ProxyModSettingsCategory(CategorySetting category)
        {
            _category = category;
        }

        public override string GetTypeKey() => "button";

        public override Message GetLabel()
        {
            return _category?.Label;
        }

        public bool Activate()
        {
            if (_category == null)
            {
                return false;
            }

            Screens.ScreenManager.PushScreen(new ModSettingsScreen(_category));
            return true;
        }
    }
}
