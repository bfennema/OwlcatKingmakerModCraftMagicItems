using System.Collections.Generic;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Enums.Damage;

namespace CraftMagicItems.Config
{
    /// <summary>
    ///     Class containing all of the loaded dictionaries of data that are loaded
    ///     from config files or game resources
    /// </summary>
    public class DictionaryData
    {
        /// <summary>Collection of items that are related to spells</summary>
        public readonly Dictionary<UsableItemType, Dictionary<string, List<BlueprintItemEquipment>>> SpellIdToItem;

        /// <summary>Crafting data loaded fron JSON files and its hierarchy</summary>
        public readonly Dictionary<string, List<ItemCraftingData>> SubCraftingData;

        /// <summary>Collection of items matching type of blueprint (shield, armor, weapon)</summary>
        public readonly Dictionary<string, BlueprintItem> TypeToItem;

        /// <summary>Collection of various item blueprints, keyed on enchantment blueprint ID</summary>
        public readonly Dictionary<string, List<BlueprintItemEquipment>> EnchantmentIdToItem;

        /// <summary>Collection of various recipies, keyed on enchantment blueprint ID</summary>
        public readonly Dictionary<string, List<RecipeData>> EnchantmentIdToRecipe;

        /// <summary>Collection of various recipies, keyed on physical material</summary>
        public readonly Dictionary<PhysicalDamageMaterial, List<RecipeData>> MaterialToRecipe;

        /// <summary>Collection of various enchantment costs, keyed on enchantment blueprint ID</summary>
        public readonly Dictionary<string, int> EnchantmentIdToCost;

        /// <summary>Default constructor</summary>
        public DictionaryData()
        {
            SpellIdToItem = new Dictionary<UsableItemType, Dictionary<string, List<BlueprintItemEquipment>>>();
            SubCraftingData = new Dictionary<string, List<ItemCraftingData>>();
            TypeToItem = new Dictionary<string, BlueprintItem>();
            EnchantmentIdToItem = new Dictionary<string, List<BlueprintItemEquipment>>();
            EnchantmentIdToRecipe = new Dictionary<string, List<RecipeData>>();
            MaterialToRecipe = new Dictionary<PhysicalDamageMaterial, List<RecipeData>>();
            EnchantmentIdToCost = new Dictionary<string, int>();
        }
    }
}