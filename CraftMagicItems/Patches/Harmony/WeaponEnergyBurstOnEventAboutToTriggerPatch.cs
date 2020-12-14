using System;
using Kingmaker;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(WeaponEnergyBurst), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    public static class WeaponEnergyBurstOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(WeaponEnergyBurst __instance, RuleDealDamage evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (logic.Owner == null || evt.AttackRoll == null || !evt.AttackRoll.IsCriticalConfirmed || evt.AttackRoll.FortificationNegatesCriticalHit || evt.DamageBundle.WeaponDamage == null)
                {
                    return false;
                }

                if (Main.EquipmentEnchantmentValid(evt.DamageBundle.Weapon, logic.Owner))
                {
                    RuleCalculateWeaponStats ruleCalculateWeaponStats = Rulebook.Trigger<RuleCalculateWeaponStats>(new RuleCalculateWeaponStats(Game.Instance.DefaultUnit, evt.DamageBundle.Weapon, null));
                    DiceFormula dice = new DiceFormula(Math.Max(ruleCalculateWeaponStats.CriticalMultiplier - 1, 1), __instance.Dice);
                    evt.DamageBundle.Add(new EnergyDamage(dice, __instance.Element));
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