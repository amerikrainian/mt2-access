using System;

namespace MonsterTrainAccessibility.Presentation.Verbosity
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class VerbosityProfileAttribute : Attribute
    {
        public VerbosityProfileAttribute(string key, params PresentationSlot[] defaultOrder)
        {
            Key = key;
            DefaultOrder = defaultOrder ?? Array.Empty<PresentationSlot>();
        }

        public string Key { get; }
        public PresentationSlot[] DefaultOrder { get; }
    }
}
