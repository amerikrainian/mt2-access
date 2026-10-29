using System.Collections;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.Module.Screens
{
    // A unit's ability: Enter on a unit whose ability is ready selects it the way the game's keyboard
    // play does (its own replay of an ability, HandUI.PlayAbility, up to the selection): keyboard mode,
    // the unit's floor, the train's view of its units, the unit, its ability. Targeting then takes over
    // as for a card; an ability that is not ready says where it stands. Keyboard mode first, as the
    // replay forces it: the train's view of its units opens only for keyboard or pad, and the game
    // leaves mouse mode on no key it does not hear (a nudged mouse puts it there for good).
    public sealed partial class BattleScreen
    {
        private static void UseAbility(CharacterState unit)
        {
            if (unit == null || unit.IsDestroyed || !unit.HasUnitAbility()) return;
            var hand = HandUi;
            if (hand == null) return;
            if (!Ready)
            {
                Speech.Say(Strings.StateDisabled, interrupt: true);
                return;
            }
            var availability = unit.GetUnitAbilityAvailability(Managers.GetCombatManager());
            if (availability != CharacterState.UnitAbilityAvailability.CanActivate)
            {
                Speech.Say(Nodes.UnitNodes.AbilityState(unit, availability), interrupt: true);
                return;
            }
            hand.StartCoroutine(SelectAbility(hand, unit));
        }

        private static IEnumerator SelectAbility(HandUI hand, CharacterState unit)
        {
            const float delay = 0.1f;
            var cards = hand.selectionBehaviour;
            var ability = hand.unitAbilitySelectionBehavior;
            var spawn = unit.GetSpawnPoint();
            int room = unit.GetCurrentRoomIndex();
            if (spawn == null || room < 0) yield break;
            InputManager.Inst.ForceInputDeviceMode(InputDeviceType.Keyboard);
            cards.UnFocusCard();
            ability.UnSelectCard();
            hand.roomAbilitySelectionBehavior.UnSelectCard();
            var rooms = Rooms;
            if (rooms.GetSelectedRoom() != room)
            {
                yield return rooms.GetRoomUI().SetSelectedRoom(room, fromPlayerInput: true);
                yield return ManualCoroutine.WaitForSecondsOrBreak(delay);
                if (rooms.GetSelectedRoom() != room) { Failed(unit, "the floor"); yield break; }
            }
            if (!cards.IsNavigatingTower())
            {
                Game?.MoveNavigationToTower();
                if (!cards.IsNavigatingTower()) { Failed(unit, "the train"); yield break; }
            }
            if (!hand.IsInViewCharactersMode())
            {
                hand.OnTowerSelected(cards.TowerSelectable, enter: true);
                yield return ManualCoroutine.WaitForSecondsOrBreak(delay);
                if (!hand.IsInViewCharactersMode()) { Failed(unit, "the units' view"); yield break; }
            }
            if (!cards.TryGetIncrToTarget(spawn, out int step)) { Failed(unit, "the unit"); yield break; }
            cards.MoveTargetWithKeyboard(step);
            ability.UpdateKeyboardNavSplinePointer(force: true);
            yield return ManualCoroutine.WaitForSecondsOrBreak(delay);
            if (!ability.IsCardFocusedOrSelected()) { Failed(unit, "the ability"); yield break; }
            if (!ability.IsCardSelected())
            {
                ability.HandleSubmit();
                yield return ManualCoroutine.WaitForSecondsOrBreak(delay);
                // Not selected: used at once (one that needs no target), or refused by the game, which
                // says why (not enough echoes): a refusal is logged only.
                if (!ability.IsCardSelected() && unit.GetUnitAbilityAvailability(Managers.GetCombatManager()) == CharacterState.UnitAbilityAvailability.CanActivate)
                    CoreLog.Info("battle: the game refused " + unit.GetName() + "'s ability");
            }
        }

        // ---- a floor's ability (a room upgrade's: Trap Door) ----

        /// <summary>The ability card a floor's upgrade grants, as the game's own panel builds it; null for
        /// an upgrade without one.</summary>
        private static CardState RoomAbility(TrainRoomAttachmentState attachment)
        {
            if (attachment == null || !attachment.HasRoomAbility) return null;
            foreach (var upgrade in attachment.UpgradeStates)
            {
                var ability = upgrade.Item2?.GetRoomAbilityUpgrade();
                var save = Managers.GetSaveManager();
                if (ability != null && save != null) return UnitOrRoomAbilityCardStateCache.Instance.Get(ability, save.RelicManager, save);
            }
            var effect = attachment.CardState != null && attachment.CardState.TryGetCardEffectState<CardEffectAttachTrainRoomAttachment>(out _, out var state) ? state : null;
            var data = effect?.GetParamCardUpgradeData()?.GetRoomAbilityUpgrade();
            var saves = Managers.GetSaveManager();
            return data != null && saves != null ? UnitOrRoomAbilityCardStateCache.Instance.Get(data, saves.RelicManager, saves) : null;
        }

        /// <summary>"Trap Door, ready, Send an enemy unit down a floor.": the floor's ability, its
        /// cooldown, what it does in the game's words; Enter uses it.</summary>
        private static NodeVtable RoomAbilityNode(int room, TrainRoomAttachmentState attachment, CardState ability)
        {
            System.Func<string> name = () => ability.GetTitle();
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new System.Collections.Generic.List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => attachment.CurrentAbilityCooldown > 0
                        ? Strings.Battle("ability.cooldown", attachment.CurrentAbilityCooldown)
                        : Strings.Battle("ability.ready"), kind: AnnouncementKinds.Value),
                    GameNodes.TooltipPart(() => ability.GetCardText(Managers.GetCardStatistics(), Managers.GetSaveManager())),
                },
                Details = () => GameNodes.Lines(TooltipUI.AppendUnitAbilityDescToStatusEffectBody(string.Empty, ability, null)),
                SearchText = name,
                OnActivate = () => UseRoomAbility(room),
            };
        }

        private static void UseRoomAbility(int room)
        {
            var hand = HandUi;
            if (hand == null) return;
            if (!Ready)
            {
                Speech.Say(Strings.StateDisabled, interrupt: true);
                return;
            }
            hand.StartCoroutine(SelectRoomAbility(hand, room));
        }

        // HandUI.PlayRoomAbility up to the selection: keyboard mode, the floor, the train's view, its
        // upgrade, its ability.
        private static IEnumerator SelectRoomAbility(HandUI hand, int room)
        {
            const float delay = 0.1f;
            var cards = hand.selectionBehaviour;
            var ability = hand.roomAbilitySelectionBehavior;
            InputManager.Inst.ForceInputDeviceMode(InputDeviceType.Keyboard);
            cards.UnFocusCard();
            hand.unitAbilitySelectionBehavior.UnSelectCard();
            ability.UnSelectCard();
            var rooms = Rooms;
            if (rooms.GetSelectedRoom() != room)
            {
                yield return rooms.GetRoomUI().SetSelectedRoom(room, fromPlayerInput: true);
                yield return ManualCoroutine.WaitForSecondsOrBreak(delay);
                if (rooms.GetSelectedRoom() != room) { Stopped("the floor"); yield break; }
            }
            if (!cards.IsNavigatingTower())
            {
                Game?.MoveNavigationToTower();
                if (!cards.IsNavigatingTower()) { Stopped("the train"); yield break; }
            }
            if (!hand.IsInViewCharactersMode())
            {
                hand.OnTowerSelected(cards.TowerSelectable, enter: true);
                yield return ManualCoroutine.WaitForSecondsOrBreak(delay);
                if (!hand.IsInViewCharactersMode()) { Stopped("the units' view"); yield break; }
            }
            if (!cards.RoomAttachmentIsActive()) { Stopped("the upgrade"); yield break; }
            var state = rooms.GetRoom(rooms.CurrentSelectedRoom);
            if (state != null) rooms.GetRoomUI().UpdateTrainRoomAttachmentDisplay(state);
            cards.SelectTrainRoomAttachment();
            yield return ManualCoroutine.WaitForSecondsOrBreak(delay);
            if (!ability.IsCardFocusedOrSelected()) { Stopped("the ability"); yield break; }
            if (!ability.IsCardSelected())
            {
                ability.HandleSubmit();
                yield return ManualCoroutine.WaitForSecondsOrBreak(delay);
                // The game refused it and said why (its cooldown): logged only.
                if (!ability.IsCardSelected()) CoreLog.Info("battle: the game refused floor " + (room + 1) + "'s ability");
            }
        }

        private static void Stopped(string step)
        {
            CoreLog.Warning("battle: selecting a floor's ability stopped at " + step);
            Speech.Say(Strings.StateDisabled, interrupt: true);
        }

        private static void Failed(CharacterState unit, string step)
        {
            CoreLog.Warning("battle: selecting " + unit.GetName() + "'s ability stopped at " + step);
            Speech.Say(Strings.StateDisabled, interrupt: true);
        }
    }
}
