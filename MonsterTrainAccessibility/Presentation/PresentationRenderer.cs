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
                if (!entry.ShowInDetails || !entry.InSummary)
                {
                    continue;
                }

                AddSlot(parts, presentation, entry.Slot, profile, forSummary: true);
            }

            return parts.Count > 0 ? Message.Join(" ", parts) : null;
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
                if (!entry.ShowInDetails)
                {
                    continue;
                }

                AddSlot(lines, presentation, entry.Slot, profile, forSummary: false);
            }

            return lines;
        }

        private static void AddSlot(
            List<Message> target,
            Presentation presentation,
            PresentationSlot slot,
            VerbosityProfile profile,
            bool forSummary)
        {
            switch (slot)
            {
                case PresentationSlot.Title:
                    Add(target, presentation.Title);
                    break;
                case PresentationSlot.Subtitle:
                    Add(target, presentation.Subtitle);
                    break;
                case PresentationSlot.Cost:
                    Add(target, presentation.Cost);
                    break;
                case PresentationSlot.Stats:
                    AddRange(target, presentation.Stats);
                    break;
                case PresentationSlot.Description:
                    Add(target, presentation.Description);
                    break;
                default:
                    if (forSummary)
                    {
                        AddSectionsForSlotSummary(target, presentation, slot, profile);
                    }
                    else
                    {
                        AddSectionsForSlot(target, presentation, slot, profile);
                    }
                    break;
            }
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

        private static void AddSectionsForSlotSummary(
            List<Message> parts,
            Presentation presentation,
            PresentationSlot slot,
            VerbosityProfile profile)
        {
            SectionKind? kind = SlotToSectionKind(slot);
            if (kind == null)
            {
                return;
            }

            List<Message> sectionParts = new List<Message>();
            List<PresentationSection> sections = SectionsForKind(presentation, kind.Value);
            for (int i = 0; i < sections.Count; i++)
            {
                AddSectionSummary(sectionParts, sections[i], profile);
            }

            if (sectionParts.Count > 0)
            {
                Add(parts, Message.Join(" ", sectionParts));
            }
        }

        private static List<PresentationSection> SectionsForKind(Presentation presentation, SectionKind kind)
        {
            List<PresentationSection> sections = new List<PresentationSection>();
            for (int i = 0; i < presentation.Sections.Count; i++)
            {
                PresentationSection section = presentation.Sections[i];
                if (section != null && section.Kind == kind)
                {
                    sections.Add(section);
                }
            }

            sections.Sort((left, right) => left.Order.CompareTo(right.Order));
            return sections;
        }

        private static void AddSectionSummary(List<Message> parts, PresentationSection section, VerbosityProfile profile)
        {
            if (section == null)
            {
                return;
            }

            if (section.NestedPresentation != null)
            {
                Add(parts, FocusSummary(section.NestedPresentation, profile));
            }
            else if (section.Title != null && section.Body != null)
            {
                Add(parts, Message.Join(": ", section.Title, section.Body));
            }
            else
            {
                Add(parts, section.Body ?? section.Title);
            }

            for (int i = 0; i < section.Children.Count; i++)
            {
                AddSectionSummary(parts, section.Children[i], profile);
            }
        }

        private static SectionKind? SlotToSectionKind(PresentationSlot slot)
        {
            switch (slot)
            {
                case PresentationSlot.Ability: return SectionKind.Ability;
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
