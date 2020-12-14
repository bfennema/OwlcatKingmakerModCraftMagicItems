using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(MissAgainstFactOwner), "OnEventAboutToTrigger")]
    // ReSharper disable once UnusedMember.Local
    public static class MissAgainstFactOwnerOnEventAboutToTriggerPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(MissAgainstFactOwner __instance, RuleAttackRoll evt)
        {
            if (__instance is ItemEnchantmentLogic logic)
            {
                if (Main.EquipmentEnchantmentValid(evt.Weapon, logic.Owner))
                {
                    foreach (BlueprintUnitFact blueprint in __instance.Facts)
                    {
                        if (evt.Target.Descriptor.HasFact(blueprint))
                        {
                            evt.AutoMiss = true;
                            return false;
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