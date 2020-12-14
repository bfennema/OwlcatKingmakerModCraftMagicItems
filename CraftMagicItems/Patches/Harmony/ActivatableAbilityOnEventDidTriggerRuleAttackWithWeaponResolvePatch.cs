#if !PATCH21
using System;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.ActivatableAbilities;
#endif

namespace CraftMagicItems.Patches.Harmony
{
#if !PATCH21
    [HarmonyLib.HarmonyPatch(typeof(ActivatableAbility), "OnEventDidTrigger", new Type[] { typeof(RuleAttackWithWeaponResolve) })]
    public static class ActivatableAbilityOnEventDidTriggerRuleAttackWithWeaponResolvePatch {
        private static bool Prefix(ActivatableAbility __instance, RuleAttackWithWeaponResolve evt) {
            if (evt.Damage != null && evt.AttackRoll.IsHit) {
                return false;
            } else {
                return true;
            }
        }
    }
#endif
}