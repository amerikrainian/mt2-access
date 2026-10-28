using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;

namespace MonsterTrainAccessibility.Core.Screens
{
    /// <summary>
    /// A screen composed of <see cref="ScreenSection"/>s, built in the order they were added inside one
    /// optional context (<see cref="ContextLabel"/>, the screen's name): the shape of a HUD with many
    /// Tab-stops. Actions resolve the screen's own first (<see cref="OwnActions"/>), then each section's
    /// in order; update and pop fan out to every section. Sections that need each other get the shared
    /// object from the screen's constructor, never a reference to the screen.
    /// </summary>
    public abstract class CompositeScreen : Screen
    {
        private readonly List<ScreenSection> _sections = new List<ScreenSection>();

        /// <summary>The sections in build order.</summary>
        public IReadOnlyList<ScreenSection> Sections => _sections;

        /// <summary>Append a section (build order = add order). Returns it, for keeping a typed reference.</summary>
        protected T Add<T>(T section) where T : ScreenSection
        {
            _sections.Add(section);
            return section;
        }

        /// <summary>The label of the context every section's nodes sit in (announced on entry from
        /// outside); null for no wrapping context.</summary>
        protected virtual string ContextLabel => null;

        /// <summary>The screen's own actions, resolved before any section's.</summary>
        protected virtual IEnumerable<ElementAction> OwnActions() { yield break; }

        public override void Build(GraphBuilder b)
        {
            string label = ContextLabel;
            if (label != null) b.PushContext(label, null, positions: false);
            foreach (var section in _sections) section.Build(b);
            if (label != null) b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            foreach (var action in OwnActions()) yield return action;
            foreach (var section in _sections)
                foreach (var action in section.GetActions()) yield return action;
        }

        public override void OnUpdate()
        {
            foreach (var section in _sections) section.OnUpdate();
        }

        public override void OnPop()
        {
            foreach (var section in _sections) section.OnPop();
        }
    }
}
