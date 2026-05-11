using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyUnlockFeature : GameObjectElement
    {
        private static readonly FieldInfo UnlockDetailsTitleField = AccessTools.Field(typeof(global::UnlockDetailsUI), "titleLabel")!;
        private static readonly FieldInfo UnlockDetailsDescriptionField = AccessTools.Field(typeof(global::UnlockDetailsUI), "descriptionLabel")!;

        private readonly global::UnlockDetailsUI.Data _data;
        private readonly global::UnlockDetailsUI _details;

        public ProxyUnlockFeature(global::UnlockDetailsUI.Data data, global::UnlockDetailsUI details)
            : base(details != null ? details.gameObject : null, label: null)
        {
            _data = data;
            _details = details;
        }

        public override bool IsVisible => _data != null && (_details == null || _details.gameObject.activeInHierarchy);
        public override Message GetLabel()
        {
            TMP_Text title = Get<TMP_Text>(_details, UnlockDetailsTitleField);
            return Message.RawCleaned(!string.IsNullOrWhiteSpace(_data?.title) ? _data.title : AccessibilityText.ReadLocalizedText(title));
        }

        public override Message GetTooltip()
        {
            TMP_Text description = Get<TMP_Text>(_details, UnlockDetailsDescriptionField);
            return Message.RawCleaned(!string.IsNullOrWhiteSpace(_data?.description) ? _data.description : AccessibilityText.ReadLocalizedText(description));
        }

        public TMP_Text Title => Get<TMP_Text>(_details, UnlockDetailsTitleField);
        public TMP_Text Description => Get<TMP_Text>(_details, UnlockDetailsDescriptionField);

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
