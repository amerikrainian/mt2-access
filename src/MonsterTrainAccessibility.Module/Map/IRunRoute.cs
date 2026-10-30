using System.Collections.Generic;

namespace MonsterTrainAccessibility.Module.Map
{
    /// <summary>How a route's current choices lie: side by side (the branches after a battle, Left and
    /// Right), or one after another (a ring's nodes, Up and Down).</summary>
    internal enum ChoiceLayout
    {
        Row,
        Column,
    }

    /// <summary>
    /// A run's route as the map screen reads it, live from the game on every call: what the player may
    /// choose right now, how those choices lie, and what follows any stop (for the route preview). A
    /// kind of run with a route of its own shape (Soul Savior's regions) is another implementation; the
    /// map screen knows only this.
    /// </summary>
    internal interface IRunRoute
    {
        /// <summary>The stops the player can act on now, in the route's order (a fork's branches left to
        /// right, a ring's stops as <see cref="Next"/> walks them); empty while the train is moving or a
        /// battle is starting.</summary>
        IReadOnlyList<MapStop> Choices();

        ChoiceLayout Layout { get; }

        /// <summary>Where the player stands when nothing is chosen: the stop the preview starts from.
        /// Null at the very start of a run.</summary>
        MapStop Origin();

        /// <summary>What follows a stop on the route (null = what follows the start of the current
        /// ring): several when the route forks.</summary>
        IReadOnlyList<MapStop> Next(MapStop from);

        /// <summary>Show a choice as the game shows one hovered (its branch lit, its tooltip up).</summary>
        void Preview(MapStop choice);

        /// <summary>Take a choice through the game: pick its branch, visit it, start its battle.</summary>
        void Choose(MapStop choice);
    }
}
