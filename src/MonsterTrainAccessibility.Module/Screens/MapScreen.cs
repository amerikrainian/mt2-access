using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Map;
using MonsterTrainAccessibility.Module.Nodes;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The run's map (<see cref="global::MapScreen"/>, or Soul Savior's, the same map under its own
    /// name), read through the run's route
    /// (<see cref="IRunRoute"/>, live from the game):
    /// <list type="bullet">
    /// <item>what the player may choose now, as real controls: the branches after a battle side by side
    /// (Left and Right), the ring's nodes and its closing battle one after another (Up and Down), under
    /// "choice" when there are several; landing shows the choice as hovered, Enter takes it through the
    /// game;</item>
    /// <item>the route preview, a cursor of its own over what lies ahead: Ctrl+Up steps on from the
    /// choice in focus to what follows it (saying "choice" where the route forks), Ctrl+Left and
    /// Ctrl+Right walk a fork's siblings, Ctrl+Down steps back. Moving the real focus starts the preview
    /// over from there.</item>
    /// </list>
    /// Nothing is declared while the train moves or a battle starts. Letters do not search here.
    /// Escape is the game's: the pause menu.
    /// </summary>
    public sealed class MapScreen : Screen
    {
        public const string RouteForward = "map.forward";
        public const string RouteBack = "map.back";
        public const string RoutePrev = "map.prev";
        public const string RouteNext = "map.next";

        // The route preview's keys are the map's alone; the run keys (Tab's HUD) as on any run screen.
        private static readonly InputCategory[] Categories = { InputCategory.Map, InputCategory.Game, InputCategory.UI };

        // This render's choices by control id: the preview starts from the one in focus.
        private readonly Dictionary<ControlId, MapStop> _choices = new Dictionary<ControlId, MapStop>();
        private RouteCursor<MapStop> _preview;
        private string _previewFrom;

        public override string Key => "game.map";
        public override IReadOnlyList<InputCategory> InputCategories => Categories;
        public override bool AllowsTypeahead => false;

        private static global::MapScreen Game
        {
            get
            {
                var map = GameScreens.Active<global::MapScreen>(global::ScreenName.Map)
                    ?? GameScreens.Active<global::MapScreen>(global::ScreenName.SoulSaviorMap);
                return map != null && map.IsInteractable() ? map : null;
            }
        }

        private static IRunRoute Route => RunRoutes.For(Game);

        public override bool IsActive() => Route != null;

        // A map opened again starts its preview over: the cursor holds the stops it walked, whose widgets
        // a resumed run has replaced, and the same choice in focus would not tell it to.
        public override void OnPush()
        {
            _preview = null;
            _previewFrom = null;
        }

        public override void Build(GraphBuilder b)
        {
            _choices.Clear();
            var route = Route;
            if (route == null) return;
            var choices = route.Choices();
            if (choices.Count == 0) return;
            b.PushContext(choices.Count > 1 ? Strings.MapChoice : null);
            bool row = route.Layout == ChoiceLayout.Row;
            if (row) b.StartRow();
            // A ring's stops one after another read as the map draws them, bottom to top: the next stop
            // at the bottom, where focus starts, Up toward the battle. A fork is a row.
            var shown = new List<MapStop>(choices);
            if (!row) shown.Reverse();
            foreach (var stop in shown)
            {
                var id = ControlId.Structural("map:" + stop.Key);
                _choices[id] = stop;
                b.AddItem(id, MapNodes.Choice(stop, route, CurrentRing, () => _previewFrom = null));
            }
            if (row) b.EndRow();
            b.PopContext();
            if (!row) b.SetStart(ControlId.Structural("map:" + choices[0].Key));
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
            yield return new ElementAction(RouteForward, Strings.Get("bind." + RouteForward), _ => Preview(c => c.Forward()));
            yield return new ElementAction(RouteBack, Strings.Get("bind." + RouteBack), _ => Preview(c => c.Back()));
            yield return new ElementAction(RoutePrev, Strings.Get("bind." + RoutePrev), _ => Preview(c => c.Cycle(-1)));
            yield return new ElementAction(RouteNext, Strings.Get("bind." + RouteNext), _ => Preview(c => c.Cycle(1)));
        }

        private static int CurrentRing()
        {
            var map = Game;
            return map != null && map.saveManager != null ? map.saveManager.GetCurrentDistance() : -1;
        }

        // One preview step, from the choice in focus (or where the player stands when none is): a new
        // focus starts the preview over.
        private void Preview(System.Func<RouteCursor<MapStop>, RouteStep<MapStop>> step)
        {
            var route = Route;
            if (route == null) return;
            var focused = FocusedChoice();
            var from = focused ?? route.Origin();
            string fromKey = from != null ? from.Key : "start";
            if (_preview == null || _previewFrom != fromKey)
            {
                _preview = new RouteCursor<MapStop>(route.Next);
                // From the choice in focus, its fellow choices are the fork Ctrl+Left/Right walk.
                // Only a fork has fellow choices; a ring's stops one after another are not alternatives.
                var choices = focused != null && route.Layout == ChoiceLayout.Row ? route.Choices() : null;
                MapStop origin = null;
                if (choices != null)
                    foreach (var c in choices)
                        if (c.Key == focused.Key) origin = c;
                _preview.Reset(origin ?? from, origin != null ? choices : null);
                _previewFrom = fromKey;
            }
            var result = step(_preview);
            switch (result.Move)
            {
                case RouteMove.NoForward:
                    Core.Speech.Say(Strings.MapNoForward, interrupt: true);
                    return;
                case RouteMove.NoBackward:
                    Core.Speech.Say(Strings.MapNoBackward, interrupt: true);
                    return;
                // No other choice that way at this fork (or no fork: the choice itself): nothing to say.
                case RouteMove.Edge:
                    return;
            }
            if (result.Stop == null) return;
            string line = MapNodes.Describe(result.Stop, CurrentRing());
            if (result.Move == RouteMove.Moved && result.ArrivedAtFork) line = Strings.MapChoice + ", " + line;
            Core.Speech.Say(line, interrupt: true);
        }

        private MapStop FocusedChoice()
        {
            var id = (Core.UI.Navigation.Active as GraphNavigator)?.FocusedNodeId;
            return id != null && _choices.TryGetValue(id, out var stop) ? stop : null;
        }
    }
}
