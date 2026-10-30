using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// A shop (<see cref="global::MerchantScreen"/>): the goods side by side in one row
    /// (<see cref="ShopNodes"/>: what each is, its price, whether the coin covers it), then the services
    /// in a row under them (Purge Card, Re-roll), then Leave. Enter buys through the game, which opens
    /// what a service needs (the deck to purge from); a re-roll lands on the new goods. Escape leaves, as in the game. The run keys (Tab's
    /// HUD, Ctrl+G's coin) are live here.
    /// </summary>
    public sealed class MerchantScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Run, InputCategory.UI };

        public override string Key => "game.merchant";
        public override int Layer => 5;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static global::MerchantScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::MerchantScreen>(global::ScreenName.Merchant);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            var content = screen != null ? screen.content : null;
            if (content == null) return;
            b.PushContext(null);
            var services = new List<MerchantGoodUIBase>();
            foreach (var service in content.GetComponentsInChildren<MerchantServiceUI>(false))
                if (service.GoodState != null && !service.Claimed) services.Add(service);
            Goods(b, content);
            Row(b, "service", services);
            var leave = content.BackButton;
            if (leave != null && leave.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("shop:leave"), GameWidgets.Button(leave, () => PauseMenuScreen.Caption(leave)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        private static MerchantGoodUIBase FirstGood()
        {
            var container = Game?.content?.GoodsContainer;
            if (container == null) return null;
            foreach (Transform child in container.transform)
            {
                var good = child.GetComponent<MerchantGoodUIBase>();
                if (good != null && child.gameObject.activeInHierarchy && good.GoodState != null && !good.Claimed) return good;
            }
            return null;
        }

        // A re-roll done (the shelf's first good another than before): focus to it, read as a landing.
        // A re-roll the game refused leaves the shelf as it was, and focus where it was.
        private static void LandOnNewGoods(MerchantGoodState before, int tries)
        {
            Later.Frames(1, () =>
            {
                var first = FirstGood();
                if (first != null && first.GoodState != before) Navigation.MoveTo(ControlId.Structural("shop:good:0"));
                else if (tries > 0) LandOnNewGoods(before, tries - 1);
            }, "shop: re-roll");
        }

        // The goods, side by side in one row.
        private static void Goods(GraphBuilder b, MerchantScreenContent content)
        {
            var container = content.GoodsContainer;
            if (container == null) return;
            var goods = new List<MerchantGoodUIBase>();
            foreach (Transform child in container.transform)
            {
                var good = child.GetComponent<MerchantGoodUIBase>();
                // A good bought stays on the shelf, sold: it is no longer for sale.
                if (good != null && child.gameObject.activeInHierarchy && good.GoodState != null && !good.Claimed) goods.Add(good);
            }
            Row(b, "good", goods);
        }

        private static void Row(GraphBuilder b, string key, List<MerchantGoodUIBase> goods, int first = 0)
        {
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            for (int i = 0; i < goods.Count; i++)
            {
                var vt = ShopNodes.Good(goods[i]);
                if (vt != null && goods[i].GoodState?.RewardData is RerollMerchantRewardData)
                {
                    var buy = vt.OnActivate;
                    vt.OnActivate = () =>
                    {
                        var before = FirstGood()?.GoodState;
                        buy?.Invoke();
                        LandOnNewGoods(before, 60);
                    };
                }
                if (vt != null) items.Add(new KeyValuePair<ControlId, NodeVtable>(ControlId.Structural("shop:" + key + ":" + (first + i)), vt));
            }
            if (items.Count == 0) return;
            b.StartRow("shop:" + key);
            foreach (var item in items) b.AddItem(item.Key, item.Value);
            b.EndRow();
        }
    }
}
