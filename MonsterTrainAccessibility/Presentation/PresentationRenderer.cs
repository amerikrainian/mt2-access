using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Presentation
{
    internal static class PresentationRenderer
    {
        public static Message Label(Presentation presentation)
        {
            return presentation?.Title;
        }

        public static Message FocusSummary(Presentation presentation)
        {
            if (presentation == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            Add(parts, presentation.Title);
            Add(parts, presentation.Subtitle);
            Add(parts, presentation.Cost);
            AddRange(parts, presentation.Stats);
            Add(parts, presentation.Description);
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        public static Message FocusTooltip(Presentation presentation)
        {
            IReadOnlyList<Message> lines = BufferLines(presentation);
            return lines.Count > 0 ? Message.JoinLines(lines) : null;
        }

        public static IReadOnlyList<Message> BufferLines(Presentation presentation)
        {
            List<Message> lines = new List<Message>();
            if (presentation == null)
            {
                return lines;
            }

            Add(lines, presentation.Title);
            Add(lines, presentation.Subtitle);
            Add(lines, presentation.Cost);
            AddRange(lines, presentation.Stats);
            Add(lines, presentation.Description);

            List<PresentationSection> ordered = new List<PresentationSection>(presentation.Sections);
            ordered.Sort(CompareSections);
            for (int i = 0; i < ordered.Count; i++)
            {
                AddSection(lines, ordered[i]);
            }

            return lines;
        }

        private static int CompareSections(PresentationSection left, PresentationSection right)
        {
            int priority = SectionPriority(left).CompareTo(SectionPriority(right));
            return priority != 0 ? priority : SectionOrder(left).CompareTo(SectionOrder(right));
        }

        private static int SectionOrder(PresentationSection section)
        {
            return section != null ? section.Order : int.MaxValue;
        }

        private static int SectionPriority(PresentationSection section)
        {
            if (section == null)
            {
                return int.MaxValue;
            }

            switch (section.Kind)
            {
                case SectionKind.Intent: return 10;
                case SectionKind.DynamicInfo: return 20;
                case SectionKind.Upgrade: return 30;
                case SectionKind.Trigger: return 40;
                case SectionKind.Status: return 50;
                case SectionKind.Context: return 55;
                case SectionKind.Tooltip: return 60;
                case SectionKind.NestedPresentation: return 70;
                case SectionKind.Annotation: return 80;
                default: return 100;
            }
        }

        private static void AddSection(List<Message> lines, PresentationSection section)
        {
            if (section == null)
            {
                return;
            }

            if (section.NestedPresentation != null)
            {
                Add(lines, section.Title);
                AddRange(lines, BufferLines(section.NestedPresentation));
            }
            else if (section.Title != null && section.Body != null)
            {
                Add(lines, Message.Join(": ", section.Title, section.Body));
            }
            else
            {
                Add(lines, section.Title);
                Add(lines, section.Body);
            }

            for (int i = 0; i < section.Children.Count; i++)
            {
                AddSection(lines, section.Children[i]);
            }
        }

        private static void AddRange(List<Message> target, IEnumerable<Message> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (Message message in source)
            {
                Add(target, message);
            }
        }

        private static void Add(List<Message> target, Message message)
        {
            if (message != null)
            {
                target.Add(message);
            }
        }
    }
}
