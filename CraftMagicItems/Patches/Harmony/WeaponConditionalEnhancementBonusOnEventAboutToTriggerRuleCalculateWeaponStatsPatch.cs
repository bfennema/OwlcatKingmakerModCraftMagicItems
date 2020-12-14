using System;
using System.Linq;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.WeaponEnchants;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Utility;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(WeaponConditionalEnhancementBonus), "OnEventAboutToTrigger", new Type[] { typeof(RuleCalculateWeaponStats) })]
    // ReSharper disable once UnusedMember.Local
    public static class WeaponConditionalEnhancementBonusOnEventAboutToTriggerRuleCalculateWeaponStatsPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(WeaponConditionalEnhancementBonus __instance, RuleCalculateWeaponStats evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (__instance.IsBane)
                {
                    if (logic.Owner.Enchantments.Any((ItemEnchantment e) => e.Get<SuppressBane>()))
                    {
                        return false;
                    }
                }

                if (__instance.CheckWielder)
                {
                    using (logic.Enchantment.Context.GetDataScope(evt.Initiator))
                    {
                        if (Main.EquipmentEnchantmentValid(evt.Weapon, logic.Owner) && __instance.Conditions.Check(null))
                        {
                            evt.AddBonusDamage(__instance.EnhancementBonus);
                            evt.Enhancement += __instance.EnhancementBonus;
                            evt.EnhancementTotal += __instance.EnhancementBonus;
                        }
                    }
                }
                else if (evt.AttackWithWeapon != null)
                {
                    using (logic.Enchantment.Context.GetDataScope(evt.AttackWithWeapon.Target))
                    {
                        if (Main.EquipmentEnchantmentValid(evt.Weapon, logic.Owner) && __instance.Conditions.Check(null))
                        {
                            evt.AddBonusDamage(__instance.EnhancementBonus);
                            evt.Enhancement += __instance.EnhancementBonus;
                            evt.EnhancementTotal += __instance.EnhancementBonus;
                        }
                    }
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