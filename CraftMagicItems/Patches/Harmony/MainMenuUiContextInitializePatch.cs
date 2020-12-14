#if PATCH21
using Kingmaker.Assets.UI.Context;
#endif

namespace CraftMagicItems.Patches.Harmony
{
#if PATCH21
    [HarmonyLib.HarmonyPatch(typeof(MainMenuUiContext), "Initialize")]
    public static class MainMenuUiContextInitializePatch
    {
        [HarmonyLib.HarmonyPriority(HarmonyLib.Priority.Last)]
        private static void Postfix()
        {
            MainMenuStartPatch.Postfix();
        }
    }
#endif
}