using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(AddInitiatorAttackRollTrigger), "CheckConditions")]
    // ReSharper disable once UnusedMember.Local
    public static class AddInitiatorAttackRollTriggerCheckConditionsPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(AddInitiatorAttackRollTrigger __instance, RuleAttackRoll evt, ref bool __result)
        {
            if (__instance is GameLogicComponent logic)
            {
                ItemEnchantment itemEnchantment = logic.Fact as ItemEnchantment;
                ItemEntity itemEntity = (itemEnchantment != null) ? itemEnchantment.Owner : null;
                RuleAttackWithWeapon ruleAttackWithWeapon = evt.Reason.Rule as RuleAttackWithWeapon;
                ItemEntityWeapon itemEntityWeapon = (ruleAttackWithWeapon != null) ? ruleAttackWithWeapon.Weapon : null;
                __result = (itemEntity == null || itemEntity == itemEntityWeapon || evt.Weapon.Blueprint.IsNatural || evt.Weapon.Blueprint.IsUnarmed) &&
                    (!__instance.CheckWeapon || (itemEntityWeapon != null && __instance.WeaponCategory == itemEntityWeapon.Blueprint.Category)) &&
                    (!__instance.OnlyHit || evt.IsHit) && (!__instance.CriticalHit || (evt.IsCriticalConfirmed && !evt.FortificationNegatesCriticalHit)) &&
                    (!__instance.SneakAttack || (evt.IsSneakAttack && !evt.FortificationNegatesSneakAttack)) &&
                    (__instance.AffectFriendlyTouchSpells || evt.Initiator.IsEnemy(evt.Target) || evt.Weapon.Blueprint.Type.AttackType != AttackType.Touch);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}