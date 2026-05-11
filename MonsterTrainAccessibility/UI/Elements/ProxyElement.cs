using System.Text.RegularExpressions;
using MonsterTrainAccessibility.Localization;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    public abstract class ProxyElement : UIElement
    {
        private static readonly Regex CamelCasePattern = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);
        private static readonly Regex TagPattern = new Regex("<[^>]+>", RegexOptions.Compiled);

        protected GameObject Target { get; private set; }

        public string OverrideLabel { get; set; }

        public override bool IsVisible => Target != null && Target.activeInHierarchy;

        protected ProxyElement(GameObject target)
        {
            Target = target;
        }

        protected ProxyElement()
        {
            Target = null;
        }

        public void SetTarget(GameObject target)
        {
            Target = target;
        }

        public static string StripTags(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            return Message.NormalizeResolvedText(text);
        }

        protected static string CleanNodeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }
            return CamelCasePattern.Replace(name, "$1 $2");
        }
    }
}
