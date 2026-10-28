using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Mono.Cecil;
using MonsterTrainAccessibility.Contracts;

namespace MonsterTrainAccessibility.Modularity
{
    /// <summary>
    /// Loads and reloads the feature side without restarting the game. Unity's Mono cannot unload an
    /// assembly, so a reload is a NEW pair of assemblies rather than a replaced one: both reloadable
    /// DLLs (Core, then Module) are read into memory, renamed with Mono.Cecil to a per-generation
    /// identity ("MonsterTrainAccessibility.Core.g3"), Module's reference to Core rewritten to match, and
    /// loaded from those bytes (never from their paths) so the on-disk files stay unlocked and dotnet
    /// build can overwrite them while the game runs. Every other dependency (Contracts, Unity, the game,
    /// BepInEx, Harmony) resolves to its one already-loaded copy, which keeps the Contracts type identity
    /// stable across the boundary. Old generations stay loaded and idle after their Dispose: a leak that
    /// only development pays (a player's game loads generation 1 alone). The host never references Core
    /// at compile time, so the plain-named Core is never loaded.
    /// </summary>
    internal sealed class ModuleLoader
    {
        private const string CoreAssemblyName = "MonsterTrainAccessibility.Core";
        private const string ModuleAssemblyName = "MonsterTrainAccessibility.Module";

        private readonly string _corePath;
        private readonly string _modulePath;
        private readonly ModHost _host;
        private readonly ManualLogSource _log;

        // Every generation's assemblies by their renamed simple name, for the resolve fallback: Mono finds
        // a byte-loaded assembly by name on its own, and this answers any lookup it does not.
        private readonly Dictionary<string, Assembly> _generations = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        private int _attempt;

        public ModuleLoader(string corePath, string modulePath, ModHost host, ManualLogSource log)
        {
            _corePath = corePath;
            _modulePath = modulePath;
            _host = host;
            _log = log;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveGeneration;
        }

        /// <summary>The live module instance, or null if no load has succeeded. Read fresh each frame.</summary>
        public IModModule Module { get; private set; }

        /// <summary>How many loads have succeeded this process (1 = the boot load; each successful
        /// reload increments).</summary>
        public int Generation { get; private set; }

        /// <summary>The live generation's renamed Core and Module images, for the REPL to compile
        /// against (a byte-loaded assembly has no file for a metadata reference).</summary>
        public byte[] CoreImage { get; private set; }
        public byte[] ModuleImage { get; private set; }

        public string CorePath => _corePath;
        public string ModulePath => _modulePath;

        /// <summary>Load Core + Module from their current bytes. Returns false (and logs) on any failure.</summary>
        public bool Load()
        {
            try
            {
                if (!File.Exists(_corePath))
                {
                    _log.LogError("ModuleLoader: Core DLL not found at " + _corePath);
                    return false;
                }
                if (!File.Exists(_modulePath))
                {
                    _log.LogError("ModuleLoader: module DLL not found at " + _modulePath);
                    return false;
                }

                // A failed attempt still burns its suffix: its assemblies may be loaded already.
                _attempt++;
                string suffix = ".g" + _attempt;
                string coreName = CoreAssemblyName + suffix;
                string moduleName = ModuleAssemblyName + suffix;

                byte[] coreImage = Rename(File.ReadAllBytes(_corePath), coreName, null, null);
                byte[] moduleImage = Rename(File.ReadAllBytes(_modulePath), moduleName, CoreAssemblyName, coreName);

                Assembly core = Assembly.Load(coreImage);
                _generations[coreName] = core;
                Assembly asm = Assembly.Load(moduleImage);
                _generations[moduleName] = asm;

                Type type = asm.GetTypes().FirstOrDefault(
                    t => typeof(IModModule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                if (type == null)
                {
                    _log.LogError("ModuleLoader: no IModModule implementor in " + asm.GetName().Name);
                    return false;
                }

                var module = (IModModule)Activator.CreateInstance(type);
                module.Load(_host);

                // Swap in only once the new module is fully live, so a failed reload (locked / corrupt /
                // half-written DLL) leaves the running module untouched rather than tearing it down.
                // The old generation is told a successor owns the game state now, so its Dispose drops
                // its hooks without handing the keyboard back to the game.
                _host.SuccessorLoaded = true;
                try { DisposeCurrent(); }
                finally { _host.SuccessorLoaded = false; }
                Module = module;
                CoreImage = coreImage;
                ModuleImage = moduleImage;
                Generation++;
                _log.LogInfo("ModuleLoader: loaded " + type.FullName + " from " + moduleName + " (generation " + Generation + ")");
                return true;
            }
            catch (ReflectionTypeLoadException ex)
            {
                _log.LogError("ModuleLoader.Load failed: " + ex);
                foreach (Exception inner in ex.LoaderExceptions)
                    if (inner != null) _log.LogError("  loader: " + inner.Message);
                return false;
            }
            catch (Exception ex)
            {
                _log.LogError("ModuleLoader.Load failed: " + ex);
                return false;
            }
        }

        /// <summary>Tear down the current module (for shutdown).</summary>
        public void Unload() => DisposeCurrent();

        /// <summary>Rebuild from the current bytes, swapping the new module in only if it loads cleanly.</summary>
        public bool Reload() => Load();

        private void DisposeCurrent()
        {
            if (Module == null)
                return;
            try { Module.Dispose(); }
            catch (Exception ex) { _log.LogError("ModuleLoader: module Dispose threw: " + ex); }
            Module = null;
        }

        // The assembly renamed to newName; with fromRef/toRef, its reference to fromRef points at toRef.
        private static byte[] Rename(byte[] image, string newName, string fromRef, string toRef)
        {
            using (var input = new MemoryStream(image))
            using (AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(input, new ReaderParameters { ReadingMode = ReadingMode.Immediate }))
            {
                asm.Name.Name = newName;
                asm.MainModule.Name = newName + ".dll";
                if (fromRef != null)
                {
                    foreach (AssemblyNameReference reference in asm.MainModule.AssemblyReferences)
                        if (reference.Name == fromRef)
                            reference.Name = toRef;
                }
                using (var output = new MemoryStream())
                {
                    asm.Write(output);
                    return output.ToArray();
                }
            }
        }

        private Assembly ResolveGeneration(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            return name != null && _generations.TryGetValue(name, out Assembly asm) ? asm : null;
        }
    }
}
