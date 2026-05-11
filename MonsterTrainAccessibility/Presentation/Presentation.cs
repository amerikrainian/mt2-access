using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Presentation
{
    internal sealed class Presentation
    {
        public Message Title { get; set; }
        public Message Subtitle { get; set; }
        public Message Cost { get; set; }
        public List<Message> Stats { get; } = new List<Message>();
        public Message Description { get; set; }
        public List<PresentationSection> Sections { get; } = new List<PresentationSection>();
    }
}
