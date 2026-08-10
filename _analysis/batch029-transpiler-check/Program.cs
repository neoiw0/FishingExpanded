using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;

namespace Batch029TranspilerCheck
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("usage: Batch029TranspilerCheck <FishingExpanded.dll>");
                return 2;
            }

            string dllPath = Path.GetFullPath(args[0]);
            BootstrapModEntry(dllPath);
            bool patchAllMode = args.Length > 1 && args[1] == "--patchall";
            AppDomain.CurrentDomain.AssemblyResolve += ResolveGameAssemblies;
            try
            {
                Assembly fe = Assembly.LoadFrom(dllPath);

                if (patchAllMode)
                {
                    var all = new Harmony("batch029-patchall");
                    all.PatchAll(fe);
                    Console.WriteLine("PASS: PatchAll completed without exception (mod init equivalent)");
                    return 0;
                }

                Type patchType = fe.GetType("FishingExpanded.Patches.BobberBarPatches");
                if (patchType == null) { Console.WriteLine("FAIL: BobberBarPatches type not found in " + dllPath); return 1; }

                MethodInfo transpiler = patchType.GetMethod("Update_Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
                if (transpiler == null) { Console.WriteLine("FAIL: Update_Transpiler not found"); return 1; }

                MethodInfo original = typeof(BobberBar).GetMethod("update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (original == null) { Console.WriteLine("FAIL: BobberBar.update not found"); return 1; }

                var harmony = new Harmony("batch029-check");
                MethodInfo replacement = harmony.Patch(original, null, null, new HarmonyMethod(transpiler), null);

                try
                {
                    // Force JIT of the patched method exactly like MonoMod's PrepareMethod pin:
                    // invalid injected IL throws InvalidProgramException here.
                    replacement.Invoke(null, new object[] { null, null });
                    Console.WriteLine("UNEXPECTED: invoke succeeded without exception");
                    return 1;
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException is InvalidProgramException)
                    {
                        Console.WriteLine("FAIL: InvalidProgramException at JIT - " + ex.InnerException.Message);
                        return 1;
                    }
                    // Valid IL: null receiver produces NRE before the body runs.
                    Console.WriteLine("PASS: patched BobberBar.update JIT-compiles (IL valid); inner=" + ex.InnerException.GetType().Name);
                    return 0;
                }
                catch (InvalidProgramException ex)
                {
                    Console.WriteLine("FAIL: InvalidProgramException - " + ex.Message);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: " + ex.GetType().FullName + ": " + ex.Message);
                Exception inner = ex.InnerException;
                while (inner != null) { Console.WriteLine("  inner: " + inner.GetType().FullName + ": " + inner.Message); inner = inner.InnerException; }
                return 1;
            }
        }

        /// <summary>在无 SMAPI 宿主环境下为 ModEntry 注入空安全 Monitor 桩，保证 Transpiler 内的日志调用不 NRE。</summary>
        private static void BootstrapModEntry(string dllPath)
        {
            try
            {
                var fe = Assembly.LoadFrom(dllPath);
                Type modEntryType = fe.GetType("FishingExpanded.ModEntry");
                object entry = Activator.CreateInstance(modEntryType);
                modEntryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, entry);
                var monitorProp = typeof(StardewModdingAPI.Mod).GetProperty("Monitor", BindingFlags.Public | BindingFlags.Instance);
                monitorProp?.SetValue(entry, DispatchProxy.Create<IMonitor, NoopProxy>());
            }
            catch (Exception ex)
            {
                Console.WriteLine("WARN: ModEntry bootstrap failed (" + ex.GetType().Name + ": " + ex.Message + ")");
            }
        }

        private class NoopProxy : DispatchProxy
        {
            public NoopProxy() { }
            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.ReturnType == typeof(bool)) return false;
                if (targetMethod.ReturnType == typeof(int)) return 0;
                return null;
            }
        }

        private static Assembly ResolveGameAssemblies(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name + ".dll";
            string[] dirs =
            {
                @"D:\GGGGG\K1515",
                @"D:\GGGGG\K1515\smapi-internal",
                @"D:\GGGGG\K1515\Mods\FishingExpanded"
            };
            foreach (string dir in dirs)
            {
                string path = Path.Combine(dir, name);
                if (File.Exists(path))
                    return Assembly.LoadFrom(path);
            }
            return null;
        }
    }
}


