using System;

namespace MonsterTrainAccessibility.UI.Elements
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class ElementSettingsAttribute : Attribute
    {
        public ElementSettingsAttribute(string key, string labelKey = null)
        {
            Key = key;
            LabelKey = labelKey;
        }

        public string Key { get; }
        public string LabelKey { get; }
    }
}
