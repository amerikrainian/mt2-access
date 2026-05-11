using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonsterTrainAccessibility.Input
{
    public class InputAction
    {
        public string Key { get; }
        public string Label { get; }
        public string GameAction { get; }
        public bool BlocksGameInput { get; set; }
        private readonly List<InputBinding> _bindings = new List<InputBinding>();

        public IReadOnlyList<InputBinding> Bindings => _bindings;

        public event Action BindingsChanged;

        public InputAction(string key, string label, string gameAction = null)
        {
            Key = key;
            Label = label;
            GameAction = gameAction;
        }

        public InputAction AddBinding(InputBinding binding)
        {
            _bindings.Add(binding);
            BindingsChanged?.Invoke();
            return this;
        }

        public InputAction AddBinding(KeyCode keycode, bool ctrl = false, bool shift = false, bool alt = false)
        {
            _bindings.Add(new KeyboardBinding(keycode, ctrl, shift, alt));
            BindingsChanged?.Invoke();
            return this;
        }

        public InputAction AddBinding(ControllerInput input, ControllerInput? modifier = null)
        {
            _bindings.Add(new ControllerBinding(input, modifier));
            BindingsChanged?.Invoke();
            return this;
        }

        public void RemoveBinding(InputBinding binding)
        {
            _bindings.Remove(binding);
            BindingsChanged?.Invoke();
        }

        public void ClearBindings()
        {
            _bindings.Clear();
            BindingsChanged?.Invoke();
        }

        public bool MatchesKey(KeyCode keycode, bool ctrl, bool shift, bool alt)
            => _bindings.OfType<KeyboardBinding>().Any(b => b.Matches(keycode, ctrl, shift, alt));

        public bool UsesKey(KeyCode keycode) => _bindings.OfType<KeyboardBinding>().Any(b => b.Keycode == keycode);

        public bool MatchesControllerInput(ControllerInput input, Func<ControllerInput, bool> isHeld)
            => _bindings.OfType<ControllerBinding>().Any(b => b.Matches(input, isHeld));

        public bool UsesControllerInput(ControllerInput input)
            => _bindings.OfType<ControllerBinding>().Any(b => b.Uses(input));

        public bool HasControllerModifier => _bindings.OfType<ControllerBinding>().Any(b => b.Modifier != null);
    }
}
