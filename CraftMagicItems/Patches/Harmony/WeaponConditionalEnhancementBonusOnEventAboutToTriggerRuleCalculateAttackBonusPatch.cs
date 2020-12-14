using System;
using System.Linq;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.WeaponEnchants;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Utility;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(WeaponConditionalEnhancementBonus), "OnEventAboutToTrigger", new Type[] { typeof(RuleCalculateAttackBonus) })]
    // ReSharper disable once UnusedMember.Local
    public static class WeaponConditionalEnhancementBonusOnEventAboutToTriggerRuleCalculateAttackBonusPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(WeaponConditionalEnhancementBonus __instance, RuleCalculateAttackBonus evt)
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
                            evt.AddBonus(__instance.EnhancementBonus, logic.Fact);
                        }
                    }
                }
                else
                {
                    using (logic.Enchantment.Context.GetDataScope(evt.Target))
                    {
                        if (Main.EquipmentEnchantmentValid(evt.Weapon, logic.Owner) && __instance.Conditions.Check(null))
                        {
                            evt.AddBonus(__instance.EnhancementBonus, logic.Fact);
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