using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSetupClanSummary : GameObjectElement
    {
        private static readonly FieldInfo RandomTitleKeyField = AccessTools.Field(typeof(global::RunSetupClassLevelInfoUI), "randomTitleKey")!;
        private static readonly FieldInfo RandomLegacyEraTitleKeyField = AccessTools.Field(typeof(global::RunSetupClassLevelInfoUI), "randomLegacyEraTitleKey")!;
        private static readonly FieldInfo RandomYoungEraTitleKeyField = AccessTools.Field(typeof(global::RunSetupClassLevelInfoUI), "randomYoungEraTitleKey")!;

        private readonly GameUISelectableButton _button;
        private readonly global::RunSetupClassLevelInfoUI _info;
        private readonly SaveManager _saveManager;
        private readonly string[] _labelTerms;

        public ProxyRunSetupClanSummary(GameUISelectableButton button, global::RunSetupClassLevelInfoUI info, SaveManager saveManager, params string[] labelTerms)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _info = info;
            _saveManager = saveManager;
            _labelTerms = labelTerms;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.RawCleaned(FormatGameLabel(ResolveClanName(), _labelTerms));
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        public override Message GetTooltip()
        {
            ClassData clan = _info?.ClassData;
            if (clan == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>
            {
                Message.FromText(_info.IsMainClass ? clan.GetDescription() : clan.GetSubclassDescription()),
                ClanLevelProgress(clan, _saveManager, _info.Level)
            };
            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal static Message ClanLevelProgress(ClassData clan, SaveManager saveManager, int fallbackLevel)
        {
            if (clan == null)
            {
                return null;
            }

            int level = fallbackLevel;
            int xp = 0;
            BalanceData balanceData = saveManager?.GetBalanceData();
            if (saveManager != null)
            {
                level = saveManager.GetClassLevelInMetagame(clan.GetID());
                xp = saveManager.GetClassXP(clan.GetID());
            }

            if (balanceData == null)
            {
                return Message.Localized("ui", "RUN_SETUP.CLAN_LEVEL", new { level });
            }

            balanceData.ApplyClassXP(ref level, ref xp);
            if (!balanceData.HasNextClassLevel(level))
            {
                return Message.Localized("ui", "RUN_SETUP.CLAN_LEVEL_MAX", new { level });
            }

            int total = balanceData.GetXPRequiredForNextClassLevel(level);
            string format = AccessibilityText.LocalizeTerm("TextFormat_Divide");
            string xpText = !string.IsNullOrWhiteSpace(format)
                ? string.Format(format, LocalizationUtil.FormatNumber(xp), LocalizationUtil.FormatNumber(total))
                : LocalizationUtil.FormatNumber(xp) + "/" + LocalizationUtil.FormatNumber(total);
            return Message.Localized("ui", "RUN_SETUP.CLAN_LEVEL_XP", new { level, xp = xpText });
        }

        private string ResolveClanName()
        {
            ClassData clan = _info?.ClassData;
            if (clan != null)
            {
                return AccessibilityText.LocalizeTerm(clan.GetTitleKey());
            }

            return LocalizeRandomTitle();
        }

        private string LocalizeRandomTitle()
        {
            if (_info == null)
            {
                return string.Empty;
            }

            FieldInfo field = RandomTitleKeyField;
            if (_info.RandomId == global::RunSetupClanSelectionLayoutUI.RandomIdLegacyEraOnly)
            {
                field = RandomLegacyEraTitleKeyField;
            }
            else if (_info.RandomId == global::RunSetupClanSelectionLayoutUI.RandomIdYoungEraOnly)
            {
                field = RandomYoungEraTitleKeyField;
            }

            return AccessibilityText.LocalizeTerm(field.GetValue(_info) as string);
        }

        private static string FormatGameLabel(string value, params string[] labelTerms)
        {
            value = Message.Clean(value);
            string label = LocalizeFirstTerm(labelTerms);
            if (string.IsNullOrWhiteSpace(label))
            {
                return value;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                return label;
            }

            string format = AccessibilityText.LocalizeTerm("TextFormat_Colon");
            return !string.IsNullOrWhiteSpace(format)
                ? Message.Clean(string.Format(format, label, value))
                : Message.Clean(label + ": " + value);
        }

        private static string LocalizeFirstTerm(params string[] terms)
        {
            if (terms == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < terms.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(terms[i]) && terms[i].HasTranslation())
                {
                    string text = AccessibilityText.LocalizeTerm(terms[i]);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }

            return string.Empty;
        }
    }
}
