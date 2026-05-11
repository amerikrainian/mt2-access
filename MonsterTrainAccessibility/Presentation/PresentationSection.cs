using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Presentation
{
    internal sealed class PresentationSection
    {
        public SectionKind Kind { get; set; }
        public Message Title { get; set; }
        public Message Body { get; set; }
        public string DedupeKey { get; set; }
        public int Order { get; set; }
        public List<PresentationSection> Children { get; } = new List<PresentationSection>();
        public Presentation NestedPresentation { get; set; }
    }
}
