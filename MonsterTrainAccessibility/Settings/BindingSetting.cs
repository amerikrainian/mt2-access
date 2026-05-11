using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.ModSettings
{
    internal sealed class BindingSetting : Setting
    {
        private readonly ConfigEntry<string> _entry;
        private bool _loading;

        public BindingSetting(ConfigFile config, string section, InputAction action)
            : base(action?.Key, Message.Raw(action?.Label))
        {
            Action = action;
            string defaultValue = SerializeBindings(action);
            _entry = config.Bind(
                section,
                action.Key,
                defaultValue,
                new ConfigDescription("Accessibility mod key bindings for " + action.Label + "."));

            MigrateDefaultControllerBindings(action);
            Action.BindingsChanged += SaveIfChangedByUser;
            Load();
        }

        public InputAction Action { get; }

        public void ResetToDefault()
        {
            _entry.Value = (string)_entry.DefaultValue;
            Load();
        }

        private void Load()
        {
            _loading = true;
            try
            {
                Action.ClearBindings();
                foreach (InputBinding binding in ParseBindings(_entry.Value))
                {
                    Action.AddBinding(binding);
                }
            }
            finally
            {
                _loading = false;
            }
        }

        private void SaveIfChangedByUser()
        {
            if (_loading)
            {
                return;
            }

            _entry.Value = SerializeBindings(Action);
        }

        private static string SerializeBindings(InputAction action)
        {
            if (action == null || action.Bindings.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            List<InputBinding> bindings = InputBindingOrder.Ordered(action.Bindings);
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(';');
                }

                builder.Append(binding.Type).Append('=').Append(binding.Serialize());
            }

            return builder.ToString();
        }

        private void MigrateDefaultControllerBindings(InputAction action)
        {
            if (action == null ||
                string.IsNullOrWhiteSpace(_entry.Value) ||
                _entry.Value.Contains("controller="))
            {
                return;
            }

            string keyboardDefault = SerializeKeyboardBindings(action);
            string controllerDefault = SerializeControllerBindings(action);
            if (string.IsNullOrWhiteSpace(controllerDefault) || _entry.Value != keyboardDefault)
            {
                return;
            }

            _entry.Value = _entry.Value + ";" + controllerDefault;
        }

        private static string SerializeKeyboardBindings(InputAction action)
        {
            return SerializeBindingsOfType(action, "keyboard");
        }

        private static string SerializeControllerBindings(InputAction action)
        {
            return SerializeBindingsOfType(action, "controller");
        }

        private static string SerializeBindingsOfType(InputAction action, string type)
        {
            if (action == null || action.Bindings.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < action.Bindings.Count; i++)
            {
                InputBinding binding = action.Bindings[i];
                if (binding == null || binding.Type != type)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(';');
                }

                builder.Append(binding.Type).Append('=').Append(binding.Serialize());
            }

            return builder.ToString();
        }

        private static IEnumerable<InputBinding> ParseBindings(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            string[] parts = value.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                int equals = part.IndexOf('=');
                if (equals <= 0 || equals >= part.Length - 1)
                {
                    continue;
                }

                string type = part.Substring(0, equals);
                string serialized = part.Substring(equals + 1);
                InputBinding binding = InputBinding.Deserialize(type, serialized);
                if (binding != null)
                {
                    yield return binding;
                }
            }
        }
    }
}
