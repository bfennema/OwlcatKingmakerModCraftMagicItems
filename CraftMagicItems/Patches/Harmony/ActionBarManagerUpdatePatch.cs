#if !PATCH21
using Kingmaker.UI.ActionBar;
#endif

namespace CraftMagicItems.Patches.Harmony
{
#if !PATCH21
    // Fix issue in Owlcat's UI - ActionBarManager.Update does not refresh the Groups (spells/Actions/Belt)
    [HarmonyLib.HarmonyPatch(typeof(ActionBarManager), "Update")]
    public static class ActionBarManagerUpdatePatch {
        private static void Prefix(ActionBarManager __instance) {
            var mNeedReset = Main.Accessors.GetActionBarManagerNeedReset(__instance);
            if (mNeedReset) {
                var mSelected = Main.Accessors.GetActionBarManagerSelected(__instance);
                __instance.Group.Set(mSelected);
            }
        }
    }
#endif
}