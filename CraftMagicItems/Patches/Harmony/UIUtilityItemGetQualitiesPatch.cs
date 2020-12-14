using Kingmaker.Blueprints.Root;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UI.Common;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(UIUtilityItem), "GetQualities")]
    // ReSharper disable once UnusedMember.Local
    public static class UIUtilityItemGetQualitiesPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(ItemEntity item, ref string __result)
        {
            if (!item.IsIdentified)
            {
                return;
            }

            ItemEntityWeapon itemEntityWeapon = item as ItemEntityWeapon;
            if (itemEntityWeapon == null)
            {
                return;
            }

            WeaponCategory category = itemEntityWeapon.Blueprint.Category;
            if (category.HasSubCategory(WeaponSubCategory.Finessable) && Main.IsOversized(itemEntityWeapon.Blueprint))
            {
                __result = __result.Replace(LocalizedTexts.Instance.WeaponSubCategories.GetText(WeaponSubCategory.Finessable), "");
                __result = __result.Replace(",  ,", ",");
                char[] charsToTrim = { ',', ' ' };
                __result = __result.Trim(charsToTrim);
            }
        }
    }
}