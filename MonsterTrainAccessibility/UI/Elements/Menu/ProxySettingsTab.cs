using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxySettingsTab : GameObjectElement
    {
        private static readonly FieldInfo SettingsTabButtonField = AccessTools.Field(typeof(global::SettingsTab), "button")!;

        private readonly global::SettingsTab _tab;
        private readonly GameUISelectableButton _button;

        public ProxySettingsTab(global::SettingsTab tab)
            : base(
                ResolveButton(tab) != null ? ResolveButton(tab).gameObject : null,
                typeKey: "button",
                label: null)
        {
            _tab = tab;
            _button = ResolveButton(tab);
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.RawCleaned(GameUIButtonSupport.ResolveLabel(_button)) ?? AuthoredLabelReader.ReadMessage(_button);
        public override Message GetStatusString()
        {
            if (_tab?.SectionRoot != null && _tab.SectionRoot.activeInHierarchy)
            {
                return Message.Localized("messages", "state.selected");
            }

            return GameButtonElement.StateMessage(_button);
        }

        public GameUISelectableButton Button => _button;

        private static GameUISelectableButton ResolveButton(global::SettingsTab tab)
        {
            return tab != null ? SettingsTabButtonField.GetValue(tab) as GameUISelectableButton : null;
        }
    }
}
