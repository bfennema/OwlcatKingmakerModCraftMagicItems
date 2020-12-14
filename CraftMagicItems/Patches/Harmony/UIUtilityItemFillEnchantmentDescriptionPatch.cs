using System;
using System.Linq;
using CraftMagicItems.Constants;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.UI.Tooltip;
using Kingmaker.Utility;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(UIUtilityItem), "FillEnchantmentDescription")]
    // ReSharper disable once UnusedMember.Local
    public static class UIUtilityItemFillEnchantmentDescriptionPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(ItemEntity item, TooltipData data, ref string __result)
        {
            string text = string.Empty;
            if (item is ItemEntityShield shield && shield.IsIdentified)
            {
                // It appears that shields are not properly identified when found.
                shield.ArmorComponent.Identify();
                shield.WeaponComponent?.Identify();
                return true;
            }
            else if (item.Blueprint.ItemType == ItemsFilter.ItemType.Neck && Main.ItemPlusEquivalent(item.Blueprint) > 0)
            {
                if (item.IsIdentified)
                {
                    foreach (ItemEnchantment itemEnchantment in item.Enchantments)
                    {
                        itemEnchantment.Blueprint.CallComponents<AddStatBonusEquipment>(c =>
                        {
                            if (!data.StatBonus.ContainsKey(c.Stat))
                            {
                                data.StatBonus.Add(c.Stat, UIUtility.AddSign(c.Value));
                            }
                        });

                        if (!data.Texts.ContainsKey(TooltipElement.Qualities))
                        {
                            data.Texts[TooltipElement.Qualities] = Main.Accessors.CallUIUtilityItemGetQualities(item);
                        }

                        if (!string.IsNullOrEmpty(itemEnchantment.Blueprint.Description))
                        {
                            text += string.Format("<b><align=\"center\">{0}</align></b>\n", itemEnchantment.Blueprint.Name);
                            text = text + itemEnchantment.Blueprint.Description + "\n\n";
                        }
                    }

                    if (item.Enchantments.Any<ItemEnchantment>() && !data.Texts.ContainsKey(TooltipElement.Qualities))
                    {
                        data.Texts[TooltipElement.Qualities] = GetEnhancementBonus(item);
                    }

                    if (GetItemEnhancementBonus(item) > 0)
                    {
                        data.Texts[TooltipElement.Enhancement] = GetEnhancementBonus(item);
                    }
                }
                __result = text;
                return false;
            }
            else
            {
                return true;
            }
        }

        private static string GetEnhancementBonus(ItemEntity item)
        {
            if (!item.IsIdentified)
            {
                return string.Empty;
            }
            int itemEnhancementBonus = GetItemEnhancementBonus(item);
            return (itemEnhancementBonus == 0) ? string.Empty : UIUtility.AddSign(itemEnhancementBonus);
        }

        public static int GetItemEnhancementBonus(ItemEntity item)
        {
            return item.Enchantments.SelectMany((ItemEnchantment f) => f.SelectComponents<EquipmentWeaponTypeEnhancement>()).Aggregate(0, (int s, EquipmentWeaponTypeEnhancement e) => s + e.Enhancement);
        }

        private static void Postfix(ItemEntity item, TooltipData data, ref string __result)
        {
            if (item is ItemEntityShield shield)
            {
                if (shield.WeaponComponent != null)
                {
                    TooltipData tmp = new TooltipData();
                    string result = Main.Accessors.CallUIUtilityItemFillEnchantmentDescription(shield.WeaponComponent, tmp);
                    if (!string.IsNullOrEmpty(result))
                    {
                        __result += $"<b><align=\"center\">{LocalizedStringBlueprints.ShieldBashLocalized}</align></b>\n";
                        __result += result;
                    }

                    data.Texts[TooltipElement.AttackType] = tmp.Texts[TooltipElement.AttackType];
                    data.Texts[TooltipElement.ProficiencyGroup] = tmp.Texts[TooltipElement.ProficiencyGroup];
                    if (tmp.Texts.ContainsKey(TooltipElement.Qualities) && !string.IsNullOrEmpty(tmp.Texts[TooltipElement.Qualities]))
                    {
                        if (data.Texts.ContainsKey(TooltipElement.Qualities))
                        {
                            data.Texts[TooltipElement.Qualities] += $",  {LocalizedStringBlueprints.ShieldBashLocalized}:  {tmp.Texts[TooltipElement.Qualities]}";
                        }
                        else
                        {
                            data.Texts[TooltipElement.Qualities] = $"{LocalizedStringBlueprints.ShieldBashLocalized}:  {tmp.Texts[TooltipElement.Qualities]}";
                        }
                    }

                    data.Texts[TooltipElement.Damage] = tmp.Texts[TooltipElement.Damage];
                    if (tmp.Texts.ContainsKey(TooltipElement.EquipDamage))
                    {
                        data.Texts[TooltipElement.EquipDamage] = tmp.Texts[TooltipElement.EquipDamage];
                    }
                    if (tmp.Texts.ContainsKey(TooltipElement.PhysicalDamage))
                    {
                        data.Texts[TooltipElement.PhysicalDamage] = tmp.Texts[TooltipElement.PhysicalDamage];
                        data.PhysicalDamage = tmp.PhysicalDamage;
                    }
                    data.Energy = tmp.Energy;
                    data.OtherDamage = tmp.OtherDamage;
                    data.Texts[TooltipElement.Range] = tmp.Texts[TooltipElement.Range];
                    data.Texts[TooltipElement.CriticalHit] = tmp.Texts[TooltipElement.CriticalHit];
                    if (tmp.Texts.ContainsKey(TooltipElement.Enhancement))
                    {
                        data.Texts[TooltipElement.Enhancement] = tmp.Texts[TooltipElement.Enhancement];
                    }
                }

                if (GameHelper.GetItemEnhancementBonus(shield.ArmorComponent) > 0)
                {
                    if (data.Texts.ContainsKey(TooltipElement.Damage))
                    {
                        data.Texts[Enum.GetValues(typeof(TooltipElement)).Cast<TooltipElement>().Max() + 1] = UIUtility.AddSign(GameHelper.GetItemEnhancementBonus(shield.ArmorComponent));
                    }
                    else
                    {
                        data.Texts[TooltipElement.Enhancement] = UIUtility.AddSign(GameHelper.GetItemEnhancementBonus(shield.ArmorComponent));
                    }
                }
            }

            if (data.Texts.ContainsKey(TooltipElement.Qualities))
            {
                data.Texts[TooltipElement.Qualities] = data.Texts[TooltipElement.Qualities].Replace(" ,", ",");
            }
        }
    }
}