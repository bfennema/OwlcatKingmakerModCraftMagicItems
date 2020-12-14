using CraftMagicItems.Localization;
using Kingmaker.Items;
using Kingmaker.UI.Common;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(ItemEntity), "VendorDescription", HarmonyLib.MethodType.Getter)]
    // ReSharper disable once UnusedMember.Local
    public static class ItemEntityVendorDescriptionPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(ItemEntity __instance, ref string __result)
        {
            // If the "vendor" is a party member, return that the item was crafted rather than from a merchant
#if PATCH21
            if (__instance.VendorBlueprint != null && __instance.VendorBlueprint.IsCompanion)
            {
                foreach (var companion in UIUtility.GetGroup(true))
                {
                    if (companion.Blueprint == __instance.VendorBlueprint)
                    {
                        __result = LocalizationHelper.FormatLocalizedString("craftMagicItems-crafted-source-description", companion.CharacterName);
                        break;
                    }
                }
                return false;
            }
#else
            if (__instance.Vendor != null && __instance.Vendor.IsPlayerFaction) {
                __result = LocalizationHelper.FormatLocalizedString("craftMagicItems-crafted-source-description", __instance.Vendor.CharacterName);
                return false;
            }
#endif
            return true;
        }
    }
}