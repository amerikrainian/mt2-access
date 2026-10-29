using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Screens
{
    public sealed partial class CompendiumScreen
    {
        // ---- Cards ----

        // The filters, each option filter a row (clan, rarity, cost) and the two dropdowns one; the
        // search on a line of its own (Left and Right are its caret's); then the page's cards as a row,
        // each by its place on the page: a page turn keeps focus on the same place, on the new card.
        private void Cards(GraphBuilder b, CompendiumSectionCards cards)
        {
            var toolbar = cards.filterToolbar;
            if (toolbar != null)
            {
                FilterNodes.Options(b, "logbook:filter:clan", toolbar.classFilterUI);
                FilterNodes.Options(b, "logbook:filter:rarity", toolbar.rarityFilterUI);
                FilterNodes.Options(b, "logbook:filter:cost", toolbar.costFilterUI);
                if (FilterNodes.Shown(toolbar.typeFilterUI) || FilterNodes.Shown(toolbar.masteryFilterUI))
                {
                    b.StartRow();
                    FilterNodes.Dropdown(b, "logbook:filter:type", toolbar.typeFilterUI);
                    FilterNodes.Dropdown(b, "logbook:filter:mastery", toolbar.masteryFilterUI);
                    b.EndRow();
                }
                FilterNodes.Search(b, _cardSearch, toolbar.searchFilterUI);
            }
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            foreach (var selectable in cards.cardSelectableUIs)
            {
                var widget = selectable?.component;
                if (widget == null || !widget.gameObject.activeInHierarchy) continue;
                var hovered = selectable;
                System.Action hover = () => GameInput.Hover(hovered);
                var card = selectable.GetUISubComponent<CardUI>() ?? widget.GetComponentInParent<CardUI>();
                var slot = CardSlot(items.Count);
                if (card != null && card.GetCardState() != null)
                {
                    items.Add(new KeyValuePair<ControlId, NodeVtable>(slot, CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Always, hover)));
                    continue;
                }
                var locked = widget.GetComponent<LockedCardUI>() ?? widget.GetComponentInParent<LockedCardUI>();
                if (locked != null)
                    items.Add(new KeyValuePair<ControlId, NodeVtable>(slot, CardNodes.Locked(locked, hover)));
            }
            Row(b, GameText.Term("Compendium_Section_Cards"), items);
        }

        private const string CardSlotKey = "logbook:card:";

        private static ControlId CardSlot(int index) => ControlId.Structural(CardSlotKey + index);

        private static bool IsCardSlot(ControlId id) => id?.StructuralKey is string key && key.StartsWith(CardSlotKey, System.StringComparison.Ordinal);

        // Items as a row in a context, or nothing when there are none (a row cannot be empty).
        private static void Row(GraphBuilder b, string label, List<KeyValuePair<ControlId, NodeVtable>> items)
        {
            if (items.Count == 0) return;
            b.PushContext(label, Strings.RoleList);
            b.StartRow();
            foreach (var item in items) b.AddItem(item.Key, item.Value);
            b.EndRow();
            b.PopContext();
        }

        // ---- Artifacts ----

        private static void Artifacts(GraphBuilder b, CompendiumSectionBlessings artifacts)
        {
            if (artifacts.countLabel != null && artifacts.countLabel.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("logbook:artifacts:count"), GameNodes.Text(() => artifacts.countLabel.Content()));
            int c = 0;
            foreach (var collection in artifacts.collectionUIs)
            {
                if (collection == null || !collection.gameObject.activeInHierarchy) continue;
                int ci = c++;
                var items = new List<KeyValuePair<ControlId, NodeVtable>>();
                int r = 0;
                foreach (var relic in collection.relicUIs)
                {
                    if (relic == null || !relic.gameObject.activeInHierarchy) continue;
                    var artifact = relic;
                    items.Add(new KeyValuePair<ControlId, NodeVtable>(ControlId.Structural("logbook:artifact:" + ci + ":" + r++), new NodeVtable
                    {
                        ControlType = ControlTypes.Item,
                        Announcements = new List<NodeAnnouncement>
                        {
                            GameNodes.LabelPart(() => FirstTitle(artifact.Tooltips)),
                            GameNodes.TooltipPart(() => FirstBody(artifact.Tooltips)),
                        },
                        SearchText = () => FirstTitle(artifact.Tooltips),
                        OnFocus = () => GameInput.Hover(artifact.SelectableUI),
                        Details = () => GameNodes.Lines(TooltipReader.Lines(artifact)),
                    }));
                }
                Row(b, Title(collection.tooltipProvider), items);
            }
        }

        private static string Title(ITooltipProvider provider)
            => provider != null ? FirstTitle(provider.Tooltips) : null;

        private static string FirstTitle(List<TooltipContent> tooltips)
            => tooltips != null && tooltips.Count > 0 ? CardTextHelper.RemoveBold(tooltips[0].title) : null;

        private static string FirstBody(List<TooltipContent> tooltips)
            => tooltips != null && tooltips.Count > 0 ? CardTextHelper.ScrubUpgradeHighlights(tooltips[0].body) : null;
    }
}
