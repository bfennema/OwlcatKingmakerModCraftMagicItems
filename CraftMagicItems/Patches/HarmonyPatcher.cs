using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Utility;

namespace CraftMagicItems.Patches
{
    /// <summary>Class that performs the Harmony patching</summary>
    public class HarmonyPatcher
    {
        /// <summary><see cref="Action{string}" /> that logs error messages</summary>
        protected Action<string> LogError;

        /// <summary>Harmony instance used to patch code</summary>
        protected HarmonyLib.Harmony HarmonyInstance;

        /// <summary>Definition constructor</summary>
        /// <param name="logger"><see cref="Action{string}" /> that logs error messages</param>
        public HarmonyPatcher(Action<string> logger)
        {
            HarmonyInstance = new HarmonyLib.Harmony("kingmaker.craftMagicItems");
            LogError = logger;
        }

        /// <summary>
        ///     Patches all classes in the assembly decorated with <see cref="HarmonyLib.HarmonyPatch" />,
        ///     starting in the order of the methods named in <paramref name="methodNameOrder" />.
        /// </summary>
        /// <param name="methodNameOrder">
        ///     Ordered array of method names that should be patched in this order before any
        ///     other methods are patched.
        /// </param>
        public void PatchAllOrdered(params MethodPatch[] orderedMethods)
        {
            foreach (var method in orderedMethods)
            {
                method.Patch(HarmonyInstance);
            }
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        ///     Unpatches all classes in the assembly decorated with <see cref="HarmonyLib.HarmonyPatch" />,
        ///     except the ones whose method names match any in <paramref name="exceptMethodName" />.
        /// </summary>
        /// <param name="exceptMethodName">Array of method names that should be not be unpatched</param>
        public void UnpatchAllExcept(params MethodPatch[] exceptMethods)
        {
            if (HarmonyInstance != null)
            {
                try
                {
                    foreach (var method in HarmonyInstance.GetPatchedMethods().ToArray())
                    {
                        var patchInfo = HarmonyLib.Harmony.GetPatchInfo(method);
                        if (patchInfo.Owners.Contains(HarmonyInstance.Id))
                        {
                            var methodPatches = exceptMethods.Where(m => m.MatchOriginal(method));
                            if (methodPatches.Count() > 0)
                            {
                                foreach (var patch in patchInfo.Prefixes)
                                {
                                    if (!methodPatches.Any(m => m.MatchPrefix(patch.PatchMethod)))
                                    {
                                        HarmonyInstance.Unpatch(method, patch.PatchMethod);
                                    }
                                }
                                foreach (var patch in patchInfo.Postfixes)
                                {
                                    if (!methodPatches.Any(m => m.MatchPostfix(patch.PatchMethod)))
                                    {
                                        HarmonyInstance.Unpatch(method, patch.PatchMethod);
                                    }
                                }
                                HarmonyInstance.Unpatch(method, HarmonyLib.HarmonyPatchType.Finalizer, HarmonyInstance.Id);
                                HarmonyInstance.Unpatch(method, HarmonyLib.HarmonyPatchType.Transpiler, HarmonyInstance.Id);
                                HarmonyInstance.Unpatch(method, HarmonyLib.HarmonyPatchType.ReversePatch, HarmonyInstance.Id);
                            }
                            else
                            {
                                HarmonyInstance.Unpatch(method, HarmonyLib.HarmonyPatchType.All, HarmonyInstance.Id);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.ModEntry.Logger.Error($"Exception during Un-patching: {e}");
                }
            }
        }
    }
}