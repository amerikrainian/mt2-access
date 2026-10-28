using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Buffers;
using Buffer = MonsterTrainAccessibility.Core.Buffers.Buffer;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// The mod's buffer roster and its review keys (Ctrl plus arrows), the Harkest Dungeon pattern:
    /// review lists for the information a focus announcement leaves out. So far the focused control's
    /// own lines (its head, then one line per tooltip); the game's buffers (the card, the unit, the
    /// floor, the combat log) join as their screens are ported. Every buffer reads live on each
    /// keypress; a focus change re-homes review to the control's buffer and rewinds the control-fed
    /// ones, since a new control is new content. An empty buffer is skipped by the review keys.
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
