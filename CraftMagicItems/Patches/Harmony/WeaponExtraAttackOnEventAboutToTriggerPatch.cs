using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(WeaponExtraAttack), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    public static class WeaponExtraAttackOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(WeaponExtraAttack __instance, RuleCalculateAttacksCount evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (logic.Owner is ItemEntityWeapon)
                {
                    evt.AddExtraAttacks(__instance.Number, __instance.Haste, __instance.Owner);
                }
                else if (evt.Initiator.GetFirstWeapon() != null
                    && (evt.Initiator.GetFirstWeapon().Blueprint.IsNatural || evt.Initiator.GetFirstWeapon().Blueprint.IsUnarmed))
                {
                    evt.AddExtraAttacks(__instance.Number, __instance.Haste);
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