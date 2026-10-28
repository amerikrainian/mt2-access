using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Input;

namespace MonsterTrainAccessibility.Core.UI
{
    /// <summary>A key the help lists: the action it stands for, what it does, the keys bound to it.</summary>
    public sealed class KeyHelpEntry
    {
        public string ActionKey;
        public string Label;
        public string Keys;
    }

    /// <summary>
    /// The keys that would do something right now, for the focused screen and the focused control:
    /// the context-sensitive key help. Nothing is declared for it. An action is listed when a key of
    /// it is live (its category active, its chord not shadowed) and one of these answers it:
    /// the navigator, asked without acting (<see cref="Navigator.WouldHandle"/>: an arrow with a way
    /// that way, Enter on a control that activates, and any key the focused screen offers through
    /// <see cref="Screens.Screen.GetActions"/>, which screens already offer only where it applies: the
    /// hex keys on a focused cell, reroll and freeze in the shop); else the action's own handler, when
    /// it has one and says it is <see cref="InputAction.IsAvailable"/> (a glance is not listed where
    /// its fact is not on screen).
    /// <para>In order, the most particular first: the screen's own keys; the handler keys that say
    /// when they apply (the glances); the navigation keys; the handler keys that always do (the
    /// buffers); the global ones. A key the screen offers reads by the label the screen gives it, which
    /// says what the key does HERE (Escape is "Pause menu" on the run, the Proceed button's own
    /// caption in the shop). Collected the
    /// moment the help opens, while the game screen still has the focus: once the help is up, the
    /// focused control is the help's own.</para>
    /// </summary>
    public static class KeyHelp
    {
        public static List<KeyHelpEntry> Collect(Func<InputAction, bool> skip = null)
            => Collect(InputManager.Actions, InputManager.IsLive, skip);

        public static List<KeyHelpEntry> Collect(IEnumerable<InputAction> actions, Func<InputAction, bool> isLive, Func<InputAction, bool> skip = null)
        {
            var screenKeys = new List<KeyHelpEntry>();
            var contextKeys = new List<KeyHelpEntry>();
            var navigationKeys = new List<KeyHelpEntry>();
            var generalKeys = new List<KeyHelpEntry>();
            var globalKeys = new List<KeyHelpEntry>();
            var offered = Offered();

            foreach (var action in actions)
            {
                if (skip != null && skip(action)) continue;
                if (isLive != null && !isLive(action)) continue;
                if (action.Category == InputCategory.UI && Navigation.WouldHandle(action))
                {
                    bool basic = action.Key.StartsWith("ui.", StringComparison.Ordinal);
                    // The screen's label for a key it offers says what the key does HERE.
                    string label = offered.TryGetValue(OfferId(action.Key), out var text) && !string.IsNullOrWhiteSpace(text)
                        ? text : action.DisplayLabel;
                    (basic ? navigationKeys : screenKeys).Add(Entry(action, label));
                }
                else if (action.HasHandler && action.IsAvailable)
                {
                    var list = action.Category != InputCategory.UI ? globalKeys : action.Available != null ? contextKeys : generalKeys;
                    list.Add(Entry(action, action.DisplayLabel));
                }
            }

            var all = new List<KeyHelpEntry>(screenKeys);
            all.AddRange(contextKeys);
            all.AddRange(navigationKeys);
            all.AddRange(generalKeys);
            all.AddRange(globalKeys);
            return all;
        }

        private static KeyHelpEntry Entry(InputAction action, string label)
            => new KeyHelpEntry { ActionKey = action.Key, Label = label, Keys = action.BindingsDisplay };

        // The navigator asks the screen for Escape by the standard id, for every other key by the key.
        private static string OfferId(string actionKey) => actionKey == UiActions.Back ? ActionIds.Back : actionKey;

        private static Dictionary<string, string> Offered()
        {
            var labels = new Dictionary<string, string>(StringComparer.Ordinal);
            var screen = Navigation.BoundScreen;
            if (screen == null) return labels;
            try
            {
                foreach (var a in screen.GetActions())
                    if (a != null && a.Id != null && !labels.ContainsKey(a.Id)) labels[a.Id] = a.Label;
            }
            catch (Exception e) { CoreLog.Warning("key help: the screen's actions failed: " + e.Message); }
            return labels;
        }
    }
}
