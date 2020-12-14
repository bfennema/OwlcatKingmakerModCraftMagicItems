#if !PATCH21
using System;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem.Rules;
#endif

namespace CraftMagicItems.Patches.Harmony
{
#if !PATCH21
    [HarmonyLib.HarmonyPatch(typeof(RuleCalculateAttacksCount), "OnTrigger")]
    public static class RuleCalculateAttacksCountOnTriggerPatch
    {
        private static void Postfix(RuleCalculateAttacksCount __instance)
        {
            int num = __instance.Initiator.Stats.BaseAttackBonus;
            int val = Math.Min(Math.Max(0, num / 5 - ((num % 5 != 0) ? 0 : 1)), 3);
            HandSlot primaryHand = __instance.Initiator.Body.PrimaryHand;
            HandSlot secondaryHand = __instance.Initiator.Body.SecondaryHand;
            ItemEntityWeapon maybeWeapon = primaryHand.MaybeWeapon;
            BlueprintItemWeapon blueprintItemWeapon = (maybeWeapon != null) ? maybeWeapon.Blueprint : null;
            BlueprintItemWeapon blueprintItemWeapon2;
            if (secondaryHand.MaybeShield != null)
            {
                if (__instance.Initiator.Descriptor.State.Features.ShieldBash)
                {
                    ItemEntityWeapon weaponComponent = secondaryHand.MaybeShield.WeaponComponent;
                    blueprintItemWeapon2 = ((weaponComponent != null) ? weaponComponent.Blueprint : null);
                }
                else
                {
                    blueprintItemWeapon2 = null;
                }
            }
            else
            {
                ItemEntityWeapon maybeWeapon2 = secondaryHand.MaybeWeapon;
                blueprintItemWeapon2 = ((maybeWeapon2 != null) ? maybeWeapon2.Blueprint : null);
            }

            if ((primaryHand.MaybeWeapon == null || !primaryHand.MaybeWeapon.HoldInTwoHands) && (blueprintItemWeapon == null || blueprintItemWeapon.IsUnarmed) && blueprintItemWeapon2 && !blueprintItemWeapon2.IsUnarmed)
            {
                __instance.SecondaryHand.PenalizedAttacks += Math.Max(0, val);
            }
        }
    }
#endif
}