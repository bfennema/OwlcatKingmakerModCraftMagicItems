using System;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(WeaponParametersDamageBonus), "OnEventAboutToTrigger", new Type[] { typeof(RuleCalculateWeaponStats) })]
    // ReSharper disable once UnusedMember.Local
    public static class WeaponParametersDamageBonusOnEventAboutToTriggerPatch
    {
        private static bool Prefix(WeaponParametersDamageBonus __instance, RuleCalculateWeaponStats evt)
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