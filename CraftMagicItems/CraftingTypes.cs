using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace CraftMagicItems {
    public enum DataTypeEnum {
        SpellBased,
        RecipeBased
    }

    public enum SlotRestrictionEnum {
        ArmorExceptRobes,
        ArmorOnlyRobes
    }

    public interface ICraftingData {
        // ReSharper disable once UnusedMember.Global
        DataTypeEnum DataType { get; set; }
    }

    public class CraftingBlueprint<T> {
        [JsonIgnore]
        private T m_blueprint;
        public CraftingBlueprint(T blueprint) {
            m_blueprint = blueprint;
        }
        public T Blueprint => m_blueprint;
    }

    public class ItemCraftingData : ICraftingData {
        public DataTypeEnum DataType { get; set; }
        [JsonProperty] public string Name;
        [JsonProperty] public string NameId;
        [JsonProperty] public string ParentNameId;
        [JsonProperty] public string FeatGuid;
        [JsonProperty] public int MinimumCasterLevel;
        [JsonProperty] public bool PrerequisitesMandatory;
        [JsonProperty("NewItemBaseIDs", ItemConverterType = typeof(CraftingBlueprintArrayConverter<BlueprintItemEquipment>))]
        private CraftingBlueprint<BlueprintItemEquipment>[][] m_NewItemBaseIDs;
        [JsonProperty] public int Count;
        [JsonIgnore] private BlueprintItemEquipment[] m_CachedNewItemBaseIDs;
        [JsonIgnore] public BlueprintItemEquipment[] NewItemBaseIDs {
            get {
                if (m_CachedNewItemBaseIDs == null && m_NewItemBaseIDs != null) {
                    List<BlueprintItemEquipment> list = new List<BlueprintItemEquipment>();
                    foreach (var row in m_NewItemBaseIDs) {
                        var tmp = row.FirstOrDefault(blueprint => blueprint.Blueprint != null);
                        if (tmp != null) {
                            list.Add(tmp.Blueprint);
                        }
                    }
                    m_CachedNewItemBaseIDs = list.ToArray();
                }
                return m_CachedNewItemBaseIDs;
            }
        }
    }

    public class SpellBasedItemCraftingData : ItemCraftingData {
        [JsonProperty] [JsonConverter(typeof(StringEnumConverter))]
        public UsableItemType UsableItemType;

        [JsonProperty] public string NamePrefixId;
        [JsonProperty] public int MaxSpellLevel;
        [JsonProperty] public int BaseItemGoldCost;
        [JsonProperty] public int Charges;
    }

    public class RecipeBasedItemCraftingData : ItemCraftingData {
        [JsonProperty] public string[] RecipeFileNames;

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public ItemsFilter.ItemType[] Slots;

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public SlotRestrictionEnum[] SlotRestrictions;
        
        [JsonProperty] public int MundaneBaseDC;
        [JsonProperty] public bool MundaneEnhancementsStackable;

        // Loaded manually from RecipeFileName
        public RecipeData[] Recipes;

        // Built after load
        public Dictionary<string, List<RecipeData>> SubRecipes;
    }

    public enum RecipeCostType {
        LevelSquared,
        EnhancementLevelSquared,
        CasterLevel,
        Flat,
        Mult
    }

    public enum ItemRestrictions {
        Weapon,
        WeaponMelee,
        WeaponRanged,
        WeaponBludgeoning,
        WeaponPiercing,
        WeaponSlashing,
        WeaponNotBludgeoning,
        WeaponNotPiercing,
        WeaponNotSlashing,
        WeaponFinessable,
        WeaponLight,
        WeaponNotLight,
        WeaponMetal,
        WeaponUseAmmunition,
        WeaponNotUseAmmunition,
        WeaponOneHanded,
        WeaponTwoHanded,
        WeaponOversized,
        WeaponNotOversized,
        WeaponDouble,
        WeaponNotDouble,
        Armor,
        ArmorMetal,
        ArmorNotMetal,
        ArmorLight,
        ArmorMedium,
        ArmorHeavy,
        ShieldArmor,
        ShieldWeapon,
        EnhancmentBonus2,
        EnhancmentBonus3,
        EnhancmentBonus4,
        EnhancmentBonus5
    }

    public enum CrafterPrerequisiteType {
        AlignmentLawful,
        AlignmentGood,
        AlignmentChaotic,
        AlignmentEvil,
        FeatureChannelEnergy
    }

    public class RecipeData {
        [JsonProperty] public string Name;
        [JsonProperty] public string NameId;
        [JsonProperty] public string ParentNameId;
        [JsonProperty] public string BonusTypeId;
        [JsonProperty] public string BonusToId;
        [JsonProperty("Enchantments", ItemConverterType = typeof(CraftingBlueprintArrayConverter<BlueprintItemEnchantment>))]
        private CraftingBlueprint<BlueprintItemEnchantment>[][] m_Enchantments;
        [JsonProperty("ResultItem", ItemConverterType = typeof(CraftingBlueprintConverter<BlueprintItem>))]
        private CraftingBlueprint<BlueprintItem>[] m_ResultItem;
        [JsonProperty] public bool EnchantmentsCumulative;
        [JsonProperty] public int CasterLevelStart;
        [JsonProperty] public int CasterLevelMultiplier;
        [JsonProperty] public int BonusMultiplier;
        [JsonProperty] public DiceType BonusDieSize;
        [JsonProperty] public int MundaneDC;
        [JsonProperty] public PhysicalDamageMaterial Material;
        [JsonProperty] public BlueprintAbility[] PrerequisiteSpells;
        [JsonProperty("PrerequisiteFeats", ItemConverterType = typeof(CraftingBlueprintConverter<BlueprintFeature>))]
        private CraftingBlueprint<BlueprintFeature>[] m_PrerequisiteFeats;

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public CrafterPrerequisiteType[] CrafterPrerequisites;
        [JsonProperty] public bool PrerequisitesMandatory;

        [JsonProperty] public bool AnyPrerequisite;

        [JsonProperty] [JsonConverter(typeof(StringEnumConverter))]
        public RecipeCostType CostType;

        [JsonProperty] public int CostFactor;

        [JsonProperty] public int CostAdjustment;

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public ItemsFilter.ItemType[] OnlyForSlots;

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public ItemRestrictions[] Restrictions;

        [JsonProperty] public bool CanApplyToMundaneItem;

        [JsonProperty] public string[] VisualMappings;
        [JsonProperty] public string[] AnimationMappings;
        [JsonProperty] public string[] NameMappings;

        [JsonIgnore] public bool NoEnchantments { get => m_Enchantments == null; }
        [JsonIgnore] private BlueprintItemEnchantment[] m_CachedEnchantments;
        [JsonIgnore] public BlueprintItemEnchantment[] Enchantments {
            get {
                if (m_CachedEnchantments == null) {
                    List<BlueprintItemEnchantment> list = new List<BlueprintItemEnchantment>();
                    if (m_Enchantments != null) {
                        foreach (var row in m_Enchantments) {
                            var tmp = row.FirstOrDefault(blueprint => blueprint.Blueprint != null);
                            if (tmp != null) {
                                list.Add(tmp.Blueprint);
                            }
                        }
                    }
                    m_CachedEnchantments = list.ToArray();
                }
                return m_CachedEnchantments;
            }
        }

        [JsonIgnore] public bool NoResultItem { get => m_ResultItem == null; }
        [JsonIgnore] public BlueprintItem ResultItem {
            get => m_ResultItem.FirstOrDefault(r => r.Blueprint != null)?.Blueprint;
        }

        [JsonIgnore] private BlueprintFeature[] m_CachedPrerequisiteFeats;
        [JsonIgnore] public BlueprintFeature[] PrerequisiteFeats {
            get {
                if (m_CachedPrerequisiteFeats == null && m_PrerequisiteFeats != null) {
                    m_CachedPrerequisiteFeats = m_PrerequisiteFeats.Where(blueprint => blueprint.Blueprint != null).Select(blueprint => blueprint.Blueprint).ToArray();
                    if (m_CachedPrerequisiteFeats.Length == 0) {
                        m_PrerequisiteFeats = null;
                        m_CachedPrerequisiteFeats = null;
                    }
                }
                return m_CachedPrerequisiteFeats;
            }
        }
    }

    public class CraftingBlueprintArrayConverter<T> : JsonConverter where T : BlueprintScriptableObject {
        public override bool CanConvert(Type objectType) {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }
            serializer.Converters.Add(new CraftingBlueprintConverter<T>());
            return serializer.Deserialize<CraftingBlueprint<T>[]>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }

    public class CraftingBlueprintConverter<T> : JsonConverter where T : BlueprintScriptableObject {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(CraftingBlueprint<T>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            string text = (string)reader.Value;
            if (text == null || text == "null") {
                return null;
            }
            return new CraftingBlueprint<T>(ResourcesLibrary.TryGetBlueprint(text) as T);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }

    public class CraftingTypeConverter : CustomCreationConverter<ICraftingData> {
        public override ICraftingData Create(Type objectType) {
            throw new NotImplementedException();
        }

        private ICraftingData Create(JObject jObject) {
            var typeString = (string) jObject.Property("DataType");
            if (!Enum.TryParse<DataTypeEnum>(typeString, out var type)) {
                throw new ApplicationException($"The ItemCraftingData type {typeString} is not supported!");
            }
            switch (type) {
                case DataTypeEnum.SpellBased:
                    return new SpellBasedItemCraftingData();
                case DataTypeEnum.RecipeBased:
                    return new RecipeBasedItemCraftingData();
                default:
                    throw new ApplicationException($"The ItemCraftingData type {typeString} is not supported!");
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            // Load JObject from stream 
            var jObject = JObject.Load(reader);
            // Create target object based on JObject 
            var target = Create(jObject);
            // Populate the object properties 
            serializer.Populate(jObject.CreateReader(), target);
            return target;
        }
    }

    public class CustomLootItem {
        [JsonProperty] public Version AddInVersion;

        [JsonProperty] public string AssetGuid;

        [JsonProperty] public string[] LootTables;
    }
}