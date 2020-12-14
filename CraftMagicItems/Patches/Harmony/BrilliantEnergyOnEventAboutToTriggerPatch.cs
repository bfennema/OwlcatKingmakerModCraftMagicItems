using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(BrilliantEnergy), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    public static class BrilliantEnergyOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(BrilliantEnergy __instance, RuleCalculateAC evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (evt.Reason.Item is ItemEntityWeapon weapon && Main.EquipmentEnchantmentValid(weapon, logic.Owner))
                {
                    evt.BrilliantEnergy = logic.Fact;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}