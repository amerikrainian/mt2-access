using System;
using System.Linq;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Events;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Module.Interop;
using ShinyShoe;

namespace MonsterTrainAccessibility.Module.Combat
{
    /// <summary>
    /// The Punished's Monolith (<see cref="MonolithUI"/>, on the run's HUD while the game shows it: a
    /// Punished run, on the map, in a battle and the screens between). Its state is the save's Monolith
    /// Level and Devotion, said in the game's own words ("Monolith Level 2", "Devotion: 3/10"). In a
    /// battle its button offers tribute, pressed through the game (the Hud's handler for it, as a click),
    /// so the game's refusals ("No cards to offer.") are its own error line. What a tribute does is told
    /// as it happens: the ember (<see cref="CombatEvents"/>), Devotion and the level (<see cref="RunEvents"/>),
    /// and the card the game discarded at random, which it shows only as an animation.
    /// </summary>
    internal static class Monolith
    {
        /// <summary>The Monolith while the game shows it, else null.</summary>
        public static MonolithUI Widget
        {
            get
            {
                var hud = GameScreens.Active<global::Hud>(global::ScreenName.Hud);
                var widget = hud != null ? hud.monolithUI : null;
                return widget != null && widget.gameObject.activeInHierarchy ? widget : null;
            }
        }

        private static SaveManager Save => AllGameManagers.Instance != null ? AllGameManagers.Instance.GetSaveManager() : null;

        /// <summary>"Monolith Level 2".</summary>
        public static string Level()
        {
            var save = Save;
            string format = GameText.Term("HudTooltip_Monolith_Level");
            return save == null || format == null ? null : string.Format(format, LocalizationUtil.FormatNumber(save.GetDevotionLevel()));
        }

        /// <summary>"Devotion: 3/10", toward the next level.</summary>
        public static string Points()
        {
            var save = Save;
            string format = GameText.Term("HudTooltip_Monolith_Points");
            if (save == null || format == null) return null;
            int threshold = MonolithHelper.GetDevotionThreshold(save.GetAllGameData(), save.GetDevotionLevel());
            return string.Format(format, LocalizationUtil.FormatNumber(save.GetDevotionPoints()), LocalizationUtil.FormatNumber(threshold));
        }

        /// <summary>Whether the Monolith is the battle's button now (the game swaps it in for the
        /// tooltip-only stand-in during a battle).</summary>
        public static bool IsButton(MonolithUI widget)
        {
            var save = Save;
            return widget != null && save != null && widget.devoteButton != null
                && ReferenceEquals(widget.GetLiveSelectable(save), widget.devoteButton);
        }

        /// <summary>Whether a tribute press reaches the game now: the button is live and not locked
        /// (the game locks it outside the player's turn). A press it refuses is still answered.</summary>
        public static bool CanPress()
        {
            var widget = Widget;
            return IsButton(widget) && widget.devoteButton.state != GameUISelectableButton.State.Locked;
        }

        public static void Devote()
        {
            var widget = Widget;
            if (IsButton(widget)) GameInput.Submit(widget.devoteButton);
        }

        public static void Read()
        {
            if (Widget == null) return;
            string level = Level(), points = Points();
            if (level != null || points != null) Speech.Say(string.Join(", ", new[] { level, points }.Where(s => s != null)), interrupt: true);
        }

        /// <summary>The card a tribute discards, picked at random by the game and shown only as it flies
        /// to the discard pile. <c>MonolithHelper.Devote</c> discards it (and whatever the discard
        /// triggers) while <c>IsDevoteInProgress</c>.</summary>
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.DiscardCard))]
        private static class Discarded
        {
            private static void Prefix(CardManager.DiscardCardParams discardCardParams)
            {
                try
                {
                    if (!MonolithHelper.IsDevoteInProgress || discardCardParams == null || discardCardParams.wasPlayed) return;
                    var card = discardCardParams.discardCard;
                    var save = Save;
                    if (card == null || save == null || save.PreviewMode) return;
                    GameEvents.Raise(EventKinds.Monolith, Strings.Event("discarded", card.GetTitle()), EventSide.Friendly);
                }
                catch (Exception e) { CoreLog.Warning("monolith discard: " + e.Message); }
            }
        }
    }
}
