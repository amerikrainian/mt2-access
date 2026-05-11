using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class MinimapScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo SectionsField = AccessTools.Field(typeof(global::MinimapScreen), "sections")!;
        private static readonly FieldInfo LeftNodeMarkersField = AccessTools.Field(typeof(global::MinimapSection), "leftNodeMarkers")!;
        private static readonly FieldInfo RightNodeMarkersField = AccessTools.Field(typeof(global::MinimapSection), "rightNodeMarkers")!;
        private static readonly FieldInfo CenterNodeField = AccessTools.Field(typeof(global::MinimapSection), "centerNode")!;
        private static readonly FieldInfo PactNodeField = AccessTools.Field(typeof(global::MinimapSection), "pactNode")!;
        private static readonly FieldInfo BattleNodeField = AccessTools.Field(typeof(global::MinimapSection), "battleNode")!;
        private static readonly FieldInfo SectionNameLabelField = AccessTools.Field(typeof(global::MinimapSection), "sectionNameLabel")!;
        private static readonly FieldInfo SectionNumberLabelField = AccessTools.Field(typeof(global::MinimapSection), "sectionNumberLabel")!;
        private static readonly FieldInfo MarkerDataField = AccessTools.Field(typeof(global::MinimapNodeMarker), "mapNodeData")!;
        private static readonly FieldInfo BattleScenarioField = AccessTools.Field(typeof(global::MinimapBattleNode), "scenarioData")!;

        private readonly global::MinimapScreen _screen;
        private bool _allowFocusAnnouncements;

        public MinimapScreen(global::MinimapScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        public override void OnPush()
        {
            BuildRegistry();
            Log.Info("[AccessibilityMod] Screen pushed: " + GetType().Name + " (" + RootList.Children.Count + " elements, " + RootList.Children.Count + " targets)");
            _allowFocusAnnouncements = false;
        }

        public override bool ShouldAnnounceFocus(UIElement element) => _allowFocusAnnouncements;

        public override bool OnActionJustPressed(Input.InputAction action)
        {
            if (IsDirectionalNavigationAction(action?.Key))
            {
                _allowFocusAnnouncements = true;
            }

            return base.OnActionJustPressed(action);
        }

        private static bool IsDirectionalNavigationAction(string actionKey)
        {
            switch (actionKey)
            {
                case "ui_up":
                case "ui_down":
                case "ui_left":
                case "ui_right":
                    return true;
                default:
                    return false;
            }
        }

        protected override void PopulateList()
        {
            List<global::MinimapSection> sections = Get<List<global::MinimapSection>>(_screen, SectionsField);
            if (sections == null)
            {
                return;
            }

            for (int i = 0; i < sections.Count; i++)
            {
                AddSection(sections[i]);
            }
        }

        protected override string BuildSignature()
        {
            List<global::MinimapSection> sections = Get<List<global::MinimapSection>>(_screen, SectionsField);
            if (sections == null)
            {
                return "0";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < sections.Count; i++)
            {
                global::MinimapSection section = sections[i];
                sb.Append(section != null && section.gameObject.activeInHierarchy).Append(':');
                AppendNodeSignature(sb, Get<global::MinimapBattleNode>(section, BattleNodeField));
                AppendNodeSignature(sb, Get<global::MinimapNodeMarker>(section, CenterNodeField));
                AppendNodeSignature(sb, Get<global::MinimapNodeMarker>(section, PactNodeField));
                AppendMarkerListSignature(sb, Get<List<global::MinimapNodeMarker>>(section, LeftNodeMarkersField));
                AppendMarkerListSignature(sb, Get<List<global::MinimapNodeMarker>>(section, RightNodeMarkersField));
                sb.Append('|');
            }

            return sb.ToString();
        }

        private void AddSection(global::MinimapSection section)
        {
            if (section == null || !section.gameObject.activeInHierarchy)
            {
                return;
            }

            ListContainer group = new ListContainer(SectionLabel(section).Resolve(), NavigationAxis.Horizontal);
            AddBattleNode(group, Get<global::MinimapBattleNode>(section, BattleNodeField));
            AddMarker(group, Get<global::MinimapNodeMarker>(section, CenterNodeField));
            AddMarker(group, Get<global::MinimapNodeMarker>(section, PactNodeField));
            AddMarkerList(group, Get<List<global::MinimapNodeMarker>>(section, LeftNodeMarkersField));
            AddMarkerList(group, Get<List<global::MinimapNodeMarker>>(section, RightNodeMarkersField));

            if (group.Children.Count > 0)
            {
                AddElement(group, section.gameObject);
            }
            else
            {
                LabeledButton element = new LabeledButton(
                    section.gameObject,
                    () => SectionLabel(section),
                    () => section != null && section.gameObject.activeInHierarchy,
                    typeKey: null);
                AddElement(element, section.gameObject);
            }
        }

        private void AddMarkerList(ListContainer group, List<global::MinimapNodeMarker> markers)
        {
            if (markers == null)
            {
                return;
            }

            for (int i = 0; i < markers.Count; i++)
            {
                AddMarker(group, markers[i]);
            }
        }

        private void AddMarker(ListContainer group, global::MinimapNodeMarker marker)
        {
            if (group == null || marker == null || !marker.gameObject.activeInHierarchy)
            {
                return;
            }

            GameObject target = marker.SelectableUI != null ? marker.SelectableUI.gameObject : marker.gameObject;
            LabeledButton element = new LabeledButton(
                target,
                () => MarkerLabel(marker),
                () => marker != null && marker.gameObject.activeInHierarchy,
                () => MarkerTooltip(marker));
            group.Add(element);
            Register(element, marker.gameObject, marker.SelectableUI != null ? marker.SelectableUI.gameObject : null);
        }

        private void AddBattleNode(ListContainer group, global::MinimapBattleNode battle)
        {
            if (group == null || battle == null || !battle.gameObject.activeInHierarchy)
            {
                return;
            }

            GameObject target = battle.SelectableUI != null ? battle.SelectableUI.gameObject : battle.gameObject;
            LabeledButton element = new LabeledButton(
                target,
                () => BattleLabel(battle),
                () => battle != null && battle.gameObject.activeInHierarchy,
                () => BattleTooltip(battle));
            group.Add(element);
            Register(element, battle.gameObject, battle.SelectableUI != null ? battle.SelectableUI.gameObject : null);
        }

        internal static Message SectionLabel(global::MinimapSection section)
        {
            return Message.Localized("ui", "MINIMAP.SECTION", new
            {
                number = AccessibilityText.ReadLocalizedText(Get<TMPro.TMP_Text>(section, SectionNumberLabelField)),
                name = AccessibilityText.ReadLocalizedText(Get<TMPro.TMP_Text>(section, SectionNameLabelField))
            });
        }

        internal static Message MarkerLabel(global::MinimapNodeMarker marker)
        {
            MapNodeData data = Get<MapNodeData>(marker, MarkerDataField);
            return Message.RawCleaned(data != null ? data.GetTooltipTitle() : string.Empty);
        }

        internal static Message MarkerTooltip(global::MinimapNodeMarker marker)
        {
            MapNodeData data = Get<MapNodeData>(marker, MarkerDataField);
            return Message.RawCleaned(data != null ? data.GetTooltipBody() : string.Empty);
        }

        internal static Message BattleLabel(global::MinimapBattleNode battle)
        {
            ScenarioData scenario = Get<ScenarioData>(battle, BattleScenarioField);
            return scenario != null
                ? Message.RawCleaned(scenario.GetBattleName())
                : Message.Localized("ui", "MINIMAP.BATTLE");
        }

        internal static Message BattleTooltip(global::MinimapBattleNode battle)
        {
            ScenarioData scenario = Get<ScenarioData>(battle, BattleScenarioField);
            return Message.RawCleaned(scenario != null ? scenario.GetBattleDescription() : string.Empty);
        }

        private static void AppendMarkerListSignature(System.Text.StringBuilder sb, List<global::MinimapNodeMarker> markers)
        {
            if (markers == null)
            {
                return;
            }

            for (int i = 0; i < markers.Count; i++)
            {
                AppendNodeSignature(sb, markers[i]);
            }
        }

        private static void AppendNodeSignature(System.Text.StringBuilder sb, Component node)
        {
            sb.Append(node != null && node.gameObject.activeInHierarchy).Append(',');
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
