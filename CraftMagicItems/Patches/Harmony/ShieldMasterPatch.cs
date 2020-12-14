#if PATCH21
#endif
using Kingmaker.Blueprints;
using Kingmaker.Designers;
#if !PATCH21
using Kingmaker.Items.Slots;
#endif
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
#if !PATCH21
using Kingmaker.UnitLogic.ActivatableAbilities;
#endif

namespace CraftMagicItems.Patches.Harmony
{
    [AllowMultipleComponents]
    public class ShieldMasterPatch : GameLogicComponent, IInitiatorRulebookHandler<RuleCalculateDamage>, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>
    {
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (!evt.Initiator.Body.SecondaryHand.HasShield || evt.DamageBundle.Weapon == null || !evt.DamageBundle.Weapon.IsShield)
            {
                return;
            }

            var armorEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.ArmorComponent);
            var weaponEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.WeaponComponent);
            if (weaponEnhancementBonus == 0 && evt.Initiator.Body.SecondaryHand.Shield.WeaponComponent.Blueprint.IsMasterwork)
            {
                weaponEnhancementBonus = 1;
            }

            var itemEnhancementBonus = armorEnhancementBonus - weaponEnhancementBonus;
            PhysicalDamage physicalDamage = evt.DamageBundle.WeaponDamage as PhysicalDamage;
            if (physicalDamage != null && itemEnhancementBonus > 0)
            {
                physicalDamage.Enchantment += itemEnhancementBonus;
                physicalDamage.EnchantmentTotal += itemEnhancementBonus;
            }
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (!evt.Initiator.Body.SecondaryHand.HasShield || evt.Weapon == null || !evt.Weapon.IsShield)
            {
                return;
            }
            var armorEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.ArmorComponent);
            var weaponEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.WeaponComponent);
            var itemEnhancementBonus = armorEnhancementBonus - weaponEnhancementBonus;
            if (itemEnhancementBonus > 0)
            {
                evt.AddBonusDamage(itemEnhancementBonus);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt) { }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (!evt.Initiator.Body.SecondaryHand.HasShield || evt.Weapon == null || !evt.Weapon.IsShield)
            {
                return;
            }

            var armorEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.ArmorComponent);
            var weaponEnhancementBonus = GameHelper.GetItemEnhancementBonus(evt.Initiator.Body.SecondaryHand.Shield.WeaponComponent);
            var num = armorEnhancementBonus - weaponEnhancementBonus;

            if (num > 0)
            {
                evt.AddBonus(num, base.Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
    }
}