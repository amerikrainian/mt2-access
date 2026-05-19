namespace MonsterTrainAccessibility.Presentation.Verbosity
{
    internal readonly struct VerbositySlotEntry
    {
        public VerbositySlotEntry(PresentationSlot slot, bool enabled, bool verbose)
        {
            Slot = slot;
            Enabled = enabled;
            Verbose = verbose;
        }

        public PresentationSlot Slot { get; }
        public bool Enabled { get; }
        public bool Verbose { get; }
    }
}
