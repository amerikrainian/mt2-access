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

        public bool IsShownInDetails(PresentationSlot slot)
        {
            return _bySlot.TryGetValue(slot, out VerbositySlotEntry entry) && entry.ShowInDetails;
        }

        public bool IsVerbose(PresentationSlot slot)
        {
            return !_bySlot.TryGetValue(slot, out VerbositySlotEntry entry) || entry.Verbose;
        }

        public bool IsInSummary(PresentationSlot slot)
        {
            return _bySlot.TryGetValue(slot, out VerbositySlotEntry entry) && entry.InSummary;
        }

        public static VerbosityProfile Default { get; } = BuildDefault();

        private static VerbosityProfile BuildDefault()
        {
            List<VerbositySlotEntry> entries = new List<VerbositySlotEntry>();
            foreach (PresentationSlot slot in System.Enum.GetValues(typeof(PresentationSlot)))
            {
                entries.Add(new VerbositySlotEntry(slot, showInDetails: true, verbose: true, inSummary: true));
            }

            return new VerbosityProfile(entries);
        }
    }
}
