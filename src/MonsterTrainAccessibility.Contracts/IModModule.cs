using System;

namespace MonsterTrainAccessibility.Contracts
{
    /// <summary>
    /// The contract every reloadable feature module implements. The host loads Core + Module into one
    /// collectible load context, instantiates the one implementor, calls <see cref="Load"/> once, drives
    /// <see cref="Tick"/> from its permanent per-frame pump, and calls <see cref="IDisposable.Dispose"/>
    /// before dropping the context on reload. The module injects no IL2CPP types and owns no native
    /// handles, so it can be torn down and rebuilt without a restart. Dispose must undo every persistent
    /// game-side effect Load created (Harmony patches by per-load id, spawned objects, input suppression).
    /// </summary>
    public interface IModModule : IDisposable
    {
        /// <summary>Capture the host (logging, speech, settings) and wire up patches. Called once.</summary>
        void Load(IModHost host);

        /// <summary>Run the module's per-frame work. Called once per frame by the host pump.</summary>
        void Tick();
    }
}
