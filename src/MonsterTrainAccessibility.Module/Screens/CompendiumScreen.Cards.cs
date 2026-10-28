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

        // The filters, the search (a Tab-stop of its own: an edit owns the arrows), then the page's
        // cards, a Tab-stop of their own.
        private void Cards(GraphBuilder b, CompendiumSectionCards cards)
        {
            var toolbar = cards.filterToolbar;
            if (toolbar != null)
            {
                FilterNodes.Options(b, "logbook:filter:clan", toolbar.classFilterUI);
                FilterNodes.Dropdown(b, "logbook:filter:type", toolbar.typeFilterUI);
                FilterNodes.Options(b, "logbook:filter:rarity", toolbar.rarityFilterUI);
                FilterNodes.Options(b, "logbook:filter:cost", toolbar.costFilterUI);
                FilterNodes.Dropdown(b, "logbook:filter:mastery", toolbar.masteryFilterUI);
                b.BeginStop("search");
                FilterNodes.Search(b, _cardSearch, toolbar.searchFilterUI);
                b.BeginStop("cards");
            }
            b.PushContext(GameText.Term("Compendium_Section_Cards"), Strings.RoleList);
            foreach (var selectable in cards.cardSelectableUIs)
            {
                var widget = selectable?.component;
                if (widget == null || !widget.gameObject.activeInHierarchy) continue;
                var hovered = selectable;
                System.Action hover = () => GameInput.Hover(hovered);
                var card = selectable.GetUISubComponent<CardUI>() ?? widget.GetComponentInParent<CardUI>();
                if (card != null && card.GetCardState() != null)
                {
                    b.AddItem(ControlId.Structural("logbook:card:" + card.GetCardState().GetCardDataID()), CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Always, hover));
                    continue;
                }
                var locked = widget.GetComponent<LockedCardUI>() ?? widget.GetComponentInParent<LockedCardUI>();
                if (locked != null)
                    b.AddItem(ControlId.Structural("logbook:locked:" + locked.GetInstanceID()), CardNodes.Locked(locked, hover));
            }
            b.PopContext();
        }

        // ---- Artifacts ----

        private static void Artifacts(GraphBuilder b, CompendiumSectionBlessings artifacts)
        {
            if (artifacts.countLabel != null && artifacts.countLabel.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("logbook:artifacts:count"), GameNodes.Text(() => artifacts.countLabel.text));
            int c = 0;
            foreach (var collection in artifacts.collectionUIs)
            {
                if (collection == null || !collection.gameObject.activeInHierarchy) continue;
                int ci = c++;
                b.PushContext(Title(collection.tooltipProvider), Strings.RoleList);
                int r = 0;
                foreach (var relic in collection.relicUIs)
                {
                    if (relic == null || !relic.gameObject.activeInHierarchy) continue;
                    var artifact = relic;
                    b.AddItem(ControlId.Structural("logbook:artifact:" + ci + ":" + r++), new NodeVtable
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
                    });
                }
                b.PopContext();
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
