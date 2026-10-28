using System;
using MonsterTrainAccessibility.Dev;
using MonsterTrainAccessibility.Modularity;
using UnityEngine;

namespace MonsterTrainAccessibility.Host
{
    /// <summary>
    /// The one MonoBehaviour, in the host because a Unity component type must outlive every module
    /// generation (a module-defined component would pin its generation and dangle on a reload). Each
    /// frame it pumps the dev server's job queue and drives the current feature module's per-frame
    /// work. The module is read fresh from the loader each frame so a reload swaps it in with no
    /// further wiring.
    /// </summary>
    public sealed class HostPump : MonoBehaviour
    {
#if DEBUG
        internal static DevServer DevServer;
#endif
        internal static ModuleLoader Loader;

        private void Update()
        {
#if DEBUG
            try
            {
                DevServer?.Pump();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("DevServer.Pump: " + ex);
            }
#endif

            try
            {
                Loader?.Module?.Tick();
            }
            catch (Exception ex)
            {
                // Full exception (with stack); a per-frame Tick failure is otherwise unlocatable.
                Plugin.Log?.LogWarning("Module.Tick: " + ex);
            }
        }
    }
}
