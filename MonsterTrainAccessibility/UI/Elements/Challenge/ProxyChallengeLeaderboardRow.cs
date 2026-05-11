using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeLeaderboardRow : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly global::LeaderboardRow _row;
        private readonly GameUISelectableButton _infoButton;
        private readonly Func<bool> _activate;

        public ProxyChallengeLeaderboardRow(global::LeaderboardRow row, GameUISelectableButton infoButton, Func<bool> activate)
        {
            _row = row;
            _infoButton = infoButton;
            _activate = activate;
        }

        private global::ChallengeLeaderboardData.LeaderEntry Entry => _row?.GetEntryData();

        public override bool IsVisible => _row != null && _row.gameObject.activeInHierarchy && Entry != null;
        public override string GetTypeKey() => _infoButton != null && _infoButton.interactable ? "button" : null;

        public override Message GetLabel()
        {
            global::ChallengeLeaderboardData.LeaderEntry entry = Entry;
            if (entry == null)
            {
                return null;
            }

            bool streamerMode = entry.SaveManager?.GetPreferencesManager()?.StreamerModeEnabled ?? false;
            string time = LocalizationUtil.FormatTimeSpan(entry.GetRunTime());
            return Message.Localized("ui", "CHALLENGE.LEADERBOARD_ROW", new
            {
                rank = entry.GetRank(),
                name = entry.GetPlayerName(streamerMode),
                score = LocalizationUtil.FormatNumber(entry.GetScore()),
                time
            });
        }

        public override Message GetStatusString()
        {
            global::ChallengeLeaderboardData.LeaderEntry entry = Entry;
            if (entry == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            if (entry.GetVictory())
            {
                parts.Add(Message.Localized("ui", "CHALLENGE.VICTORY"));
            }
            if (entry.IsClean())
            {
                parts.Add(Message.Localized("ui", "CHALLENGE.CLEAN_RUN"));
            }
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        public override Message GetTooltip()
        {
            global::ChallengeLeaderboardData.LeaderEntry entry = Entry;
            if (entry == null)
            {
                return TooltipText.ForComponent(_infoButton != null ? _infoButton.transform : null);
            }

            List<Message> parts = new List<Message>
            {
                entry.IsClean()
                    ? Message.Localized("ui", "CHALLENGE.CLEAN_RUN_DETAIL")
                    : Message.Localized("ui", "CHALLENGE.RESTARTS_UNDOS", new
                    {
                        restarts = entry.GetNumBattleRestarts(),
                        undos = entry.GetNumUndos()
                    }),
                TooltipText.ForComponent(_infoButton != null ? _infoButton.transform : null)
            };
            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public void SelectForNavigation()
        {
            if (_infoButton != null && InputManager.Inst != null)
            {
                InputManager.Inst.SelectGameUIComponent(_infoButton, allowClearingSelection: false);
            }
        }

        public bool Activate() => _infoButton != null && _infoButton.interactable && _activate != null && _activate();
    }
}
