using System;
using System.Reflection;
using BepInEx.Configuration;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal static class ElementSettingsRegistry
    {
        private static bool _initialized;
        private static ConfigFile _config;

        public static void Initialize(ConfigFile config)
        {
            _config = config;
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (_config == null)
            {
                Log.Warn("[AccessibilityMod] ElementSettingsRegistry initialized before config was available.");
                return;
            }

            _initialized = true;
            CategorySetting root = new CategorySetting("elements", Message.Localized("ui", "ELEMENT_SETTINGS.CATEGORY"));
            Type baseType = typeof(UIElement);
            Type attributeType = typeof(ElementSettingsAttribute);
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (!baseType.IsAssignableFrom(type) || type.IsAbstract)
                {
                    continue;
                }

                ElementSettingsAttribute attribute = type.GetCustomAttribute(attributeType) as ElementSettingsAttribute;
                if (attribute == null)
                {
                    continue;
                }

                CategorySetting category = new CategorySetting(attribute.Key, LabelFor(attribute));
                InvokeRegisterSettings(type, category);
                if (category.Children.Count > 0)
                {
                    root.Add(category);
                }
            }

            if (root.Children.Count > 0)
            {
                global::MonsterTrainAccessibility.ModSettings.ModSettings.Register(root);
            }
        }

        private static Message LabelFor(ElementSettingsAttribute attribute)
        {
            string labelKey = !string.IsNullOrWhiteSpace(attribute.LabelKey)
                ? attribute.LabelKey
                : "ELEMENT_SETTINGS." + NormalizeKey(attribute.Key);
            return Message.Localized("ui", labelKey);
        }

        private static void InvokeRegisterSettings(Type type, CategorySetting category)
        {
            MethodInfo method = type.GetMethod(
                "RegisterSettings",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                return;
            }

            ParameterInfo[] parameters = method.GetParameters();
            try
            {
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(CategorySetting))
                {
                    method.Invoke(null, new object[] { category });
                }
                else if (parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(CategorySetting) &&
                    parameters[1].ParameterType == typeof(ConfigFile))
                {
                    method.Invoke(null, new object[] { category, _config });
                }
                else
                {
                    Log.Warn("[AccessibilityMod] Ignoring invalid RegisterSettings signature on " + type.FullName);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("[AccessibilityMod] Element RegisterSettings failed for " + type.FullName + ": " + ex);
            }
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "UNKNOWN";
            }

            char[] chars = key.ToUpperInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}
