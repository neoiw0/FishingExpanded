using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley.Menus;

namespace Batch034TranspilerDebug
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string dllPath = Path.GetFullPath(args[0]);
            AppDomain.CurrentDomain.AssemblyResolve += ResolveGameAssemblies;
            Assembly fe = Assembly.LoadFrom(dllPath);
            Type patchType = fe.GetType("FishingExpanded.Patches.BobberBarPatches");
            MethodInfo transpiler = patchType.GetMethod("Update_Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo original = typeof(BobberBar).GetMethod("update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            MethodBody mb = original.GetMethodBody();
            Console.WriteLine("  EH clauses: " + (mb != null ? mb.ExceptionHandlingClauses.Count : -1));

            var originalInsns = PatchProcessor.GetOriginalInstructions(original);
            var patched = (IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { originalInsns });
            var list = patched.ToList();

            bool ok = SimulateTypes(original, list);
            Console.WriteLine(ok ? "TYPE SIM OK" : "TYPE SIM FAILED");
            return ok ? 0 : 1;
        }

        private static bool SimulateTypes(MethodInfo original, List<CodeInstruction> codes)
        {
            var locals = original.GetMethodBody().LocalVariables.Cast<LocalVariableInfo>().Select(v => v.LocalType).ToList();
            var parameters = original.GetParameters().Select(p => p.ParameterType).ToList();
            Type instanceType = original.DeclaringType;

            var labelToIndex = new Dictionary<Label, int>();
            for (int i = 0; i < codes.Count; i++)
                foreach (var lbl in codes[i].labels)
                    labelToIndex[lbl] = i;

            var entryStates = new Dictionary<int, List<Type>>();
            var queue = new Queue<int>();
            entryStates[0] = new List<Type>();
            queue.Enqueue(0);
            int guard = 0;
            while (queue.Count > 0)
            {
                if (++guard > 200000) { Console.WriteLine("  CFG SIM: guard limit exceeded (possible oscillation)"); return false; }
                int i = queue.Dequeue();
                var stack = new List<Type>(entryStates[i]);
                if (!Step(codes, i, stack, locals, parameters, instanceType, original))
                    return false;

                foreach (int s in Successors(codes, i, labelToIndex))
                {
                    var incoming = new List<Type>(stack);
                    if (entryStates.TryGetValue(s, out var existing))
                    {
                        var merged = Merge(existing, incoming);
                        if (merged == null)
                        {
                            Console.WriteLine($"  [{i}] {Describe(codes[i])}: INCOMPATIBLE MERGE at target {s}");
Console.WriteLine("    existing: [" + string.Join(", ", existing.Select(Fmt)) + "]");
Console.WriteLine("    incoming: [" + string.Join(", ", incoming.Select(Fmt)) + "]");
                            DumpAround(codes, i);
                            return false;
                        }
                        if (!Same(merged, existing))
                        {
                            entryStates[s] = merged;
                            queue.Enqueue(s);
                        }
                    }
                    else
                    {
                        entryStates[s] = incoming;
                        queue.Enqueue(s);
                    }
                }
            }
            Console.WriteLine($"  CFG SIM finished, blocks={entryStates.Count}");
            return true;
        }

        private static List<int> Successors(List<CodeInstruction> codes, int i, Dictionary<Label, int> labelToIndex)
        {
            var op = codes[i].opcode;
            var result = new List<int>();
            if (op == OpCodes.Br || op == OpCodes.Br_S || op == OpCodes.Leave || op == OpCodes.Leave_S)
            {
                result.Add(TargetIndex(codes[i], labelToIndex, i));
            }
            else if (op == OpCodes.Brtrue || op == OpCodes.Brtrue_S || op == OpCodes.Brfalse || op == OpCodes.Brfalse_S ||
                     op == OpCodes.Beq || op == OpCodes.Beq_S || op == OpCodes.Bge || op == OpCodes.Bge_S || op == OpCodes.Bge_Un || op == OpCodes.Bge_Un_S ||
                     op == OpCodes.Ble || op == OpCodes.Ble_S || op == OpCodes.Ble_Un || op == OpCodes.Ble_Un_S || op == OpCodes.Blt || op == OpCodes.Blt_S ||
                     op == OpCodes.Bne_Un || op == OpCodes.Bne_Un_S || op == OpCodes.Blt_Un || op == OpCodes.Blt_Un_S || op == OpCodes.Bgt || op == OpCodes.Bgt_S ||
                     op == OpCodes.Bgt_Un || op == OpCodes.Bgt_Un_S)
            {
                result.Add(i + 1);
                result.Add(TargetIndex(codes[i], labelToIndex, i));
            }
            else if (op == OpCodes.Switch)
            {
                result.Add(i + 1);
                foreach (var lbl in (Label[])codes[i].operand)
                {
                    if (!labelToIndex.TryGetValue(lbl, out int t)) throw new InvalidOperationException("switch label missing at " + i);
                    result.Add(t);
                }
            }
            else if (op == OpCodes.Ret || op == OpCodes.Throw)
            {
                // terminal
            }
            else if (op == OpCodes.Endfinally)
            {
                Console.WriteLine($"  [{i}] {Describe(codes[i])}: endfinally without EH is invalid");
            }
            else
            {
                result.Add(i + 1);
            }
            return result;
        }

        private static int TargetIndex(CodeInstruction c, Dictionary<Label, int> labelToIndex, int i)
        {
            if (c.operand is Label lbl && labelToIndex.TryGetValue(lbl, out int t)) return t;
            throw new InvalidOperationException("branch target missing at " + i);
        }

        private static List<Type> Merge(List<Type> existing, List<Type> incoming)
        {
            if (existing.Count != incoming.Count) return null;
            var merged = new List<Type>(existing.Count);
            for (int k = 0; k < existing.Count; k++)
            {
                Type a = existing[k], b = incoming[k];
                if (a == b) { merged.Add(a); continue; }
                if (Normalize(a) == Normalize(b)) { merged.Add(a); continue; }
                if (a == typeof(object) || b == typeof(object)) { merged.Add(typeof(object)); continue; }
                if (!a.IsValueType && !b.IsValueType) { merged.Add(typeof(object)); continue; }
                if (a.IsByRef && b.IsByRef)
                {
                    Type ea = a.GetElementType(), eb = b.GetElementType();
                    if (ea == eb) { merged.Add(a); continue; }
                    if (!ea.IsValueType && !eb.IsValueType) { merged.Add(typeof(object).MakeByRefType()); continue; }
                    return null;
                }
                return null;
            }
            return merged;
        }

        private static bool Same(List<Type> a, List<Type> b)
        {
            if (a.Count != b.Count) return false;
            for (int k = 0; k < a.Count; k++) if (a[k] != b[k]) return false;
            return true;
        }

        private static bool Step(List<CodeInstruction> codes, int index, List<Type> stack, List<Type> locals, List<Type> parameters, Type instanceType, MethodInfo original)
        {
            var c = codes[index];
            var op = c.opcode;
            string tag = Describe(c);
            try
            {
                if (op == OpCodes.Ldarg_0 || op == OpCodes.Ldarg || op == OpCodes.Ldarg_S) { stack.Add(instanceType); }
                else if (op == OpCodes.Ldarg_1) { stack.Add(parameters.Count > 0 ? parameters[0] : typeof(object)); }
                else if (op == OpCodes.Ldarg_2) { stack.Add(parameters.Count > 1 ? parameters[1] : typeof(object)); }
                else if (op == OpCodes.Ldarg_3) { stack.Add(parameters.Count > 2 ? parameters[2] : typeof(object)); }
                else if (op == OpCodes.Ldarga || op == OpCodes.Ldarga_S) { stack.Add(parameters[GetParamIndex(c, parameters)].MakeByRefType()); }
                else if (op == OpCodes.Ldloc || op == OpCodes.Ldloc_S || op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1 || op == OpCodes.Ldloc_2 || op == OpCodes.Ldloc_3)
                {
                    int slot = op == OpCodes.Ldloc_0 ? 0 : op == OpCodes.Ldloc_1 ? 1 : op == OpCodes.Ldloc_2 ? 2 : op == OpCodes.Ldloc_3 ? 3 : GetLocalIndex(c, locals);
                    stack.Add(locals[slot]);
                }
                else if (op == OpCodes.Stloc || op == OpCodes.Stloc_S || op == OpCodes.Stloc_0 || op == OpCodes.Stloc_1 || op == OpCodes.Stloc_2 || op == OpCodes.Stloc_3)
                {
                    int slot = op == OpCodes.Stloc_0 ? 0 : op == OpCodes.Stloc_1 ? 1 : op == OpCodes.Stloc_2 ? 2 : op == OpCodes.Stloc_3 ? 3 : GetLocalIndex(c, locals);
                    PopChecked(stack, locals[slot], tag, index);
                }
                else if (op == OpCodes.Ldloca || op == OpCodes.Ldloca_S) { stack.Add(locals[GetLocalIndex(c, locals)].MakeByRefType()); }
                else if (op == OpCodes.Ldc_I4 || op == OpCodes.Ldc_I4_S || op == OpCodes.Ldc_I4_0 || op == OpCodes.Ldc_I4_1 || op == OpCodes.Ldc_I4_2 || op == OpCodes.Ldc_I4_3 || op == OpCodes.Ldc_I4_4 || op == OpCodes.Ldc_I4_5 || op == OpCodes.Ldc_I4_6 || op == OpCodes.Ldc_I4_7 || op == OpCodes.Ldc_I4_8 || op == OpCodes.Ldc_I4_M1) { stack.Add(typeof(int)); }
                else if (op == OpCodes.Ldc_I8) { stack.Add(typeof(long)); }
                else if (op == OpCodes.Ldc_R4) { stack.Add(typeof(float)); }
                else if (op == OpCodes.Ldc_R8) { stack.Add(typeof(double)); }
                else if (op == OpCodes.Ldstr) { stack.Add(typeof(string)); }
                else if (op == OpCodes.Ldnull) { stack.Add(typeof(object)); }
                else if (op == OpCodes.Ldsfld) { stack.Add(FieldType(c, tag)); }
                else if (op == OpCodes.Ldsflda) { stack.Add(FieldType(c, tag).MakeByRefType()); }
                else if (op == OpCodes.Ldfld) { PopChecked(stack, typeof(object), tag, index); stack.Add(FieldType(c, tag)); }
                else if (op == OpCodes.Ldflda) { PopChecked(stack, typeof(object), tag, index); stack.Add(FieldType(c, tag).MakeByRefType()); }
                else if (op == OpCodes.Stfld) { PopChecked(stack, FieldType(c, tag), tag, index); PopChecked(stack, typeof(object), tag, index); }
                else if (op == OpCodes.Ldobj) { PopChecked(stack, null, tag, index); stack.Add((c.operand as Type) ?? typeof(object)); }
                else if (op == OpCodes.Stobj) { PopChecked(stack, (c.operand as Type) ?? typeof(object), tag, index); PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Ldind_I || op == OpCodes.Ldind_I1 || op == OpCodes.Ldind_I2 || op == OpCodes.Ldind_I4 || op == OpCodes.Ldind_I8) { PopChecked(stack, null, tag, index); stack.Add(typeof(int)); }
                else if (op == OpCodes.Ldind_R4) { PopChecked(stack, null, tag, index); stack.Add(typeof(float)); }
                else if (op == OpCodes.Ldind_R8) { PopChecked(stack, null, tag, index); stack.Add(typeof(double)); }
                else if (op == OpCodes.Ldind_Ref) { PopChecked(stack, null, tag, index); stack.Add(typeof(object)); }
                else if (op == OpCodes.Stind_I || op == OpCodes.Stind_I1 || op == OpCodes.Stind_I2 || op == OpCodes.Stind_I4 || op == OpCodes.Stind_I8 || op == OpCodes.Stind_R4 || op == OpCodes.Stind_R8 || op == OpCodes.Stind_Ref) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Ldelem_Ref) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); stack.Add(typeof(object)); }
                else if (op == OpCodes.Stelem_Ref) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Ldelema) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); stack.Add((c.operand as Type ?? typeof(object)).MakeByRefType()); }
                else if (op == OpCodes.Ldelem_I1 || op == OpCodes.Ldelem_I2 || op == OpCodes.Ldelem_I4 || op == OpCodes.Ldelem_I8 || op == OpCodes.Ldelem_U1 || op == OpCodes.Ldelem_U2 || op == OpCodes.Ldelem_U4) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); stack.Add(typeof(int)); }
                else if (op == OpCodes.Ldelem_R4) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); stack.Add(typeof(float)); }
                else if (op == OpCodes.Ldelem_R8) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); stack.Add(typeof(double)); }
                else if (op == OpCodes.Ldlen) { PopChecked(stack, null, tag, index); stack.Add(typeof(int)); }
                else if (op == OpCodes.Stelem_I1 || op == OpCodes.Stelem_I2 || op == OpCodes.Stelem_I4 || op == OpCodes.Stelem_I8 || op == OpCodes.Stelem_R4 || op == OpCodes.Stelem_R8) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Newarr) { PopChecked(stack, null, tag, index); stack.Add((c.operand as Type ?? typeof(object)).MakeArrayType()); }
                else if (op == OpCodes.Add || op == OpCodes.Sub || op == OpCodes.Mul || op == OpCodes.Div || op == OpCodes.Rem)
                {
                    Type b = PopChecked(stack, null, tag, index); Type a = PopChecked(stack, null, tag, index);
                    Type ka = NumKind(a), kb = NumKind(b);
                    if (ka == null || kb == null || ka != kb)
                    {
                        Console.WriteLine($"  [{index}] {tag}: ARITH TYPE MISMATCH {Fmt(a)} op {Fmt(b)}");
                        DumpAround(codes, index);
                        return false;
                    }
                    stack.Add(ka == typeof(double) ? typeof(double) : ka == typeof(float) ? typeof(float) : typeof(int));
                }
                else if (op == OpCodes.Neg) { Type a = PopChecked(stack, null, tag, index); stack.Add(a); }
                else if (op == OpCodes.And || op == OpCodes.Or || op == OpCodes.Xor || op == OpCodes.Shl || op == OpCodes.Shr || op == OpCodes.Shr_Un) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); stack.Add(typeof(int)); }
                else if (op == OpCodes.Not) { PopChecked(stack, null, tag, index); stack.Add(typeof(int)); }
                else if (op == OpCodes.Clt || op == OpCodes.Clt_Un || op == OpCodes.Cgt || op == OpCodes.Cgt_Un || op == OpCodes.Ceq)
                {
                    PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); stack.Add(typeof(int));
                }
                else if (op == OpCodes.Conv_I1 || op == OpCodes.Conv_I2 || op == OpCodes.Conv_I4 || op == OpCodes.Conv_I8 || op == OpCodes.Conv_U1 || op == OpCodes.Conv_U2 || op == OpCodes.Conv_U4 || op == OpCodes.Conv_U8 || op == OpCodes.Conv_Ovf_I || op == OpCodes.Conv_Ovf_I4 || op == OpCodes.Conv_Ovf_I8 || op == OpCodes.Conv_Ovf_U || op == OpCodes.Conv_Ovf_U4 || op == OpCodes.Conv_Ovf_U8 || op == OpCodes.Conv_Ovf_I_Un || op == OpCodes.Conv_Ovf_I4_Un || op == OpCodes.Conv_Ovf_I8_Un || op == OpCodes.Conv_Ovf_U_Un || op == OpCodes.Conv_Ovf_U4_Un || op == OpCodes.Conv_Ovf_U8_Un || op == OpCodes.Conv_U || op == OpCodes.Conv_I) { PopChecked(stack, null, tag, index); stack.Add(typeof(int)); }
                else if (op == OpCodes.Conv_R4) { PopChecked(stack, null, tag, index); stack.Add(typeof(float)); }
                else if (op == OpCodes.Conv_R8 || op == OpCodes.Conv_R_Un) { PopChecked(stack, null, tag, index); stack.Add(typeof(double)); }
                else if (op == OpCodes.Call || op == OpCodes.Callvirt)
                {
                    var method = c.operand as MethodInfo;
                    if (method == null) { Console.WriteLine($"  [{index}] {tag}: call operand missing!"); DumpAround(codes, index); return false; }
                    var ps = method.GetParameters();
                    for (int k = ps.Length - 1; k >= 0; k--) PopChecked(stack, ps[k].ParameterType, tag, index);
                    if (!method.IsStatic)
                    {
                        Type owner = method.DeclaringType;
                        if (owner != null && owner.IsValueType) owner = owner.MakeByRefType();
                        PopChecked(stack, owner ?? typeof(object), tag, index);
                    }
                    if (method.ReturnType != typeof(void)) stack.Add(method.ReturnType);
                }
                else if (op == OpCodes.Newobj)
                {
                    var ctor = c.operand as ConstructorInfo;
                    if (ctor == null) { Console.WriteLine($"  [{index}] {tag}: newobj operand missing!"); DumpAround(codes, index); return false; }
                    var ps = ctor.GetParameters();
                    for (int k = ps.Length - 1; k >= 0; k--) PopChecked(stack, ps[k].ParameterType, tag, index);
                    stack.Add(ctor.DeclaringType);
                }
                else if (op == OpCodes.Br || op == OpCodes.Br_S || op == OpCodes.Leave || op == OpCodes.Leave_S) { }
                else if (op == OpCodes.Brtrue || op == OpCodes.Brtrue_S || op == OpCodes.Brfalse || op == OpCodes.Brfalse_S) { PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Beq || op == OpCodes.Beq_S || op == OpCodes.Bge || op == OpCodes.Bge_S || op == OpCodes.Bge_Un || op == OpCodes.Bge_Un_S || op == OpCodes.Ble || op == OpCodes.Ble_S || op == OpCodes.Ble_Un || op == OpCodes.Ble_Un_S || op == OpCodes.Blt || op == OpCodes.Blt_S || op == OpCodes.Bne_Un || op == OpCodes.Bne_Un_S || op == OpCodes.Blt_Un || op == OpCodes.Blt_Un_S || op == OpCodes.Bgt || op == OpCodes.Bgt_S || op == OpCodes.Bgt_Un || op == OpCodes.Bgt_Un_S) { PopChecked(stack, null, tag, index); PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Switch) { PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Dup) { stack.Add(stack[stack.Count - 1]); }
                else if (op == OpCodes.Pop) { PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Ret)
                {
                    if (original.ReturnType == typeof(void))
                    {
                        if (stack.Count != 0) { Console.WriteLine($"  [{index}] {tag}: RET with {stack.Count} items left on stack: [{string.Join(", ", stack.Select(Fmt))}]"); DumpAround(codes, index); return false; }
                    }
                    else PopChecked(stack, original.ReturnType, tag, index);
                }
                else if (op == OpCodes.Initobj) { PopChecked(stack, null, tag, index); }
                else if (op == OpCodes.Box) { PopChecked(stack, null, tag, index); stack.Add(typeof(object)); }
                else if (op == OpCodes.Unbox) { PopChecked(stack, null, tag, index); stack.Add(((Type)c.operand).MakeByRefType()); }
                else if (op == OpCodes.Unbox_Any) { PopChecked(stack, null, tag, index); stack.Add((Type)c.operand); }
                else if (op == OpCodes.Isinst) { PopChecked(stack, null, tag, index); stack.Add((Type)c.operand); }
                else if (op == OpCodes.Castclass) { PopChecked(stack, null, tag, index); stack.Add((Type)c.operand); }
                else if (op == OpCodes.Constrained || op == OpCodes.Readonly || op == OpCodes.Volatile || op == OpCodes.Unaligned || op == OpCodes.Tailcall || op == OpCodes.Endfinally) { }
                else if (op == OpCodes.Ldtoken || op == OpCodes.Arglist || op == OpCodes.Localloc || op == OpCodes.Calli || op == OpCodes.Mkrefany || op == OpCodes.Refanyval || op == OpCodes.Refanytype || op == OpCodes.Jmp) { stack.Add(typeof(object)); }
                else { Console.WriteLine($"  [{index}] {tag}: UNHANDLED opcode {op}"); DumpAround(codes, index); return false; }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"  [{index}] {tag}: TYPE MISMATCH - {ex.Message}");
                DumpAround(codes, index);
                return false;
            }
            return true;
        }

        private static Type PopChecked(List<Type> stack, Type expected, string tag, int index)
        {
            if (stack.Count == 0) throw new InvalidOperationException("stack underflow at " + tag);
            Type t = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (expected != null && !Compatible(t, expected))
            {
                throw new InvalidOperationException($"expected {Fmt(expected)} but stack top is {Fmt(t)} (at {tag})");
            }
            return t;
        }

        private static bool Compatible(Type actual, Type expected)
        {
            if (expected == typeof(object) || actual == typeof(object) || actual == typeof(IntPtr) || expected == typeof(IntPtr)) return true;
            Type a = Normalize(actual);
            Type e = Normalize(expected);
            if (a == e) return true;
            if (expected.IsAssignableFrom(actual)) return true;
            if (actual.IsByRef && Compatible(actual.GetElementType(), expected)) return true;
            if (expected.IsByRef && actual.IsByRef) return Compatible(actual.GetElementType(), expected.GetElementType());
            return false;
        }

        private static Type Normalize(Type t)
        {
            if (t == typeof(bool) || t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) || t == typeof(char) || t == typeof(uint) || t == typeof(ulong) || t.IsEnum) return typeof(int);
            return t;
        }

        private static Type NumKind(Type t)
        {
            t = Normalize(t);
            if (t == typeof(float) || t == typeof(double) || t == typeof(int)) return t;
            return null;
        }

        private static string Fmt(Type t)
        {
            if (t == null) return "null";
            if (t == typeof(object)) return "object";
            if (t.IsByRef) return Fmt(t.GetElementType()) + "&";
            return t.Name;
        }

        private static int GetParamIndex(CodeInstruction c, List<Type> parameters)
        {
            if (c.operand is int i) return i;
            if (c.operand is byte b) return b;
            return 0;
        }

        private static int GetLocalIndex(CodeInstruction c, List<Type> locals)
        {
            if (c.operand is LocalBuilder lb) return lb.LocalIndex;
            if (c.operand is int i) return i;
            return 0;
        }

        private static Type FieldType(CodeInstruction c, string tag)
        {
            if (c.operand is FieldInfo f) return f.FieldType;
            throw new InvalidOperationException("field operand missing at " + tag);
        }

        private static string Describe(CodeInstruction c)
        {
            string operand = c.operand == null ? "" : c.operand.ToString();
            return $"{c.opcode} {operand}".Trim();
        }

        private static void DumpAround(List<CodeInstruction> codes, int index)
        {
            int start = Math.Max(0, index - 12);
            int end = Math.Min(codes.Count - 1, index + 6);
            for (int i = start; i <= end; i++)
            {
                string mark = i == index ? " >>" : "   ";
                Console.WriteLine($"  {mark} [{i}] {Describe(codes[i])}");
            }
        }

        private static Assembly ResolveGameAssemblies(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name + ".dll";
            string[] dirs = { @"D:\GGGGG\K1515", @"D:\GGGGG\K1515\smapi-internal", @"D:\GGGGG\K1515\Mods\FishingExpanded" };
            foreach (string dir in dirs)
            {
                string path = Path.Combine(dir, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        }
    }
}


