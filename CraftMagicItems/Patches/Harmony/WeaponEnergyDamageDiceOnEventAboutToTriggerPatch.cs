using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(WeaponEnergyDamageDice), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    public static class WeaponEnergyDamageDiceOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(WeaponEnergyDamageDice __instance, RuleCalculateWeaponStats evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (Main.EquipmentEnchantmentValid(evt.Weapon, logic.Owner))
                {
                    DamageDescription item = new DamageDescription
                    {
                        TypeDescription = new DamageTypeDescription
                        {
                            Type = DamageType.Energy,
                            Energy = __instance.Element
                        },
                        Dice = __instance.EnergyDamageDice
                    };
                    evt.DamageDescription.Add(item);
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