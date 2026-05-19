using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation.Verbosity;

namespace MonsterTrainAccessibility.Presentation
{
    internal static class PresentationRenderer
    {
        public static Message Label(Presentation presentation)
        {
            return presentation?.Title;
        }

        public static Message FocusSummary(Presentation presentation, VerbosityProfile profile = null)
        {
            if (presentation == null)
            {
                return null;
            }

            profile = profile ?? VerbosityProfile.Default;
            List<Message> parts = new List<Message>();
            IReadOnlyList<VerbositySlotEntry> entries = profile.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                VerbositySlotEntry entry = entries[i];
                if (!entry.Enabled)
                {
                    continue;
                }

                switch (entry.Slot)
                {
                    case PresentationSlot.Title:
                        Add(parts, presentation.Title);
                        break;
                    case PresentationSlot.Subtitle:
                        Add(parts, presentation.Subtitle);
                        break;
                    case PresentationSlot.Cost:
                        Add(parts, presentation.Cost);
                        break;
                    case PresentationSlot.Stats:
                        AddRange(parts, presentation.Stats);
                        break;
                    case PresentationSlot.Description:
                        Add(parts, presentation.Description);
                        break;
                }
            }

            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        public static Message FocusTooltip(Presentation presentation, VerbosityProfile profile = null)
        {
            IReadOnlyList<Message> lines = BufferLines(presentation, profile);
            return lines.Count > 0 ? Message.JoinLines(lines) : null;
        }

        public static IReadOnlyList<Message> BufferLines(Presentation presentation, VerbosityProfile profile = null)
        {
            List<Message> lines = new List<Message>();
            if (presentation == null)
            {
                return lines;
            }

            profile = profile ?? VerbosityProfile.Default;
            IReadOnlyList<VerbositySlotEntry> entries = profile.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                VerbositySlotEntry entry = entries[i];
                if (!entry.Enabled)
                {
                    continue;
                }

                switch (entry.Slot)
                {
                    case PresentationSlot.Title:
                        Add(lines, presentation.Title);
                        break;
                    case PresentationSlot.Subtitle:
                        Add(lines, presentation.Subtitle);
                        break;
                    case PresentationSlot.Cost:
                        Add(lines, presentation.Cost);
                        break;
                    case PresentationSlot.Stats:
                        AddRange(lines, presentation.Stats);
                        break;
                    case PresentationSlot.Description:
                        Add(lines, presentation.Description);
                        break;
                    default:
                        AddSectionsForSlot(lines, presentation, entry.Slot, profile);
                        break;
                }
            }

            return lines;
        }

        private static void AddSectionsForSlot(
            List<Message> lines,
            Presentation presentation,
            PresentationSlot slot,
            VerbosityProfile profile)
        {
            SectionKind? kind = SlotToSectionKind(slot);
            if (kind == null)
            {
                return;
            }

            List<PresentationSection> sections = new List<PresentationSection>();
            for (int i = 0; i < presentation.Sections.Count; i++)
            {
                PresentationSection section = presentation.Sections[i];
                if (section != null && section.Kind == kind.Value)
                {
                    sections.Add(section);
                }
            }

            sections.Sort((left, right) => left.Order.CompareTo(right.Order));
            for (int i = 0; i < sections.Count; i++)
            {
                AddSection(lines, sections[i], profile);
            }
        }

        private static SectionKind? SlotToSectionKind(PresentationSlot slot)
        {
            switch (slot)
            {
                case PresentationSlot.Intent: return SectionKind.Intent;
                case PresentationSlot.DynamicInfo: return SectionKind.DynamicInfo;
                case PresentationSlot.Upgrade: return SectionKind.Upgrade;
                case PresentationSlot.Trigger: return SectionKind.Trigger;
                case PresentationSlot.Status: return SectionKind.Status;
                case PresentationSlot.Context: return SectionKind.Context;
                case PresentationSlot.Tooltip: return SectionKind.Tooltip;
                case PresentationSlot.NestedPresentation: return SectionKind.NestedPresentation;
                case PresentationSlot.Annotation: return SectionKind.Annotation;
                default: return null;
            }
        }

        private static void AddSection(List<Message> lines, PresentationSection section, VerbosityProfile profile)
        {
            if (section == null)
            {
                return;
            }

            if (section.NestedPresentation != null)
            {
                Add(lines, section.Title);
                AddRange(lines, BufferLines(section.NestedPresentation, profile));
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
                AddSection(lines, section.Children[i], profile);
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
