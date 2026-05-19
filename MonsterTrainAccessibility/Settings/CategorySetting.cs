using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.ModSettings
{
    internal sealed class CategorySetting : Setting
    {
        private readonly List<Setting> _children = new List<Setting>();

        public CategorySetting(string key, Message label)
            : base(key, label)
        {
        }

        public IReadOnlyList<Setting> Children => _children;
        public bool ReorderableChildren { get; set; }
        public Action<Setting, Setting> OnChildSwap { get; set; }
        public bool CanConfigure { get; set; } = true;

        public void Add(Setting child)
        {
            if (child != null)
            {
                _children.Add(child);
            }
        }
    }
}
