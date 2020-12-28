using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.ActionBar;
using Kingmaker.UI.Common;
using Kingmaker.UI.Tooltip;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.View.Animation;
using UnityEngine;

namespace CraftMagicItems {
    /**
     * Spacehamster's idea: create reflection-based accessors up front, so the mod fails on startup if the Kingmaker code changes in an incompatible way.
     */
    public class CraftMagicItemsAccessors {
        public readonly HarmonyLib.AccessTools.FieldRef<Spellbook, List<BlueprintSpellList>> GetSpellbookSpecialLists =
            Accessors.CreateFieldRef<Spellbook, List<BlueprintSpellList>>("m_SpecialLists");

        public readonly HarmonyLib.AccessTools.FieldRef<ActionBarManager, bool> GetActionBarManagerNeedReset = Accessors.CreateFieldRef<ActionBarManager, bool>("m_NeedReset");

        public readonly HarmonyLib.AccessTools.FieldRef<ActionBarManager, UnitEntityData> GetActionBarManagerSelected =
            Accessors.CreateFieldRef<ActionBarManager, UnitEntityData>("m_Selected");

        public readonly HarmonyLib.AccessTools.FieldRef<Spellbook, List<AbilityData>[]> GetSpellbookKnownSpells =
            Accessors.CreateFieldRef<Spellbook, List<AbilityData>[]>("m_KnownSpells");

        public readonly HarmonyLib.AccessTools.FieldRef<Spellbook, Dictionary<BlueprintAbility, int>> GetSpellbookKnownSpellLevels =
            Accessors.CreateFieldRef<Spellbook, Dictionary<BlueprintAbility, int>>("m_KnownSpellLevels");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintUnitFact, LocalizedString> SetBlueprintUnitFactDisplayName =
            Accessors.CreateFieldRef<BlueprintUnitFact, LocalizedString>("m_DisplayName");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintUnitFact, LocalizedString> SetBlueprintUnitFactDescription =
            Accessors.CreateFieldRef<BlueprintUnitFact, LocalizedString>("m_Description");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintUnitFact, Sprite> SetBlueprintUnitFactIcon = Accessors.CreateFieldRef<BlueprintUnitFact, Sprite>("m_Icon");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItem, List<BlueprintItemEnchantment>> SetBlueprintItemCachedEnchantments =
            Accessors.CreateFieldRef<BlueprintItem, List<BlueprintItemEnchantment>>("m_CachedEnchantments");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemShield, BlueprintItemArmor> SetBlueprintItemShieldArmorComponent =
            Accessors.CreateFieldRef<BlueprintItemShield, BlueprintItemArmor>("m_ArmorComponent");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemShield, BlueprintItemWeapon> SetBlueprintItemShieldWeaponComponent =
            Accessors.CreateFieldRef<BlueprintItemShield, BlueprintItemWeapon>("m_WeaponComponent");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemWeapon, DamageTypeDescription> SetBlueprintItemWeaponDamageType =
            Accessors.CreateFieldRef<BlueprintItemWeapon, DamageTypeDescription>("m_DamageType");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemWeapon, BlueprintWeaponEnchantment[]> SetBlueprintItemWeaponEnchantments =
            Accessors.CreateFieldRef<BlueprintItemWeapon, BlueprintWeaponEnchantment[]>("m_Enchantments");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemWeapon, bool> SetBlueprintItemWeaponOverrideDamageType =
            Accessors.CreateFieldRef<BlueprintItemWeapon, bool>("m_OverrideDamageType");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintWeaponType, DiceFormula> SetBlueprintItemBaseDamage =
            Accessors.CreateFieldRef<BlueprintWeaponType, DiceFormula>("m_BaseDamage");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItem, Sprite> SetBlueprintItemIcon = Accessors.CreateFieldRef<BlueprintItem, Sprite>("m_Icon");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEquipmentHand, WeaponVisualParameters> SetBlueprintItemEquipmentHandVisualParameters =
            Accessors.CreateFieldRef<BlueprintItemEquipmentHand, WeaponVisualParameters>("m_VisualParameters");

        public readonly HarmonyLib.AccessTools.FieldRef<WeaponVisualParameters, GameObject> SetWeaponVisualParametersModel =
            Accessors.CreateFieldRef<WeaponVisualParameters, GameObject>("m_WeaponModel");

        public readonly HarmonyLib.AccessTools.FieldRef<WeaponVisualParameters, WeaponAnimationStyle> SetBlueprintItemEquipmentWeaponAnimationStyle =
            Accessors.CreateFieldRef<WeaponVisualParameters, WeaponAnimationStyle>("m_WeaponAnimationStyle");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemArmor, ArmorVisualParameters> SetBlueprintItemArmorVisualParameters =
            Accessors.CreateFieldRef<BlueprintItemArmor, ArmorVisualParameters>("m_VisualParameters");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemArmor, BlueprintEquipmentEnchantment[]> SetBlueprintItemArmorEnchantments =
            Accessors.CreateFieldRef<BlueprintItemArmor, BlueprintEquipmentEnchantment[]>("m_Enchantments");

