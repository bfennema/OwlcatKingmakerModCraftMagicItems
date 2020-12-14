#if !PATCH21
using HarmonyLib;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
#endif

namespace CraftMagicItems.Patches.Harmony
{
#if !PATCH21
    [HarmonyLib.HarmonyPatch(typeof(ItemEntityWeapon), "HoldInTwoHands", MethodType.Getter)]
    public static class ItemEntityWeaponHoldInTwoHandsPatch {
        private static void Postfix(ItemEntityWeapon __instance, ref bool __result) {
            if (!__result) {
                if (__instance.IsShield && __instance.Blueprint.IsOneHandedWhichCanBeUsedWithTwoHands && __instance.Wielder != null) {
                    HandSlot handSlot = __instance.Wielder.Body.PrimaryHand;
                    __result = handSlot != null && !handSlot.HasItem;
                }
            }
        }
    }
#endif
}