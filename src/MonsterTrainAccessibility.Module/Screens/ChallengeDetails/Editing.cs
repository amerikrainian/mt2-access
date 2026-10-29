using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.Module.Screens
{
    public sealed partial class ChallengeDetailsScreen
    {
        // ---- a challenge being made (Create or Clone Challenge) ----

        // What a picker was opened from: focus goes back there when it closes.
        private ControlId _opener;

        /// <summary>
        /// The settings of a challenge being made: a row of each clan ("Primary Clan, button,
        /// Banished"), Enter opening the game's clan picker, then its Swap Champion with the champion
        /// in use, and the Pyre Heart with its stats, Enter opening its picker; the Covenant Rank on a
        /// line of its own, a slider stepped through the game's own arrows; a row of each mutator, Enter
        /// opening the mutator picker for its slot, then Add Mutators with how many are in out of the
        /// most allowed ("1/3", as the game counts them).
        /// </summary>
        private void Editable(GraphBuilder b, ChallengeDetailsUI details)
        {
            if (details == null) return;
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            Clan(items, "main", details.mainClassInfo);
            Clan(items, "sub", details.subClassInfo);
            var pyre = details.pyreHeartInfo;
            if (Visible(pyre) && Visible(pyre.editPyreHeartButton))
            {
                var vt = GameWidgets.Button(pyre.editPyreHeartButton, () => StartingConditionsNodes.HeaderOf(pyre), null,
                    new NodeAnnouncement(() => StartingConditionsNodes.PyreHeartValue(pyre), kind: AnnouncementKinds.Value));
                vt.Details = () => GameNodes.Lines(TooltipReader.Lines(pyre));
                items.Add(Opens("pyre", vt));
            }
            Row(b, null, items);
            // A slider takes Left and Right: a line of its own.
            var covenant = details.covenantSelection;
            if (Visible(details.covenantRoot) && covenant != null && covenant.covenantUI != null)
            {
                var item = Item("covenant", StartingConditionsNodes.CovenantSelector(covenant, () => HeaderText(details.covenantRoot.transform, "Text")));
                b.AddItem(item.Key, item.Value);
            }
            items = new List<KeyValuePair<ControlId, NodeVtable>>();
            var mutators = details.mutatorRoot;
            if (Visible(mutators))
            {
                int i = 0;
                foreach (var mutator in mutators.GetComponentsInChildren<MutatorButtonUI>(false))
                {
                    if (mutator.MutatorState == null || mutator.transform.parent != mutators.activeMutatorsLayout.transform) continue;
                    var shown = mutator;
                    var vt = StartingConditionsNodes.Mutator(shown);
                    vt.ControlType = ControlTypes.Button;
                    vt.OnActivate = () => GameInput.Submit(shown.Button);
                    items.Add(Opens("mutator:" + i++, vt));
                }
                var add = mutators.addMutatorButton;
                if (Visible(add))
                    items.Add(Opens("mutator:add", GameWidgets.Button(add, () => GameText.Term("ScreenRunSetup_AddMutatorsButton"), null,
                        new NodeAnnouncement(() => mutators.currentMutatorStates != null ? mutators.currentMutatorStates.Count + "/" + mutators.maxMutators : null,
                            kind: AnnouncementKinds.Value))));
            }
            // A slot cleared from its picker is gone: focus comes back to the slot now in its place, else
            // the row's last.
            if (_opener != null && _opener.StructuralKey is string opener && opener.StartsWith("challenge:mutator:") && items.Count > 0
                && !items.Exists(i => i.Key.Equals(_opener)))
                _opener = items[System.Math.Min(items.Count - 1, Slot(opener))].Key;
            Row(b, null, items);
        }

        private static int Slot(string key)
            => int.TryParse(key.Substring("challenge:mutator:".Length), out int slot) ? slot : int.MaxValue;

        // A clan and its Swap Champion. The swap says the champion it leaves in use.
        private void Clan(List<KeyValuePair<ControlId, NodeVtable>> items, string key, ClassLevelInfoUI clan)
        {
            if (!Visible(clan) || !Visible(clan.editClassButton)) return;
            items.Add(Opens("clan:" + key, GameWidgets.Button(clan.editClassButton, () => StartingConditionsNodes.HeaderOf(clan), null,
                new NodeAnnouncement(() => Shown(clan.titleLabel), kind: AnnouncementKinds.Value))));
            var swap = clan.swapChampionButton;
            if (Visible(swap))
                items.Add(Item("champion:" + key, GameWidgets.Button(swap, () => GameText.Term("ScreenMainMenu_SwapChampion"), null,
                    new NodeAnnouncement(() => Champion(clan), live: true, kind: AnnouncementKinds.Value))));
        }

        private static string Champion(ClassLevelInfoUI clan)
            => clan.ClassData != null ? clan.ClassData.GetChampionCard(clan.ChampionIndex)?.GetName() : null;

        // An item whose Enter opens a picker: focus comes back to it when the picker closes.
        private KeyValuePair<ControlId, NodeVtable> Opens(string key, NodeVtable vt)
        {
            var item = Item(key, vt);
            var open = vt.OnActivate;
            vt.OnActivate = () =>
            {
                _opener = item.Key;
                open?.Invoke();
            };
            return item;
        }

        // ---- the pickers ----

        /// <summary>The picker the game has open, if any, headed by what it picks for: the clans
        /// (<see cref="ClanNodes.DropdownOption"/>), the Pyre Hearts
        /// (<see cref="StartingConditionsNodes.PyreHeartDropdownOption"/>), or the mutators (Clear
        /// mutator first when a slot is being changed, then each,
        /// <see cref="StartingConditionsNodes.MutatorDropdownOption"/>). Focus lands on the choice in use.
        /// Enter picks through the game, which closes the picker; Escape closes it unchanged.</summary>
        private static bool Picker(GraphBuilder b, ChallengeDetailsUI details)
        {
            if (details == null) return false;
            var clans = details.classDropdown;
            if (clans != null && clans.gameObject.activeInHierarchy)
            {
                var editing = details.editingClassUI;
                b.PushContext(editing != null ? StartingConditionsNodes.HeaderOf(editing) : null, Strings.RoleList);
                if (clans.classButtons != null)
                    foreach (var option in clans.classButtons)
                    {
                        if (option == null || option.classData == null || !option.gameObject.activeInHierarchy) continue;
                        var id = ControlId.Structural("challenge:pick:clan:" + option.classData.GetID());
                        b.AddItem(id, ClanNodes.DropdownOption(option));
                        if (ClanNodes.DropdownChosen(option)) b.SetStart(id);
                    }
                b.PopContext();
                return true;
            }
            var hearts = details.pyreHeartDropdown;
            if (hearts != null && hearts.gameObject.activeInHierarchy)
            {
                b.PushContext(StartingConditionsNodes.HeaderOf(details.pyreHeartInfo), Strings.RoleList);
                if (hearts.pyreHeartButtons != null)
                    foreach (var option in hearts.pyreHeartButtons)
                    {
                        if (option == null || option.PyreHeartCharacterData == null || !option.gameObject.activeInHierarchy) continue;
                        var id = ControlId.Structural("challenge:pick:pyre:" + option.PyreHeartCharacterData.GetID());
                        b.AddItem(id, StartingConditionsNodes.PyreHeartDropdownOption(option));
                        if (option.PyreHeartCharacterData == hearts.chosenPyreHeart) b.SetStart(id);
                    }
                b.PopContext();
                return true;
            }
            var mutators = details.mutatorRoot;
            if (mutators != null && mutators.mutatorDropdownRoot != null && mutators.mutatorDropdownRoot.gameObject.activeInHierarchy)
            {
                b.PushContext(GameText.Term("ScreenRunSetup_CustomizeYourRun"), Strings.RoleList);
                var clear = mutators.clearMutatorButton;
                if (Visible(clear))
                    b.AddItem(ControlId.Structural("challenge:pick:mutator:clear"), GameWidgets.Button(clear,
                        () => PauseMenuScreen.Caption(clear) ?? GameText.Term("ScreenChallengeDetails_ClearMutator")));
                // The slot being changed holds this one: focus lands on it.
                var editing = mutators.editingMutatorIndex >= 0 && mutators.editingMutatorIndex < mutators.currentMutatorStates.Count
                    ? mutators.currentMutatorStates[mutators.editingMutatorIndex] : null;
                ControlId first = null;
                if (mutators.dropdownMutatorUIs != null)
                    foreach (var option in mutators.dropdownMutatorUIs)
                    {
                        if (option == null || option.MutatorState == null || !option.gameObject.activeInHierarchy) continue;
                        var id = ControlId.Structural("challenge:pick:mutator:" + option.MutatorState.GetRelicDataID());
                        b.AddItem(id, StartingConditionsNodes.MutatorDropdownOption(option));
                        if (editing != null && option.MutatorState.GetRelicDataID() == editing.GetRelicDataID()) b.SetStart(id);
                        else if (editing == null && first == null && !option.Chosen) b.SetStart(first = id);
                    }
                b.PopContext();
                return true;
            }
            return false;
        }

        // "Banished, Pyreborne, Proto Heartcage, Cheap Trick, Zoom, Enhance": what Randomize All rolled.
        private static string Rolled(ChallengeDetailsUI details)
        {
            if (details == null) return null;
            var parts = new List<string>
            {
                Visible(details.mainClassInfo) ? Shown(details.mainClassInfo.titleLabel) : null,
                Visible(details.subClassInfo) ? Shown(details.subClassInfo.titleLabel) : null,
                Visible(details.pyreHeartInfo) ? Shown(details.pyreHeartInfo.titleLabel) : null,
            };
            if (details.mutatorRoot != null && details.mutatorRoot.currentMutatorStates != null)
                foreach (var mutator in details.mutatorRoot.currentMutatorStates) parts.Add(mutator.GetName());
            return Join(", ", parts.ToArray());
        }
    }
}