        //public readonly SetterHandler<BlueprintBuff, int> SetBlueprintBuffFlags = Accessors.CreateSetter<BlueprintBuff, int>("m_Flags");
        public void SetBlueprintBuffFlags(BlueprintBuff buff, int flags) {
            HarmonyLib.AccessTools.Field(typeof(BlueprintBuff), "m_Flags").SetValue(buff, flags);
        }

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItem, LocalizedString> SetBlueprintItemDisplayNameText =
            Accessors.CreateFieldRef<BlueprintItem, LocalizedString>("m_DisplayNameText");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItem, LocalizedString> SetBlueprintItemDescriptionText =
            Accessors.CreateFieldRef<BlueprintItem, LocalizedString>("m_DescriptionText");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItem, LocalizedString> SetBlueprintItemFlavorText =
            Accessors.CreateFieldRef<BlueprintItem, LocalizedString>("m_FlavorText");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItem, int> SetBlueprintItemCost = Accessors.CreateFieldRef<BlueprintItem, int>("m_Cost");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItem, bool> SetBlueprintItemIsStackable = Accessors.CreateFieldRef<BlueprintItem, bool>("m_IsStackable");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEnchantment, LocalizedString> GetBlueprintItemEnchantmentEnchantName =
            Accessors.CreateFieldRef<BlueprintItemEnchantment, LocalizedString>("m_EnchantName");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEnchantment, LocalizedString> SetBlueprintItemEnchantmentEnchantName =
            Accessors.CreateFieldRef<BlueprintItemEnchantment, LocalizedString>("m_EnchantName");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEnchantment, LocalizedString> GetBlueprintItemEnchantmentDescription =
            Accessors.CreateFieldRef<BlueprintItemEnchantment, LocalizedString>("m_Description");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEnchantment, LocalizedString> SetBlueprintItemEnchantmentDescription =
            Accessors.CreateFieldRef<BlueprintItemEnchantment, LocalizedString>("m_Description");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEnchantment, LocalizedString> SetBlueprintItemEnchantmentPrefix =
            Accessors.CreateFieldRef<BlueprintItemEnchantment, LocalizedString>("m_Prefix");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEnchantment, LocalizedString> SetBlueprintItemEnchantmentSuffix =
            Accessors.CreateFieldRef<BlueprintItemEnchantment, LocalizedString>("m_Suffix");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEnchantment, int> SetBlueprintItemEnchantmentEnchantmentCost =
            Accessors.CreateFieldRef<BlueprintItemEnchantment, int>("m_EnchantmentCost");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItemEnchantment, int> SetBlueprintItemEnchantmentEnchantmentIdentifyDC =
            Accessors.CreateFieldRef<BlueprintItemEnchantment, int>("m_IdentifyDC");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintScriptableObject, string> SetBlueprintScriptableObjectAssetGuid =
            Accessors.CreateFieldRef<BlueprintScriptableObject, string>("m_AssetGuid");

        public readonly HarmonyLib.AccessTools.FieldRef<LootItemsPackFixed, LootItem> SetLootItemsPackFixedItem = Accessors.CreateFieldRef<LootItemsPackFixed, LootItem>("m_Item");

        public readonly HarmonyLib.AccessTools.FieldRef<LootItem, BlueprintItem> SetLootItemItem = Accessors.CreateFieldRef<LootItem, BlueprintItem>("m_Item");

        public readonly FastSetter<RuleDealDamage, int> SetRuleDealDamageDamage = Accessors.CreateSetter<RuleDealDamage, int>("Damage");

        public readonly HarmonyLib.AccessTools.FieldRef<BlueprintItem, float> SetBlueprintItemWeight = Accessors.CreateFieldRef<BlueprintItem, float>("m_Weight");

        public readonly FastStaticInvoker<ItemEntity, string> CallUIUtilityItemGetQualities =
            Accessors.CreateStaticInvoker<ItemEntity, string>(typeof(UIUtilityItem), "GetQualities");

        public readonly FastStaticInvoker<ItemEntity, TooltipData, string> CallUIUtilityItemFillEnchantmentDescription =
            Accessors.CreateStaticInvoker<ItemEntity, TooltipData, string>(typeof(UIUtilityItem), "FillEnchantmentDescription");

        public readonly FastStaticInvoker<TooltipData, ItemEntityWeapon, string, string> CallUIUtilityItemFillWeaponQualities =
            Accessors.CreateStaticInvoker<TooltipData, ItemEntityWeapon, string, string>(typeof(UIUtilityItem), "FillWeaponQualities");
    }
}