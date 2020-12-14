using Kingmaker.Blueprints.Items.Equipment;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(BlueprintItemEquipmentUsable), "Cost", HarmonyLib.MethodType.Getter)]
    // ReSharper disable once UnusedMember.Local
    public static class BlueprintItemEquipmentUsableCostPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(BlueprintItemEquipmentUsable __instance, ref int __result)
        {
            if (__result == 0 && __instance.SpellLevel == 0)
            {
                // Owlcat's cost calculation doesn't handle level 0 spells properly.
                int chargeCost;
                switch (__instance.Type)
                {
                    case UsableItemType.Wand:
                        chargeCost = 15;
                        break;
                    case UsableItemType.Scroll:
                        chargeCost = 25;
                        break;
                    case UsableItemType.Potion:
                        chargeCost = 50;
                        break;
                    default:
                        return;
                }

                __result = __instance.CasterLevel * chargeCost * __instance.Charges / 2;
            }
        }
    }
}