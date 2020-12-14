using System.Linq;
using CraftMagicItems.Constants;
using Kingmaker.Blueprints;
using Kingmaker.Items;
using Kingmaker.Utility;
using Kingmaker.View.Equipment;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(UnitViewHandSlotData), "OwnerWeaponScale", HarmonyLib.MethodType.Getter)]
    public static class UnitViewHandSlotDataWeaponScalePatch
    {
        private static void Postfix(UnitViewHandSlotData __instance, ref float __result)
        {
            if (__instance.VisibleItem is ItemEntityWeapon weapon && !weapon.Blueprint.AssetGuid.Contains(",visual="))
            {
                var enchantment = Main.GetEnchantments(weapon.Blueprint).FirstOrDefault(e => e.AssetGuid.StartsWith(ItemQualityBlueprints.OversizedGuid));
                if (enchantment != null)
                {
                    var component = enchantment.GetComponent<WeaponBaseSizeChange>();
                    if (component != null)
                    {
                        if (component.SizeCategoryChange > 0)
                        {
                            __result *= 4.0f / 3.0f;
                        }
                        else if (component.SizeCategoryChange < 0)
                        {
                            __result *= 0.75f;
                        }
                    }
                }
            }
        }
    }
}