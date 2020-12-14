using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if PATCH21
#endif
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Items;
#if !PATCH21
using Kingmaker.Items.Slots;
#endif
using Kingmaker.RuleSystem.Rules;
#if !PATCH21
using Kingmaker.UnitLogic.ActivatableAbilities;
#endif

namespace CraftMagicItems.Patches.Harmony
{

    [HarmonyLib.HarmonyPatch(typeof(TwoWeaponFightingAttackPenalty), "OnEventAboutToTrigger", new Type[] { typeof(RuleCalculateAttackBonusWithoutTarget) })]
    public static class TwoWeaponFightingAttackPenaltyOnEventAboutToTriggerPatch
    {
        static public BlueprintFeature ShieldMaster;
        static MethodInfo methodToFind;
        private static bool Prepare()
        {
            try
            {
                methodToFind = HarmonyLib.AccessTools.Property(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.IsShield)).GetGetMethod();
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.Log($"Error Preparing: {ex.Message}");
                return false;
            }
            return true;
        }
        private static IEnumerable<HarmonyLib.CodeInstruction> Transpiler(IEnumerable<HarmonyLib.CodeInstruction> instructions, ILGenerator il)
        {
            Label start = il.DefineLabel();
            yield return new HarmonyLib.CodeInstruction(OpCodes.Ldarg_0);
            yield return new HarmonyLib.CodeInstruction(OpCodes.Ldarg_1);
            yield return new HarmonyLib.CodeInstruction(OpCodes.Call, new Func<TwoWeaponFightingAttackPenalty, RuleCalculateAttackBonusWithoutTarget, bool>(CheckShieldMaster).Method);
            yield return new HarmonyLib.CodeInstruction(OpCodes.Brfalse_S, start);
            yield return new HarmonyLib.CodeInstruction(OpCodes.Ret);
            var skip = 0;
            HarmonyLib.CodeInstruction prev = instructions.First();
            prev.labels.Add(start);
            foreach (var inst in instructions.Skip(1))
            {
                if (prev.opcode == OpCodes.Ldloc_1 && inst.opcode == OpCodes.Callvirt && inst.operand as MethodInfo == methodToFind)
                {
                    // ldloc.1
                    // callvirt instance bool Kingmaker.Items.ItemEntityWeapon::get_IsShield()
                    // brtrue.s IL_0152
                    skip = 3;
                }
                if (skip > 0)
                {
                    skip--;
                }
                else
                {
                    yield return prev;
                }
                prev = inst;
            }
            if (skip == 0)
            {
                yield return prev;
            }
        }

        private static bool CheckShieldMaster(TwoWeaponFightingAttackPenalty component, RuleCalculateAttackBonusWithoutTarget evt)
        {
            ItemEntityWeapon maybeWeapon2 = evt.Initiator.Body.SecondaryHand.MaybeWeapon;
#if !PATCH21
            RuleAttackWithWeapon ruleAttackWithWeapon = evt.Reason.Rule as RuleAttackWithWeapon;
            if (ruleAttackWithWeapon != null && !ruleAttackWithWeapon.IsFullAttack)
                return true;
#endif
            return maybeWeapon2 != null && evt.Weapon == maybeWeapon2 && maybeWeapon2.IsShield && component.Owner.Progression.Features.HasFact(ShieldMaster);
        }
    }
}
