using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib;

internal class MethodCopier
{
	private readonly MethodBodyReader reader;

	private readonly List<MethodInfo> transpilers = new List<MethodInfo>();

	internal MethodCopier(MethodBase fromMethod, ILGenerator toILGenerator, LocalBuilder[] existingVariables = null)
	{
		if ((object)fromMethod == null)
		{
			throw new ArgumentNullException("fromMethod");
		}
		reader = new MethodBodyReader(fromMethod, toILGenerator);
		reader.DeclareVariables(existingVariables);
		reader.GenerateInstructions();
	}

	internal void SetDebugging(bool debug)
	{
		reader.SetDebugging(debug);
	}

	internal void SetArgumentShift(bool useShift)
	{
		reader.SetArgumentShift(useShift);
	}

	internal void AddTranspiler(MethodInfo transpiler)
	{
		transpilers.Add(transpiler);
	}

	internal List<CodeInstruction> Finalize(Emitter emitter, List<Label> endLabels, out bool hasReturnCode)
	{
		return reader.FinalizeILCodes(emitter, transpilers, endLabels, out hasReturnCode);
	}

	internal static List<CodeInstruction> GetInstructions(ILGenerator generator, MethodBase method, int maxTranspilers)
	{
		if (generator == null)
		{
			throw new ArgumentNullException("generator");
		}
		if ((object)method == null)
		{
			throw new ArgumentNullException("method");
		}
		LocalBuilder[] existingVariables = MethodPatcher.DeclareLocalVariables(generator, method);
		bool argumentShift = StructReturnBuffer.NeedsFix(method);
		MethodCopier methodCopier = new MethodCopier(method, generator, existingVariables);
		methodCopier.SetArgumentShift(argumentShift);
		Patches patchInfo = Harmony.GetPatchInfo(method);
		if (patchInfo != null)
		{
			List<MethodInfo> sortedPatchMethods = PatchFunctions.GetSortedPatchMethods(method, patchInfo.Transpilers.ToArray(), debug: false);
			for (int i = 0; i < maxTranspilers && i < sortedPatchMethods.Count; i++)
			{
				methodCopier.AddTranspiler(sortedPatchMethods[i]);
			}
		}
		bool hasReturnCode;
		return methodCopier.Finalize(null, null, out hasReturnCode);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.0.0.9375' (yours is '9.1.0.7988')
