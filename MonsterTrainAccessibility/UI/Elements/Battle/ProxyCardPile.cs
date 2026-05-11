using System.Globalization;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyCardPile : GameObjectElement
    {
        private static readonly FieldInfo DeckUIField = AccessTools.Field(typeof(global::BattleHud), "deckUI")!;
        private static readonly FieldInfo DiscardUIField = AccessTools.Field(typeof(global::BattleHud), "discardUI")!;
        private static readonly FieldInfo ExhaustedUIField = AccessTools.Field(typeof(global::BattleHud), "exhaustedUI")!;
        private static readonly FieldInfo EatenUIField = AccessTools.Field(typeof(global::BattleHud), "eatenUI")!;

        private readonly global::CardPileCountUI _pile;
        private readonly PileKind _kind;

        private ProxyCardPile(global::CardPileCountUI pile, PileKind kind)
            : base(
                target: pile?.Button != null ? pile.Button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _pile = pile;
            _kind = kind;
        }

        public static ProxyCardPile Draw(global::BattleHud hud) => Create(hud, DeckUIField, PileKind.Draw);
        public static ProxyCardPile Discard(global::BattleHud hud) => Create(hud, DiscardUIField, PileKind.Discard);
        public static ProxyCardPile Consume(global::BattleHud hud) => Create(hud, ExhaustedUIField, PileKind.Consume);
        public static ProxyCardPile Eaten(global::BattleHud hud) => Create(hud, EatenUIField, PileKind.Eaten);

        public override bool IsVisible => _pile != null && _pile.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("combat", LabelKey(_kind));
        public override Message GetStatusString() => Message.RawCleaned(Count().ToString(CultureInfo.InvariantCulture));

        public global::CardPileCountUI Pile => _pile;

        private static ProxyCardPile Create(global::BattleHud hud, FieldInfo field, PileKind kind)
        {
            global::CardPileCountUI pile = hud != null ? field.GetValue(hud) as global::CardPileCountUI : null;
            return pile != null ? new ProxyCardPile(pile, kind) : null;
        }

        private int Count()
        {
            CardManager cardManager = AllGameManagers.Instance.OrNull()?.GetCardManager();
            if (cardManager == null)
            {
                return 0;
            }

            switch (_kind)
            {
                case PileKind.Draw:
                    return cardManager.GetDrawPile()?.Count ?? 0;
                case PileKind.Discard:
                    return cardManager.GetDiscardPile()?.Count ?? 0;
                case PileKind.Consume:
                    return cardManager.GetExhaustedPile()?.Count ?? 0;
                case PileKind.Eaten:
                    return cardManager.GetEatenPile()?.Count ?? 0;
                default:
                    return 0;
            }
        }

        private static string LabelKey(PileKind kind)
        {
            switch (kind)
            {
                case PileKind.Draw:
                    return "PILE.DRAW";
                case PileKind.Discard:
                    return "PILE.DISCARD";
                case PileKind.Consume:
                    return "PILE.CONSUME";
                case PileKind.Eaten:
                    return "PILE.EATEN";
                default:
                    return "PILE.DRAW";
            }
        }

        private enum PileKind
        {
            Draw,
            Discard,
            Consume,
            Eaten
        }
    }
}
