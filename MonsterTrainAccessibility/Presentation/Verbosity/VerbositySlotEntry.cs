namespace MonsterTrainAccessibility.Presentation.Verbosity
{
    internal readonly struct VerbositySlotEntry
    {
        public VerbositySlotEntry(PresentationSlot slot, bool showInDetails, bool verbose, bool inSummary)
        {
            Slot = slot;
            ShowInDetails = showInDetails;
            Verbose = verbose;
            InSummary = inSummary;
        }

        public PresentationSlot Slot { get; }
        public bool ShowInDetails { get; }
        public bool Verbose { get; }
        public bool InSummary { get; }
    }
}
