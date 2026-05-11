using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CardDetailsScreen : GameScreen
    {
        private static readonly FieldInfo CardUIField = AccessTools.Field(typeof(global::CardDetailsScreen), "cardUI")!;
        private static readonly FieldInfo NextButtonField = AccessTools.Field(typeof(global::CardDetailsScreen), "nextButton")!;
        private static readonly FieldInfo ArtistAttributionField = AccessTools.Field(typeof(global::CardDetailsScreen), "artistAttribution")!;
        private static readonly FieldInfo ArtistNameField = AccessTools.Field(typeof(global::CardDetailsScreen), "artistName")!;

        private readonly global::CardDetailsScreen _screen;

        public CardDetailsScreen(global::CardDetailsScreen screen)
        {
            _screen = screen;
        }

        public override void OnPush()
        {
            base.OnPush();
            (RootElement as ListContainer)?.FocusFirst();
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = root;

            CardUI card = Get<CardUI>(_screen, CardUIField);
            if (card != null)
            {
                ProxyCardDetailsCard element = new ProxyCardDetailsCard(card);
                root.Add(element);
                Register(card.SelectableUI?.component?.gameObject ?? card.gameObject, element);
            }

            GameObject artistRoot = Get<GameObject>(_screen, ArtistAttributionField);
            TMP_Text artistName = Get<TMP_Text>(_screen, ArtistNameField);
            if (artistRoot != null && artistName != null)
            {
                ProxyCardDetailsArtist element = new ProxyCardDetailsArtist(artistRoot, artistName);
                root.Add(element);
                Register(artistRoot, element);
            }

            AddButton(root, Get<GameUISelectableButton>(_screen, NextButtonField));
        }

        private void AddButton(ListContainer root, GameUISelectableButton button)
        {
            if (button == null)
            {
                return;
            }

            LabeledButton element = new LabeledButton(button, () => Message.FromText(AccessibleScreenText.ReadButtonLabel(button)));
            root.Add(element);
            Register(button.gameObject, element);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
