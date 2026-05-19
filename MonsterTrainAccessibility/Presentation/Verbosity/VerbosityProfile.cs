using System.Collections.Generic;

namespace MonsterTrainAccessibility.Presentation.Verbosity
{
    internal sealed class VerbosityProfile
    {
        private readonly IReadOnlyList<VerbositySlotEntry> _entries;
        private readonly Dictionary<PresentationSlot, VerbositySlotEntry> _bySlot;

        public VerbosityProfile(IReadOnlyList<VerbositySlotEntry> entries)
        {
            _entries = entries ?? new List<VerbositySlotEntry>();
            _bySlot = new Dictionary<PresentationSlot, VerbositySlotEntry>();
            for (int i = 0; i < _entries.Count; i++)
            {
                _bySlot[_entries[i].Slot] = _entries[i];
            }
        }

        public IReadOnlyList<VerbositySlotEntry> Entries => _entries;

        public bool IsEnabled(PresentationSlot slot)
        {
            return _bySlot.TryGetValue(slot, out VerbositySlotEntry entry) && entry.Enabled;
        }

        public bool IsVerbose(PresentationSlot slot)
        {
            return !_bySlot.TryGetValue(slot, out VerbositySlotEntry entry) || entry.Verbose;
        }

        public static VerbosityProfile Default { get; } = BuildDefault();

        private static VerbosityProfile BuildDefault()
        {
            List<VerbositySlotEntry> entries = new List<VerbositySlotEntry>();
            foreach (PresentationSlot slot in System.Enum.GetValues(typeof(PresentationSlot)))
            {
                entries.Add(new VerbositySlotEntry(slot, enabled: true, verbose: true));
            }

            return new VerbosityProfile(entries);
        }
    }
}
