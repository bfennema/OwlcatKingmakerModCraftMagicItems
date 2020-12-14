using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(DamageGrace), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    public static class DamageGraceOnEventAboutToTriggerPatch
    {
        private static bool Prefix(DamageGrace __instance, RuleCalculateWeaponStats evt)
        {
            if (evt.Weapon != null
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