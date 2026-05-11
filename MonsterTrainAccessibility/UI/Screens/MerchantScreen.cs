using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class MerchantScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo ContentField = AccessTools.Field(typeof(global::MerchantScreen), "content")!;
        private static readonly FieldInfo GoodUIsField = AccessTools.Field(typeof(global::MerchantScreen), "goodUIs")!;
        private static readonly FieldInfo SourceMerchantDataField = AccessTools.Field(typeof(global::MerchantScreen), "sourceMerchantData")!;
        private static readonly FieldInfo CharacterSpeechLabelField = AccessTools.Field(typeof(global::MerchantCharacterUI), "speechBubbleLabel")!;

        private readonly global::MerchantScreen _screen;

        public MerchantScreen(global::MerchantScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        protected override void PopulateList()
        {
            global::MerchantScreenContent content = Get<global::MerchantScreenContent>(_screen, ContentField);
            AddMerchantDialogue(content?.Character);

            List<MerchantGoodUIBase> goods = Get<List<MerchantGoodUIBase>>(_screen, GoodUIsField);
            MerchantData merchantData = Get<MerchantData>(_screen, SourceMerchantDataField);
            AddGoodGroup(goods, merchantData, isService: false, Message.Localized("ui", "GROUPS.MERCHANT.GOODS").Resolve());
            AddGoodGroup(goods, merchantData, isService: true, Message.Localized("ui", "GROUPS.MERCHANT.SERVICES").Resolve());

            if (content?.BackButton != null)
            {
                LabeledButton back = new LabeledButton(content.BackButton, "MERCHANT.LEAVE");
                AddElement(back, content.BackButton.gameObject);
            }
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            AppendDialogueSignature(sb, Get<global::MerchantScreenContent>(_screen, ContentField)?.Character);

            List<MerchantGoodUIBase> goods = Get<List<MerchantGoodUIBase>>(_screen, GoodUIsField);
            if (goods != null)
            {
                for (int i = 0; i < goods.Count; i++)
                {
                    MerchantGoodUIBase good = goods[i];
                    sb.Append(good != null && good.gameObject.activeInHierarchy)
                        .Append(':')
                        .Append(good?.GoodState?.RewardData?.RewardTitle)
                        .Append(':')
                        .Append(good?.GoodState?.Claimed)
                        .Append('|');
                }
            }

            return sb.ToString();
        }

        private static void AppendDialogueSignature(System.Text.StringBuilder sb, global::MerchantCharacterUI character)
        {
            TMP_Text label = Get<TMP_Text>(character, CharacterSpeechLabelField);
            bool hasText = Message.ShouldAdd(Message.Clean(AccessibilityText.ReadLocalizedText(label)));
            sb.Append("dialogue:")
                .Append(character != null && character.gameObject.activeInHierarchy)
                .Append(':')
                .Append(label != null && label.gameObject.activeInHierarchy)
                .Append(':')
                .Append(hasText)
                .Append('|');
        }

        private void AddMerchantDialogue(global::MerchantCharacterUI character)
        {
            if (character == null)
            {
                return;
            }

            TMP_Text label = Get<TMP_Text>(character, CharacterSpeechLabelField);
            ProxyMerchantDialogue element = new ProxyMerchantDialogue(character, label);
            AddElement(element, character.gameObject, label != null ? label.gameObject : null);
        }

        private void AddGoodGroup(List<MerchantGoodUIBase> goods, MerchantData merchantData, bool isService, string label)
        {
            if (goods == null)
            {
                return;
            }

            ListContainer group = new ListContainer(label, NavigationAxis.Horizontal)
            {
                AnnouncePosition = false
            };
            for (int i = 0; i < goods.Count; i++)
            {
                MerchantGoodUIBase good = goods[i];
                if (good == null || good.GoodState == null || good.GoodState.IsService != isService)
                {
                    continue;
                }

                ProxyMerchantGood element = new ProxyMerchantGood(good, merchantData);
                group.Add(element);
                Register(good.gameObject, element);
                if (good.SelectableUI?.component != null)
                {
                    Register(good.SelectableUI.component.gameObject, element);
                }
            }

            if (group.Children.Count > 0)
            {
                RootList.Add(group);
            }
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
