using System;

namespace MonsterTrainAccessibility.Localization
{
    internal static class AccessibilityLocalizationScope
    {
        [ThreadStatic]
        private static int _depth;

        public static bool IsActive => _depth > 0;

        public static T Run<T>(Func<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _depth++;
            try
            {
                return action();
            }
            finally
            {
                _depth--;
            }
        }

        public static void Run(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _depth++;
            try
            {
                action();
            }
            finally
            {
                _depth--;
            }
        }
    }
}
