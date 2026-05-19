using System;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.ModSettings
{
    internal sealed class ActionSetting : Setting
    {
        private readonly Func<bool> _activate;

        public ActionSetting(
            string key,
            Message label,
            Func<bool> activate,
            Message successMessage = null,
            bool rebuildScreenOnActivate = false)
            : base(key, label)
        {
            _activate = activate;
            SuccessMessage = successMessage;
            RebuildScreenOnActivate = rebuildScreenOnActivate;
        }

        public Message SuccessMessage { get; }
        public bool RebuildScreenOnActivate { get; }

        public bool Activate()
        {
            return _activate != null && _activate();
        }
    }
}
