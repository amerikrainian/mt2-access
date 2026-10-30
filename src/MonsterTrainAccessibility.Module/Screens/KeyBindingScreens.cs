using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Input;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    // The game declares its own InputManager in the global namespace.
    using InputManager = Core.Input.InputManager;

    /// <summary>Every key the mod binds, whatever the screen, as its categories ("Global keys",
    /// "Battle keys"...), each opening its actions (<see cref="CategoryKeysScreen"/>); Reset all keys
    /// ends the list. The keys that do something HERE are <see cref="HelpScreen"/>'s (F1).</summary>
    public sealed class KeyHelpScreen : Screen
    {
        public override string Key => "mod.keys";
        public override bool IsActive() => false; // only ever a child of the mod menu

        public override void Build(GraphBuilder b)
        {
            b.PushContext(Strings.ModKeyHelp, Strings.RoleList);
            foreach (InputCategory category in Enum.GetValues(typeof(InputCategory)))
            {
                var c = category;
                if (!InputManager.Actions.Any(a => a.Category == c)) continue;
                b.AddItem(ControlId.Structural("keys:" + c), GameNodes.Button(() => Strings.ModKeyCategory(c.ToString()),
                    () => PushChild(new CategoryKeysScreen(c))));
            }
            b.AddItem(ControlId.Structural("keys:resetall"), GameNodes.Button(() => Strings.KeysResetAll, AskResetAll, AnyChanged));
            b.PopContext();
        }

        private static bool AnyChanged()
        {
            foreach (var a in InputManager.Actions)
                if (KeyBindings.IsRebindable(a) && !KeyBindings.IsDefault(a)) return true;
            return false;
        }

        private static void AskResetAll()
        {
            // No first: focus lands on it.
            ChoiceSubmenuScreen.Open(Strings.KeysResetAllAsk, new[]
            {
                new ChoiceOption(Strings.KeysNo, null),
                new ChoiceOption(Strings.KeysYes, () =>
                {
                    KeyBindings.ResetAll();
                    Speech.Say(Strings.KeysResetAllDone, interrupt: true);
                }),
            });
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }

    /// <summary>One category's actions, "what it does: its keys", in the order they are registered.
    /// Enter on an action opens its keys and buttons (<see cref="ActionKeysScreen"/>).</summary>
    public sealed class CategoryKeysScreen : Screen
    {
        private readonly InputCategory _category;

        public CategoryKeysScreen(InputCategory category) => _category = category;

        public override string Key => "mod.categorykeys";
        public override bool IsActive() => false; // only ever a child of the key list

        public override void Build(GraphBuilder b)
        {
            b.PushContext(Strings.ModKeyCategory(_category.ToString()), Strings.RoleList, positions: false);
            foreach (var action in InputManager.Actions)
            {
                if (action.Category != _category) continue;
                var a = action;
                b.AddItem(ControlId.Structural("keys:" + a.Key), GameNodes.Button(() => Strings.HelpRow(a.DisplayLabel, a.BindingsDisplay),
                    () => PushChild(new ActionKeysScreen(a))));
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }

    /// <summary>
    /// One action's keys and buttons, a child of a category's list: each binding (Enter: Replace or
    /// Remove; one of the game's own keys is listed as set in the game's Bind Keys, and left alone),
    /// Add key, Add button, and Reset to default while the bindings differ from it. Changes save at once
    /// (<see cref="KeyBindings"/>). A protected action's last binding on a device cannot be removed
    /// (Replace still changes it).
    /// </summary>
    public sealed class ActionKeysScreen : Screen
    {
        private readonly InputAction _action;

        public ActionKeysScreen(InputAction action) => _action = action;

        public override string Key => "mod.actionkeys";
        public override bool IsActive() => false; // only ever a child of a category's list

        public override void Build(GraphBuilder b)
        {
            b.PushContext(_action.DisplayLabel, Strings.RoleList);
            if (_action.Bindings.Count == 0)
                b.AddItem(ControlId.Structural("actionkeys:none"), GameNodes.Text(() => Strings.KeysNone));
            foreach (var binding in _action.Bindings)
            {
                var bound = binding;
                var id = ControlId.Structural("actionkeys:" + bound.Type + ":" + bound.Serialize());
                if (bound.Fixed)
                    b.AddItem(id, GameNodes.Text(() => Strings.HelpRow(bound.DisplayName, Strings.KeysGame(GameText.Term("ScreenSettings_KeyMappings")))));
                else
                    b.AddItem(id, GameNodes.Button(() => bound.DisplayName, () => Choose(bound)));
            }
            b.AddItem(ControlId.Structural("actionkeys:addkey"), GameNodes.Button(() => Strings.KeysAddKey, () => PushChild(new BindKeyScreen(_action, null, controller: false))));
            b.AddItem(ControlId.Structural("actionkeys:addbutton"), GameNodes.Button(() => Strings.KeysAddButton, () => PushChild(new BindKeyScreen(_action, null, controller: true))));
            b.AddItem(ControlId.Structural("actionkeys:reset"), GameNodes.Button(
                () => Strings.HelpRow(Strings.KeysReset, KeyBindings.DefaultsDisplay(_action) ?? Strings.KeysNone), Reset, () => !KeyBindings.IsDefault(_action)));
            b.PopContext();
        }

        // Replace or Remove, for one binding.
        private void Choose(InputBinding binding)
        {
            var self = this;
            bool removable = KeyBindings.CanRemove(_action, binding);
            ChoiceSubmenuScreen.Open(binding.DisplayName, new[]
            {
                new ChoiceOption(Strings.KeysReplace, () => self.PushChild(new BindKeyScreen(_action, binding, binding.Device == "controller"))),
                new ChoiceOption(Strings.KeysRemove, () =>
                {
                    if (KeyBindings.Remove(_action, binding)) Speech.Say(Strings.KeysRemoved(binding.DisplayName), interrupt: true);
                }, enabled: removable, detail: removable ? null : Strings.KeysNeeded),
            });
        }

        private void Reset()
        {
            KeyBindings.Reset(_action);
            Speech.Say(Strings.KeysResetDone(_action.BindingsDisplay), interrupt: true);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }

    /// <summary>
    /// Waits for an action's new key or button: added, or in place of <c>replacing</c>. The mod stands
    /// down meanwhile (<see cref="CapturesRawInput"/>; focus mode keeps the game deaf), and the keyboard
    /// and pad are read directly. In phases: waiting until nothing is held, so the Enter (or A) that
    /// opened it is never taken; listening; settling until nothing is held again, so the new binding
    /// never reaches the mod as it closes.
    /// <list type="bullet">
    /// <item>A key: the first key that is not a modifier goes down; it, with the Ctrl, Shift and Alt
    /// held then, is the key. Escape alone cancels.</item>
    /// <item>A button: taken as it is let go, so a trigger can be held first: a button let go with a
    /// trigger held is the trigger and the button ("LT+A"); a trigger let go with no other button
    /// pressed meanwhile is the trigger alone. B alone, or Escape, cancels.</item>
    /// </list>
    /// A binding another action of the same category (or Global) has is refused, named, and the capture
    /// listens on; one only another category has is taken and that action named ("C is also Read
    /// coin"): the deeper category wins wherever both are live.
    /// </summary>
    public sealed class BindKeyScreen : Screen
    {
        private enum Phase { Waiting, Listening, Settling }

        private readonly InputAction _action;
        private readonly InputBinding _replacing;
        private readonly bool _controller;
        private Phase _phase = Phase.Waiting;
        // The pad controls pressed while listening: a trigger let go after another of them was a modifier.
        private readonly HashSet<ControllerInput> _pressed = new HashSet<ControllerInput>();

        public BindKeyScreen(InputAction action, InputBinding replacing, bool controller)
        {
            _action = action;
            _replacing = replacing;
            _controller = controller;
        }

        public override string Key => "mod.bindkey";
        public override bool IsActive() => false; // only ever a child of an action's keys
        public override bool CapturesRawInput => true;
        public override bool AllowsTypeahead => false;

        private string Prompt => _controller ? Strings.RebindButton(_action.DisplayLabel) : Strings.RebindKey(_action.DisplayLabel);

        public override void Build(GraphBuilder b)
        {
            b.PushContext(null);
            b.AddItem(ControlId.Structural("bindkey:prompt"), GameNodes.Text(() => Prompt));
            b.PopContext();
        }

        public override void OnUpdate()
        {
            try { Step(); }
            catch (Exception e)
            {
                CoreLog.Warning("BindKeyScreen: " + e.Message);
                Close();
            }
        }

        private static bool AnyHeld => RawKeys.AnyHeld || Gamepad.AnyHeld;

        private void Step()
        {
            switch (_phase)
            {
                case Phase.Waiting:
                    if (!AnyHeld) _phase = Phase.Listening;
                    return;
                case Phase.Listening:
                    var binding = _controller ? ListenButton() : ListenKey();
                    if (binding == null) return;
                    if (binding == Cancel)
                    {
                        Speech.Say(Strings.KeysCancelled, interrupt: true);
                        _phase = Phase.Settling;
                    }
                    else if (Take(binding)) _phase = Phase.Settling;
                    return;
                case Phase.Settling:
                    if (!AnyHeld) Close();
                    return;
            }
        }

        // What listening hands back to say "cancelled".
        private static readonly InputBinding Cancel = new ControllerBinding(ControllerInput.B);

        private InputBinding ListenKey()
        {
            var key = RawKeys.FirstPressed();
            if (key == null)
                return Gamepad.Pressed().Contains(ControllerInput.B) && !Gamepad.IsHeld(ControllerInput.LeftTrigger) && !Gamepad.IsHeld(ControllerInput.RightTrigger) ? Cancel : null;
            bool ctrl = RawKeys.CtrlHeld, shift = RawKeys.ShiftHeld, alt = RawKeys.AltHeld;
            if (key == KeyCode.Escape && !ctrl && !shift && !alt) return Cancel;
            return new KeyboardBinding(key.Value, ctrl, shift, alt);
        }

        private InputBinding ListenButton()
        {
            if (RawKeys.FirstPressed() == KeyCode.Escape) return Cancel;
            foreach (var down in Gamepad.Pressed()) _pressed.Add(down);
            foreach (var up in Gamepad.Released().ToList())
            {
                if (!_pressed.Contains(up)) continue;
                if (ControllerBinding.IsModifier(up))
                {
                    // A trigger held for another button was its modifier, not the binding.
                    if (_pressed.Any(p => !ControllerBinding.IsModifier(p))) continue;
                    return new ControllerBinding(up);
                }
                ControllerInput? modifier = Gamepad.IsHeld(ControllerInput.LeftTrigger) ? ControllerInput.LeftTrigger
                    : Gamepad.IsHeld(ControllerInput.RightTrigger) ? ControllerInput.RightTrigger : (ControllerInput?)null;
                if (up == ControllerInput.B && modifier == null) return Cancel;
                return new ControllerBinding(up, modifier);
            }
            return null;
        }

        // Applies the binding; false (listening on) when another action of its category has it.
        private bool Take(InputBinding binding)
        {
            string name = binding.DisplayName;
            var own = KeyBindings.Existing(_action, binding);
            if (own != null)
            {
                // The binding being replaced, pressed again: nothing changes.
                if (own == _replacing) { Speech.Say(Strings.KeysKept(name), interrupt: true); return true; }
                Speech.Say(Strings.KeysTaken(name, _action.DisplayLabel), interrupt: true);
                _pressed.Clear();
                return false;
            }
            var conflict = KeyBindings.Conflict(_action, binding);
            if (conflict != null)
            {
                Speech.Say(Strings.KeysTaken(name, conflict.DisplayLabel), interrupt: true);
                _pressed.Clear();
                return false;
            }
            if (_replacing != null) KeyBindings.Replace(_action, _replacing, binding);
            else KeyBindings.Add(_action, binding);
            var said = new List<string> { Strings.KeysBound(name) };
            foreach (var other in KeyBindings.Overlaps(_action, binding))
                said.Add(Strings.RebindConflict(name, other.DisplayLabel));
            Speech.Say(string.Join(". ", said), interrupt: true);
            return true;
        }

        private void Close() => ParentScreen?.RemoveChild(this);
    }
}
