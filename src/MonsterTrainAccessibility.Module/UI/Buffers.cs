using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Buffers;
using MonsterTrainAccessibility.Core.Events;
using Buffer = MonsterTrainAccessibility.Core.Buffers.Buffer;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// The mod's buffer roster and its review keys (Ctrl plus arrows), the Harkest Dungeon pattern:
    /// review lists for the information a focus announcement leaves out: the focused control's own
    /// lines (its head, then one line per part and tooltip: a unit's or a card's details), the game's
    /// event history and the units' chatter (<see cref="GameEvents"/>, newest first). Every buffer reads
    /// live on each keypress; a focus change re-homes review to the control's buffer and rewinds the
    /// control-fed ones, since a new control is new content. An empty buffer is skipped by the review
    /// keys; a log reviewed while events arrive keeps its cursor on the line it was on.
    /// </summary>
    internal static class Buffers
    {
        public static BufferManager Manager { get; private set; }
        public static BufferControls Controls { get; private set; }

        private static Buffer _ui;
        private static ControlId _homed;

        public static void Init()
        {
            Manager = new BufferManager();
            Controls = new BufferControls(Manager, (text, interrupt) => Speech.Say(text, interrupt: interrupt));

            _ui = Manager.Add(new Buffer(BufferKeys.Ui, () => Strings.BufferUi));
            _ui.SetSource(() => NodeLines.Lines(Navigation.FocusedNode));
            Log(BufferKeys.Events, () => Strings.BufferEvents, GameEvents.Events);
            Log(BufferKeys.Quotes, () => Strings.BufferQuotes, GameEvents.Quotes);
        }

        private static void Log(string key, System.Func<string> label, EventLog log)
        {
            var buffer = Manager.Add(new Buffer(key, label));
            buffer.SetSource(() => log.Lines);
            log.Added += buffer.LinesInserted;
        }

        /// <summary>Per frame: a focus change (by key or by the game moving things) re-homes review to
        /// the control's own buffer and rewinds the control-fed buffers.</summary>
        public static void Tick()
        {
            if (Manager == null) return;
            var id = Navigation.FocusedNode?.Id;
            if (Equals(id, _homed)) return;
            _homed = id;
            _ui.Reset();
            Manager.SetCurrent(BufferKeys.Ui);
        }
    }
}
