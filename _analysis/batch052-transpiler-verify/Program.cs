using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley.Menus;

namespace TranspilerVerify
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            string feDll = args.Length > 0
                ? Path.GetFullPath(args[0])
                : @"D:\GGGGG\K1515\Mods\FishingExpanded\FishingExpanded.dll";
            Assembly fe = Assembly.LoadFrom(feDll);
            Type patchType = fe.GetType("FishingExpanded.Patches.BobberBarPatches");
            MethodInfo transpiler = patchType.GetMethod(
                "Update_Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo original = typeof(BobberBar).GetMethod(
                "update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var originalInsns = PatchProcessor.GetOriginalInstructions(original).ToList();
            Console.WriteLine("Original instruction count: " + originalInsns.Count);

            FieldInfo speedField = typeof(BobberBar).GetField(
                "bobberBarSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int sites = 0;
            for (int i = 0; i < originalInsns.Count; i++)
            {
                if ((originalInsns[i].opcode == OpCodes.Ldloc || originalInsns[i].opcode == OpCodes.Ldloc_S) &&
                    i >= 3 && i + 2 < originalInsns.Count &&
                    originalInsns[i - 3].opcode == OpCodes.Ldarg_0 &&
                    originalInsns[i - 2].opcode == OpCodes.Dup &&
                    originalInsns[i - 1].opcode == OpCodes.Ldfld && Equals(originalInsns[i - 1].operand, speedField) &&
                    originalInsns[i + 1].opcode == OpCodes.Add &&
                    originalInsns[i + 2].opcode == OpCodes.Stfld && Equals(originalInsns[i + 2].operand, speedField))
                {
                    sites++;
                    object op = originalInsns[i].operand;
                    string idx = "?";
                    if (op is LocalVariableInfo lvi) idx = lvi.LocalIndex.ToString();
                    else if (op is LocalBuilder lb) idx = lb.LocalIndex.ToString();
                    Console.WriteLine(
                        $"BAR-INPUT SITE #{sites} at {i}: ldloc operand type = {op?.GetType().FullName ?? "null"}, LocalIndex = {idx}");
                }
            }
            Console.WriteLine("BAR-INPUT sites found: " + sites);

            var patched = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { originalInsns })).ToList();
            int applyCalls = patched.Count(c =>
                c.opcode == OpCodes.Call && c.operand is MethodInfo m && m.Name == "ApplyBarInput");
            int bounceCalls = patched.Count(c =>
                c.opcode == OpCodes.Call && c.operand is MethodInfo m && m.Name == "ApplyBounce");
            int accelCalls = patched.Count(c =>
                c.opcode == OpCodes.Call && c.operand is MethodInfo m && m.Name == "GetAccelerationBoost");
            Console.WriteLine(
                $"Patched count: {patched.Count}; ApplyBarInput calls: {applyCalls}; " +
                $"ApplyBounce calls: {bounceCalls}; GetAccelerationBoost calls: {accelCalls}");
            return 0;
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
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
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        }
    }
}
