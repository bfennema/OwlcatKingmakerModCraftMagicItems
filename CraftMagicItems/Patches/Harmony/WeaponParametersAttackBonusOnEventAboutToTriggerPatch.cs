using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(WeaponParametersAttackBonus), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    public static class WeaponParametersAttackBonusOnEventAboutToTriggerPatch
    {
        private static bool Prefix(WeaponParametersAttackBonus __instance, RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (evt.Weapon != null 
                && __instance.OnlyFinessable 
                && evt.Weapon.Blueprint.Type.Category.HasSubCategory(WeaponSubCategory.Finessable) 
                && Main.IsOversized(evt.Weapon.Blueprint))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}