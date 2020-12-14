using Kingmaker.Blueprints;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.UI.Tooltip;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(UIUtilityItem), "FillArmorEnchantments")]
    // ReSharper disable once UnusedMember.Local
    public static class UIUtilityItemFillArmorEnchantmentsPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(TooltipData data, ItemEntityShield armor)
        {
            if (armor.IsIdentified)
            {
                foreach (var itemEnchantment in armor.Enchantments)
                {
                    itemEnchantment.Blueprint.CallComponents<AddStatBonusEquipment>(c =>
                    {
                        if (c.Descriptor != ModifierDescriptor.ArmorEnhancement && c.Descriptor != ModifierDescriptor.ShieldEnhancement && !data.StatBonus.ContainsKey(c.Stat))
                        {
                            data.StatBonus.Add(c.Stat, UIUtility.AddSign(c.Value));
                        }
                    });

                    var component = itemEnchantment.Blueprint.GetComponent<AllSavesBonusEquipment>();
                    if (component != null)
                    {
                        StatType[] saves = { StatType.SaveReflex, StatType.SaveWill, StatType.SaveFortitude };
                        foreach (var save in saves)
                        {
                            if (!data.StatBonus.ContainsKey(save))
                            {
                                data.StatBonus.Add(save, UIUtility.AddSign(component.Value));
                            }
                        }
                    }
                }
            }
        }
    }
}