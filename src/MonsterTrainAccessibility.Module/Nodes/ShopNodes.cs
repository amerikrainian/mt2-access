using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using TMPro;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// What a shop sells (<see cref="MerchantGoodUIBase"/>), each a button that buys it through the
    /// game's own buy button: a good as its reward panel shows it (<see cref="RewardNodes"/>: an
    /// artifact, a card), a service by its sign ("Purge Card", what it does); then the price as the buy
    /// button draws it ("40 Coin", the game's "Sold" once bought), and "can't afford" while the coin is
    /// short. Landing on it shows it as the pointer does, its tooltips with it.
    /// </summary>
    internal static class ShopNodes
    {
        public static NodeVtable Good(MerchantGoodUIBase good)
        {
            NodeVtable vt = null;
            if (good is MerchantGoodDetailsUI details && details.rewardUI != null)
                foreach (var reward in RewardNodes.Details(details.rewardUI)) { vt = reward; break; }
            else if (good is MerchantServiceUI service && Shown(service.titleLabel) != null)
                vt = new NodeVtable
                {
                    Announcements = new List<NodeAnnouncement>
                    {
                        GameNodes.LabelPart(() => Shown(service.titleLabel)),
                        GameNodes.TooltipPart(() => Shown(service.descriptionLabel)),
                    },
                    SearchText = () => Shown(service.titleLabel),
                };
            if (vt == null) return null;
            var parts = new List<NodeAnnouncement>(vt.Announcements);
            parts.Insert(1, new NodeAnnouncement(() => Price(good), kind: AnnouncementKinds.Value));
            parts.Insert(2, new NodeAnnouncement(() => good.Claimed || good.Affordable ? null : Strings.Get("shop.unaffordable"),
                kind: AnnouncementKinds.Enabled));
            vt.Announcements = parts;
            vt.ControlType = ControlTypes.Button;
            var hover = vt.OnFocus;
            vt.OnFocus = () =>
            {
                GameInput.Hover(good.SelectableUI);
                hover?.Invoke();
            };
            vt.OnActivate = () => GameInput.Submit(good.SelectableUI);
            return vt;
        }

        // The buy button's label: the price, or the game's word once sold.
        private static string Price(MerchantGoodUIBase good)
        {
            var button = good.buyButton;
            string price = button != null && button.gameObject.activeInHierarchy ? Shown(button.label) : null;
            // The coin is an icon after the number: its word, without the icon's padding.
            return price != null ? Core.Speech.IconWords(price).Trim() : null;
        }

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
    }
}
