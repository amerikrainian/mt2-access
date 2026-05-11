namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class BattleRoomNavigation : Elements.IRoomNavigationSource, Elements.ICreatureNavigationSource
    {
        private readonly BattleScreen _screen;

        public BattleRoomNavigation(BattleScreen screen)
        {
            _screen = screen;
        }

        public void SelectRoom(int roomIndex)
        {
            global::RoomUI roomUI = _screen?.RoomUI;
            if (roomUI == null)
            {
                return;
            }

            try
            {
                roomUI.StartCoroutine(roomUI.SetSelectedRoom(roomIndex, fromPlayerInput: true));
            }
            catch (System.Exception ex)
            {
                Core.Log.Info("[AccessibilityMod] Failed to select battle room " + roomIndex + ": " + ex);
            }
        }

        public void ViewCharacter(CharacterState character, int roomIndex)
        {
            SelectRoom(roomIndex);
            if (_screen == null || character == null || _screen.IsTargeting)
            {
                return;
            }

            global::HandUI handUI = _screen.HandUI;
            global::CardSelectionBehaviour selectionBehavior = _screen.SelectionBehaviour;
            if (handUI == null || selectionBehavior == null)
            {
                return;
            }

            try
            {
                _screen.RoomAbilitySelectionBehavior?.UnFocusCard(true);

                if (!selectionBehavior.IsNavigatingTower())
                {
                    _screen.ParkSelectionOnTower();
                }

                if (!handUI.IsInViewCharactersMode())
                {
                    handUI.OnTowerSelected(selectionBehavior.TowerSelectable, enter: true);
                }

                selectionBehavior.SetViewCharacter(character);
                selectionBehavior.UpdateKeyboardNavSplinePointer(force: true);
                FocusUnitAbilityForViewedCharacter(character);
            }
            catch (System.Exception ex)
            {
                Core.Log.Info("[AccessibilityMod] Failed to sync battle view character " + character.GetName() + ": " + ex);
            }
        }

        private void FocusUnitAbilityForViewedCharacter(CharacterState character)
        {
            global::UnitAbilitySelectionBehaviour unitAbility = _screen?.UnitAbilitySelectionBehavior;
            if (unitAbility == null || character == null || !character.HasUnitAbility())
            {
                return;
            }

            RoomState room = character.GetCurrentRoom();
            SpawnPoint spawnPoint = character.GetSpawnPoint();
            SpawnPointUI spawnPointUI = room != null && spawnPoint != null
                ? room.GetSpawnPointUIFromSpawnPoint(spawnPoint)
                : null;
            if (room == null || spawnPointUI == null)
            {
                return;
            }

            unitAbility.OnPointerEnter(room, null, spawnPointUI, null);
            unitAbility.UpdateKeyboardNavSplinePointer(force: true);
        }
    }
}
