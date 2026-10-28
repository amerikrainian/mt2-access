using System;

namespace MonsterTrainAccessibility.Core
{
    /// <summary>
    /// The logging seam for Core: the module points these at the host logger in Load; tests and boot
    /// leave the no-op defaults. Core never references BepInEx or Unity, so this is the only way out.
    /// </summary>
    public static class CoreLog
    {
        public static Action<string> Info = _ => { };
        public static Action<string> Warning = _ => { };
        public static Action<string> Error = _ => { };
    }
}
