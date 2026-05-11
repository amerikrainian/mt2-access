using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Presentation
{
    internal sealed class PresentationBuilder
    {
        private readonly HashSet<string> _seenSections = new HashSet<string>(System.StringComparer.Ordinal);
        private readonly Presentation _presentation = new Presentation();
        private int _nextSectionOrder;

        public void SetTitle(Message title)
        {
            if (title != null)
            {
                _presentation.Title = title;
            }
        }

        public void SetSubtitle(Message subtitle)
        {
            if (subtitle != null)
            {
                _presentation.Subtitle = subtitle;
            }
        }

        public void SetCost(Message cost)
        {
            if (cost != null)
            {
                _presentation.Cost = cost;
            }
        }

        public void SetDescription(Message description)
        {
            if (description != null)
            {
                _presentation.Description = description;
            }
        }

        public void AddStat(Message stat)
        {
            if (stat != null)
            {
                _presentation.Stats.Add(stat);
            }
        }

        public void AddSection(PresentationSection section)
        {
            if (section == null)
            {
                return;
            }

            string key = SectionKey(section);
            if (!string.IsNullOrWhiteSpace(key) && !_seenSections.Add(key))
            {
                return;
            }

            section.Order = _nextSectionOrder;
            _nextSectionOrder++;
            _presentation.Sections.Add(section);
        }

        public void AddSection(SectionKind kind, Message title, Message body, string dedupeKey = null)
        {
            AddSection(new PresentationSection
            {
                Kind = kind,
                Title = title,
                Body = body,
                DedupeKey = dedupeKey
            });
        }

        public void AddNestedPresentation(SectionKind kind, Presentation nested, Message title = null, string dedupeKey = null)
        {
            if (nested == null)
            {
                return;
            }

            AddSection(new PresentationSection
            {
                Kind = kind,
                Title = title,
                NestedPresentation = nested,
                DedupeKey = dedupeKey
            });
        }

        public Presentation Build()
        {
            return _presentation;
        }

        private static string SectionKey(PresentationSection section)
        {
            if (!string.IsNullOrWhiteSpace(section.DedupeKey))
            {
                return section.Kind + ":" + section.DedupeKey;
            }

            string title = section.Title?.Resolve();
            string body = section.Body?.Resolve();
            if (!Message.ShouldAdd(title) && !Message.ShouldAdd(body))
            {
                return null;
            }

            return section.Kind + ":" + (title ?? string.Empty) + "\n" + (body ?? string.Empty);
        }
    }
}
