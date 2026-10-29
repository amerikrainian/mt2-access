using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Combat;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;

namespace MonsterTrainAccessibility.Module.Screens
{
    // The battle's read keys, main's set: ember (R), forge points (Ctrl+R), the moon (P), what the
    // coming combat does to the focused unit (I), to the focused floor (Ctrl+I), to every floor
    // (Ctrl+Shift+I), the focused floor's room and upgrades (B), every floor's room (Shift+B). Its
    // jumps: the top panel (Shift+Up) and the hand (Shift+Down), each where it was last left; a floor
    // by its number (Ctrl+1 for Floor 1), said as its heading alone ("Floor 1, 3/5"), the floor
    // selected as arrowing there does. Shift and the game's key for the hand's nth card (a digit by
    // default) reads that card as a landing on it does, focus staying where it is. Coin (Ctrl+G) and
    // the pyre (Ctrl+H) are the run's, live on the map too.
    public sealed partial class BattleScreen
    {
        public const string KeyEmber = "battle.ember";
        public const string KeyForge = "battle.forge_points";
        public const string KeyMoon = "battle.moon";
        public const string KeyOutcome = "battle.outcome";
        public const string KeyFloorOutcomes = "battle.floor_outcomes";
        public const string KeyAllOutcomes = "battle.all_outcomes";
        public const string KeyCapacity = "battle.capacity";
        public const string KeyAllCapacity = "battle.all_capacity";
        public const string KeyHand = "battle.hand";
        public const string KeyTop = "battle.top";
        private const string CardPrefix = "battle.card.";
        private const string FloorPrefix = "battle.floor.";

        public static string KeyCard(int n) => CardPrefix + n;
        public static string KeyFloor(int n) => FloorPrefix + n;

        private static int Numbered(string key, string prefix)
            => key.StartsWith(prefix, System.StringComparison.Ordinal) && int.TryParse(key.Substring(prefix.Length), out int n) ? n : -1;

        // The floor under focus (its units' row, or the floor being targeted); -1 elsewhere.
        private int _focusFloor = -1;

        /// <summary>Whether a read key does anything now, for the key help.</summary>
        public static bool Offers(string key)
        {
            if (!(Core.Screens.ScreenManager.Current is BattleScreen screen)) return false;
            int card = Numbered(key, CardPrefix);
            if (card > 0) return Managers?.GetCardManager() != null && card <= Managers.GetCardManager().GetNumCardsInHand();
            int floor = Numbered(key, FloorPrefix);
            if (floor > 0) return Targeting == null && Rooms != null && floor <= Rooms.GetNumRooms();
            switch (key)
            {
                case KeyMoon: return Managers?.GetPlayerManager() != null && Managers.GetPlayerManager().GetHasMoonPhaseCardEffect();
                case KeyOutcome: return FocusedUnit() != null;
                case KeyFloorOutcomes:
                case KeyCapacity: return screen.CurrentFloor >= 0;
                case KeyHand:
                case KeyTop: return Targeting == null;
                default: return true;
            }
        }

        private int CurrentFloor => Targeting != null ? SelectedFloor : _focusFloor;

        public void Read(string key)
        {
            int card = Numbered(key, CardPrefix);
            if (card > 0)
            {
                ReadCard(card);
                return;
            }
            int floor = Numbered(key, FloorPrefix);
            if (floor > 0)
            {
                GoToFloor(floor - 1);
                return;
            }
            switch (key)
            {
                case KeyEmber:
                    Say(Strings.Battle("read.ember", Managers.GetPlayerManager()?.GetEnergy() ?? 0));
                    break;
                case KeyForge:
                    Say(Strings.Battle("read.forge_points", Managers.GetSaveManager()?.GetForgePoints() ?? 0));
                    break;
                case KeyMoon:
                    if (Offers(KeyMoon)) Say(Managers.GetPlayerManager().CurrentMoonPhase.LocalizedName());
                    break;
                case KeyOutcome:
                    var unit = FocusedUnit();
                    if (unit != null) Say(Outcomes.Of(unit) ?? Strings.Battle("outcome.no_change"));
                    break;
                case KeyFloorOutcomes:
                    if (CurrentFloor >= 0) Say(FloorOutcomes(CurrentFloor) ?? Strings.Battle("outcome.no_changes"));
                    break;
                case KeyAllOutcomes:
                    Say(AllOutcomes());
                    break;
                case KeyCapacity:
                    if (CurrentFloor >= 0) Say(CapacityDetails(CurrentFloor));
                    break;
                case KeyAllCapacity:
                    Say(AllCapacity());
                    break;
                case KeyHand:
                    if (Targeting == null && _rowLanding.TryGetValue("hand", out var hand)) Navigation.MoveTo(hand);
                    break;
                case KeyTop:
                    if (Targeting == null && _rowLanding.TryGetValue("top", out var top)) Navigation.MoveTo(top);
                    break;
            }
        }

