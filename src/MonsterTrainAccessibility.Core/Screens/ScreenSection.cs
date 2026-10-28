using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;

namespace MonsterTrainAccessibility.Core.Screens
{
    /// <summary>
    /// One self-contained part of a <see cref="CompositeScreen"/>: typically a Tab-stop (a list, a grid,
    /// a strip of controls) declared fresh on every render exactly like a screen's own Build, with its
    /// own state, screen-level actions, per-frame update and pop cleanup. A section that declares no
    /// nodes can still contribute behavior (a pending keyboard drag, a deferred focus landing). Sections
    /// share state only through objects handed to them explicitly by the screen that composes them.
    /// </summary>
    public abstract class ScreenSection
    {
        /// <summary>Declare this section's nodes (immediate mode: fresh from live state every render).</summary>
        public virtual void Build(GraphBuilder b) { }

        /// <summary>Screen-level actions this section answers, dispatched by id after the screen's own.</summary>
        public virtual IEnumerable<ElementAction> GetActions() { yield break; }

        /// <summary>Per-frame update while the composite screen is the active one.</summary>
        public virtual void OnUpdate() { }

        /// <summary>The composite screen left the stack: drop transient state.</summary>
        public virtual void OnPop() { }
    }
}