        private static void Say(string text) => Speech.Say(text, interrupt: true);

        // The unit the focus is on, by the node's subject.
        private static CharacterState FocusedUnit() => Navigation.FocusedNode?.Vtable?.Subject?.Invoke() as CharacterState;

        // "Shield Steward, 3 health. Fel, dies": the floor's units the combat changes.
        private static string FloorOutcomes(int room)
        {
            var parts = new List<string>();
            foreach (var unit in Units(room))
            {
                string outcome = Outcomes.Of(unit);
                if (outcome != null) parts.Add(Strings.Battle("outcome.unit", unit.GetName(), outcome));
            }
            return parts.Count > 0 ? string.Join(". ", parts) : null;
        }

        private static string AllOutcomes()
        {
            var rooms = Rooms;
            var parts = new List<string>();
            if (rooms != null)
                for (int room = 0; room < rooms.GetNumRooms(); room++)
                {
                    string floor = FloorOutcomes(room);
                    if (floor != null) parts.Add(Strings.Battle("outcome.floor", FloorName(room), floor));
                }
            return parts.Count > 0 ? string.Join(". ", parts) : Strings.Battle("outcome.no_changes");
        }

        // B: "3/5, 2/4" (room and echoes), then the floor's upgrades: the game's own panel for the
        // selected floor's upgrade, its tooltips one after another; elsewhere their names.
        private static string CapacityDetails(int room)
        {
            var state = Rooms?.GetRoom(room);
            if (state == null) return null;
            var parts = new List<string> { CapacityWithEchoes(state) };
            var display = room == SelectedFloor ? Rooms.GetRoomUI()?.GetTrainRoomAttachmentDisplay() : null;
            var lines = display != null && display.gameObject.activeInHierarchy ? TooltipReader.Lines(display.tooltipProvider) : null;
            if (lines != null && lines.Count > 0) parts.AddRange(lines);
            else parts.AddRange(Attachments(state));
            return string.Join(". ", parts);
        }

        // Shift+B: "Floor 1 3/5, Floor 2 5/5, ...".
        private static string AllCapacity()
        {
            var rooms = Rooms;
            var parts = new List<string>();
            if (rooms != null)
                for (int room = 0; room < rooms.GetNumRooms(); room++)
                    parts.Add(Strings.Battle("floor_capacity", FloorName(room), CapacityWithEchoes(rooms.GetRoom(room))));
            return string.Join(", ", parts);
        }

        // The hand's nth card as a landing on it reads it, its row's heading aside; focus stays.
        private static void ReadCard(int n)
        {
            var cards = Managers.GetCardManager();
            if (cards == null || n > cards.GetNumCardsInHand()) return;
            var state = cards.GetHandCard(n - 1);
            var render = (Navigation.Active as Core.UI.GraphNavigator)?.CurrentRender;
            if (state == null || render == null || !render.Nodes.TryGetValue(Core.Graph.ControlId.Structural(CardKey(state)), out var node)) return;
            Say(Core.Graph.GraphAnnouncer.LeafText(node));
        }

        // To a floor as arrowing there lands (where it was last left, the floor selected), said as the
        // floor's heading alone: the unit landed on is not read. Already on the floor, it is only said.
        private void GoToFloor(int room)
        {
            if (!_rowLanding.TryGetValue("floor:" + room, out var landing)) return;
            Navigation.MoveTo(landing, announce: false);
            Say(FloorLabel(room));
        }

        /// <summary>Ctrl+G, anywhere in a run: "50 Coin".</summary>
        public static void ReadCoin()
        {
            var save = AllGameManagers.Instance?.GetSaveManager();
            if (save != null) Say(Strings.Battle("read.gold", save.GetGold(), GameText.Term("HudTooltip_Gold_Title")));
        }

        /// <summary>Ctrl+H, anywhere in a run: "50/50 hp".</summary>
        public static void ReadPyre()
        {
            var save = AllGameManagers.Instance?.GetSaveManager();
            if (save != null) Say(Strings.Battle("read.pyre", save.GetTowerHP(), save.GetMaxTowerHP()));
        }
    }
}
