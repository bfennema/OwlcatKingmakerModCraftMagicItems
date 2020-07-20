using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CraftMagicItems.Config;
using CraftMagicItems.Constants;
using CraftMagicItems.Localization;
using CraftMagicItems.Patches;
using CraftMagicItems.Patches.Harmony;
using CraftMagicItems.UI;
using CraftMagicItems.UI.Sections;
using CraftMagicItems.UI.UnityModManager;
using Kingmaker;
#if PATCH21
#endif
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.GameModes;
using Kingmaker.Items;
#if !PATCH21
using Kingmaker.Items.Slots;
#endif
using Kingmaker.Kingdom;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.Log;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
#if !PATCH21
using Kingmaker.UnitLogic.ActivatableAbilities;
#endif
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using Newtonsoft.Json;
using UnityEngine;
using UnityModManagerNet;
using Random = System.Random;

namespace CraftMagicItems
{
    public static class Main {
        private const string BondedItemRitual = "bondedItemRitual";

        private const string CustomPriceLabel = "Crafting Cost: ";

        private static readonly WeaponCategory[] AmmunitionWeaponCategories = {
            WeaponCategory.Longbow,
            WeaponCategory.Shortbow,
            WeaponCategory.LightCrossbow,
            WeaponCategory.HeavyCrossbow,
            WeaponCategory.HandCrossbow,
            WeaponCategory.LightRepeatingCrossbow,
            WeaponCategory.HeavyRepeatingCrossbow
        };

        private static readonly ItemsFilter.ItemType[] BondedItemSlots = {
            ItemsFilter.ItemType.Weapon,
            ItemsFilter.ItemType.Ring,
            ItemsFilter.ItemType.Neck
        };

        private enum ItemLocationFilter
        {
            All,
            Avaliable,
            Inventory,
            Stash
        }

        public static readonly MethodPatch[] MethodPatchList =
        {
            new MethodPatch(
                typeof(ResourcesLibrary).GetMethods().Single(m => m.Name == "TryGetBlueprint" && m.ReturnType == typeof(BlueprintScriptableObject)),
                postfix: new HarmonyLib.HarmonyMethod(typeof(CustomBlueprintBuilder.ResourcesLibraryTryGetBlueprintFallbackPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)) { priority = HarmonyLib.Priority.First }),
            new MethodPatch(typeof(ResourcesLibrary).GetMethods().Single(m => m.Name == "TryGetBlueprint" && m.ReturnType == typeof(BlueprintScriptableObject)),
                new HarmonyLib.HarmonyMethod(typeof(CustomBlueprintBuilder.ResourcesLibraryTryGetBlueprintModPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static)),
                new HarmonyLib.HarmonyMethod(typeof(CustomBlueprintBuilder.ResourcesLibraryTryGetBlueprintModPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static))),
            new MethodPatch(
                typeof(Game).GetMethod("OnAreaLoaded", BindingFlags.NonPublic | BindingFlags.Instance),
                postfix: new HarmonyLib.HarmonyMethod(typeof(GameOnAreaLoadedPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static))),
            new MethodPatch(
                typeof(Player).GetMethod("PostLoad"),
                postfix: new HarmonyLib.HarmonyMethod(typeof(PlayerPostLoadPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static))),
        };

        /// <summary>UI selections made within Unity Mod Manager</summary>
        public static Selections Selections;

        /// <summary>Settings that are saved and managed by Unity Mod Manager</summary>
        public static Settings ModSettings;

        /// <summary>Dictionaries that are loaded from configs and such</summary>
        public static DictionaryData LoadedData;

        public static UnityModManager.ModEntry ModEntry;
        public static CraftMagicItemsAccessors Accessors;
#if PATCH21
        public static readonly List<LogItemData> PendingLogItems = new List<LogItemData>();
#else
        public static readonly List<LogDataManager.LogItemData> PendingLogItems = new List<LogDataManager.LogItemData>();
#endif

        public static bool modEnabled = true;
        public static CraftMagicItemsBlueprintPatcher blueprintPatcher;

        public static readonly Dictionary<ItemEntity, CraftingProjectData> ItemUpgradeProjects = new Dictionary<ItemEntity, CraftingProjectData>();
        public static readonly List<CraftingProjectData> ItemCreationProjects = new List<CraftingProjectData>();

        private static readonly Random RandomGenerator = new Random();

        // ReSharper disable once UnusedMember.Local
        private static void Load(UnityModManager.ModEntry modEntry)
        {
            HarmonyPatcher patcher = new HarmonyPatcher(modEntry.Logger.Error);
            try
            {
                Selections = new Selections();
                ModEntry = modEntry;
                ModSettings = UnityModManager.ModSettings.Load<Settings>(modEntry);
                Selections.SelectedIndex[CustomPriceLabel] = Mathf.Abs(ModSettings.CraftingPriceScale - 1f) < 0.001 ? 0 :
                    Mathf.Abs(ModSettings.CraftingPriceScale - 2f) < 0.001 ? 1 : 2;
                modEnabled = modEntry.Active;
                modEntry.OnSaveGUI = OnSaveGui;
                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGui;
                CustomBlueprintBuilder.InitialiseBlueprintRegex(CraftMagicItemsBlueprintPatcher.BlueprintRegex);
                patcher.PatchAllOrdered(MethodPatchList);   // Patch the recovery methods first.
                Accessors = new CraftMagicItemsAccessors();
                blueprintPatcher = new CraftMagicItemsBlueprintPatcher(Accessors, modEnabled);
            }
            catch (Exception e)
            {
                modEntry.Logger.Error($"Exception during Load: {e}");
                modEnabled = false;
                CustomBlueprintBuilder.Enabled = false;
                // Unpatch everything except methods involved in recovering a save when mod is disabled.
                patcher.UnpatchAllExcept(MethodPatchList);
                throw;
            }
        }

        private static void OnSaveGui(UnityModManager.ModEntry modEntry) {
            ModSettings.Save(modEntry);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool enabled) {
            modEnabled = enabled;
            CustomBlueprintBuilder.Enabled = enabled;
            MainMenuStartPatch.ModEnabledChanged();
            return true;
        }

        private static void OnGui(UnityModManager.ModEntry modEntry) {
            if (!modEnabled) {
                UmmUiRenderer.RenderLabelRow("The mod is disabled.  Loading saved games with custom items and feats will cause them to revert to regular versions.");
                return;
            }

            try {
                if (Game.Instance == null || Game.Instance.CurrentMode != GameModeType.Default
                    && Game.Instance.CurrentMode != GameModeType.GlobalMap
                    && Game.Instance.CurrentMode != GameModeType.FullScreenUi
                    && Game.Instance.CurrentMode != GameModeType.Pause
                    && Game.Instance.CurrentMode != GameModeType.EscMode
                    && Game.Instance.CurrentMode != GameModeType.Rest
                    && Game.Instance.CurrentMode != GameModeType.Kingdom) {
                    UmmUiRenderer.RenderLabelRow("Item crafting is not available in this game state.");
                    return;
                }

                GUILayout.BeginVertical();

                UmmUiRenderer.RenderLabelRow($"Number of custom Craft Magic Items blueprints loaded: {CustomBlueprintBuilder.CustomBlueprintIDs.Count}");

                GetSelectedCrafter(true);

                //render toggleable views in the main functionality of the mod
                if (UmmUiRenderer.RenderToggleSection("Craft Magic Items", Selections.CurrentSection == OpenSection.CraftMagicItemsSection))
                {
                    Selections.CurrentSection = OpenSection.CraftMagicItemsSection;
                    RenderCraftMagicItemsSection();
                }

                if (UmmUiRenderer.RenderToggleSection("Craft Mundane Items", Selections.CurrentSection == OpenSection.CraftMundaneItemsSection))
                {
                    Selections.CurrentSection = OpenSection.CraftMundaneItemsSection;
                    RenderCraftMundaneItemsSection();
                }

                if (UmmUiRenderer.RenderToggleSection("Work in Progress", Selections.CurrentSection == OpenSection.ProjectsSection))
                {
                    Selections.CurrentSection = OpenSection.ProjectsSection;
                    RenderProjectsSection();
                }

                if (UmmUiRenderer.RenderToggleSection("Feat Reassignment", Selections.CurrentSection == OpenSection.FeatsSection))
                {
                    Selections.CurrentSection = OpenSection.FeatsSection;
                    UserInterfaceEventHandlingLogic.RenderFeatReassignmentSection(FeatReassignmentSectionRendererFactory.GetFeatReassignmentSectionRenderer());
                }

                if (UmmUiRenderer.RenderToggleSection("Cheats", Selections.CurrentSection == OpenSection.CheatsSection))
                {
                    Selections.CurrentSection = OpenSection.CheatsSection;
                    UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(CheatSectionRendererFactory.GetCheatSectionRenderer(), ModSettings, CustomPriceLabel);
                }

                GUILayout.EndVertical();
            }
            catch (Exception e)
            {
                modEntry.Logger.Error($"Error rendering GUI: {e}");
            }
        }

        public static T ReadJsonFile<T>(string fileName, params JsonConverter[] converters) {
            try {
                var serializer = new JsonSerializer();
                foreach (var converter in converters) {
                    serializer.Converters.Add(converter);
                }

                using (var reader = new StreamReader(fileName))
                using (var textReader = new JsonTextReader(reader)) {
                    return serializer.Deserialize<T>(textReader);
                }
            } catch (Exception e) {
                ModEntry.Logger.Warning($"Exception reading JSON data from file {fileName}: {e}");
                throw;
            }
        }

        public static CraftingTimerComponent GetCraftingTimerComponentForCaster(UnitDescriptor caster, bool create = false) {
            // Manually search caster.Buffs rather than using GetFact, because we don't want to TryGetBlueprint if the mod is disabled.
            var timerBuff = caster.Buffs.Enumerable.FirstOrDefault(fact => fact.Blueprint.AssetGuid == CraftMagicItemsBlueprintPatcher.TimerBlueprintGuid);
            if (timerBuff == null) {
                if (!modEnabled) {
                    // The mod is disabled - clean up the timer buff.
                    var baseBlueprintGuid = CraftMagicItemsBlueprintPatcher.TimerBlueprintGuid.Substring(0, CustomBlueprintBuilder.VanillaAssetIdLength);
                    timerBuff = caster.Buffs.Enumerable.FirstOrDefault(fact => fact.Blueprint.AssetGuid == baseBlueprintGuid);
                    if (timerBuff != null) {
                        caster.RemoveFact(timerBuff);
                    }

                    return null;
                }

                if (!create) {
                    return null;
                }

                var timerBlueprint = (BlueprintBuff) ResourcesLibrary.TryGetBlueprint(CraftMagicItemsBlueprintPatcher.TimerBlueprintGuid);
                caster.AddFact<Buff>(timerBlueprint);
                timerBuff = (Buff) caster.GetFact(timerBlueprint);
            }

            return timerBuff.SelectComponents<CraftingTimerComponent>().First();
        }

        public static BondedItemComponent GetBondedItemComponentForCaster(UnitDescriptor caster, bool create = false) {
            // Manually search caster.Buffs rather than using GetFact, because we don't want to TryGetBlueprint if the mod is disabled.
            var bondedItemBuff =
                caster.Buffs.Enumerable.FirstOrDefault(fact => fact.Blueprint.AssetGuid == CraftMagicItemsBlueprintPatcher.BondedItemBuffBlueprintGuid);
            if (bondedItemBuff == null) {
                if (!modEnabled) {
                    // The mod is disabled - clean up the bonded item buff.
                    var baseBlueprintGuid = CraftMagicItemsBlueprintPatcher.TimerBlueprintGuid.Substring(0, CustomBlueprintBuilder.VanillaAssetIdLength);
                    bondedItemBuff = caster.Buffs.Enumerable.FirstOrDefault(fact => fact.Blueprint.AssetGuid == baseBlueprintGuid);
                    if (bondedItemBuff != null) {
                        caster.RemoveFact(bondedItemBuff);
                    }

                    return null;
                }

                if (!create) {
                    return null;
                }

                var timerBlueprint = (BlueprintBuff) ResourcesLibrary.TryGetBlueprint(CraftMagicItemsBlueprintPatcher.BondedItemBuffBlueprintGuid);
                caster.AddFact<Buff>(timerBlueprint);
                bondedItemBuff = (Buff) caster.GetFact(timerBlueprint);
            }

            return bondedItemBuff.SelectComponents<BondedItemComponent>().First();
        }

        /// <summary>Renders the crafting menu (by selected character)</summary>
        private static void RenderCraftMagicItemsSection() {
            var caster = GetSelectedCrafter(false);
            if (caster == null) {
                return;
            }

            //can the character have a bonded item?
            var hasBondedItemFeature =
                caster.Descriptor.Progression.Features.Enumerable.Any(feature => Features.BondedItemFeatures.Contains(feature.Blueprint.AssetGuid));
            var bondedItemData = GetBondedItemComponentForCaster(caster.Descriptor);

            if (!hasBondedItemFeature && bondedItemData && bondedItemData.ownerItem != null) {
                // Cleanup - they've presumably respecced the character.
                GUILayout.BeginHorizontal();
                GUILayout.Label("This character has a bonded item, but does not seem to have the bonded item class feature!",
                    new GUIStyle {normal = {textColor = Color.red}});
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Revert the bonded item and clean up.", GUILayout.ExpandWidth(false))) {
                    UnBondFromCurrentBondedItem(caster);
                    var timerBlueprint = (BlueprintBuff) ResourcesLibrary.TryGetBlueprint(CraftMagicItemsBlueprintPatcher.BondedItemBuffBlueprintGuid);
                    caster.Descriptor.RemoveFact(timerBlueprint);
                }
            }

            //what crafting options are available (which feats are available for the selected character)
            var itemTypes = LoadedData.ItemCraftingData
                .Where(data => data.FeatGuid != null && (ModSettings.IgnoreCraftingFeats || CharacterHasFeat(caster, data.FeatGuid)))
                .ToArray();
            if (!Enumerable.Any(itemTypes) && !hasBondedItemFeature) {
                UmmUiRenderer.RenderLabelRow($"{caster.CharacterName} does not know any crafting feats.");
                return;
            }

            //list the selection items
            var itemTypeNames = itemTypes.Select(data => new L10NString(data.NameId).ToString())
                .PrependConditional(hasBondedItemFeature, new L10NString("craftMagicItems-bonded-object-name")).ToArray();

            //render whatever the user has selected
            var selectedItemTypeIndex = DrawSelectionUserInterfaceElements("Crafting: ", itemTypeNames, 6, ref Selections.SelectedCustomName, false);

            //render options for actual selection
            if (hasBondedItemFeature && selectedItemTypeIndex == 0) {
                RenderBondedItemCrafting(caster);
            }
            else {
                var craftingData = itemTypes[hasBondedItemFeature ? selectedItemTypeIndex - 1 : selectedItemTypeIndex];
                if (craftingData is SpellBasedItemCraftingData spellBased) {
                    RenderSpellBasedCrafting(caster, spellBased);
                } else {
                    RenderRecipeBasedCrafting(caster, craftingData as RecipeBasedItemCraftingData);
                }
            }

            //render current cash
            UmmUiRenderer.RenderLabelRow($"Current Money: {Game.Instance.Player.Money}");
        }

        private static RecipeBasedItemCraftingData GetBondedItemCraftingData(BondedItemComponent bondedComponent) {
            // Find crafting data relevant to the bonded item
            return LoadedData.ItemCraftingData.OfType<RecipeBasedItemCraftingData>()
                .First(data => data.Slots.Contains(bondedComponent.ownerItem.Blueprint.ItemType) && !IsMundaneCraftingData(data));
        }

        private static void UnBondFromCurrentBondedItem(UnitEntityData caster) {
            var bondedComponent = GetBondedItemComponentForCaster(caster.Descriptor);
            if (bondedComponent && bondedComponent.ownerItem != null && bondedComponent.everyoneElseItem != null) {
                var ownerItem = bondedComponent.ownerItem;
                var everyoneElseItem = bondedComponent.everyoneElseItem;
                // Need to set these to null now so the unequipping/equipping below doesn't invoke the automagic item swapping.
                bondedComponent.ownerItem = null;
                bondedComponent.everyoneElseItem = null;
                var holdingSlot = ownerItem.HoldingSlot;
                if (holdingSlot != null && ownerItem != everyoneElseItem) {
                    // Revert the old bonded item to its original form.
                    using (new DisableBattleLog()) {
                        Game.Instance.Player.Inventory.Remove(ownerItem);
                        holdingSlot.InsertItem(everyoneElseItem);
                    }
                }
                // Cancel any upgrading of the old bonded item that was in progress.
                if (ItemUpgradeProjects.ContainsKey(ownerItem)) {
                    CancelCraftingProject(ItemUpgradeProjects[ownerItem]);
                }
            }
        }

        private static void BondWithObject(UnitEntityData caster, ItemEntity item) {
            UnBondFromCurrentBondedItem(caster);
            var bondedComponent = GetBondedItemComponentForCaster(caster.Descriptor, true);
            bondedComponent.ownerItem = item;
            bondedComponent.everyoneElseItem = item;
            // Cancel any pending crafting projects by other characters for the new bonded item.
            if (ItemUpgradeProjects.ContainsKey(item) && ItemUpgradeProjects[item].Crafter != caster) {
                CancelCraftingProject(ItemUpgradeProjects[item]);
            }
        }

        private static void RenderBondedItemCrafting(UnitEntityData caster) {
            // Check if the caster is performing a bonded item ritual.
            var projects = GetCraftingTimerComponentForCaster(caster.Descriptor);
            var ritualProject = projects == null ? null : projects.CraftingProjects.FirstOrDefault(project => project.ItemType == BondedItemRitual);
            if (ritualProject != null) {
                UmmUiRenderer.RenderLabelRow($"{caster.CharacterName} is in the process of bonding with {ritualProject.ResultItem.Name}");
                return;
            }
            var bondedComponent = GetBondedItemComponentForCaster(caster.Descriptor);
            var characterCasterLevel = CharacterCasterLevel(caster.Descriptor);
            if (bondedComponent == null || bondedComponent.ownerItem == null || Selections.SelectedBondWithNewObject) {
                if (Selections.SelectedBondWithNewObject) {
                    UmmUiRenderer.RenderLabelRow("You may bond with a different object by performing a special ritual that costs 200 gp per caster level. This ritual takes 8 " +
                                "hours to complete. Items replaced in this way do not possess any of the additional enchantments of the previous bonded item, " +
                                "and the previous bonded item loses any enchantments you added via your bond.");
                    if (GUILayout.Button("Cancel bonding to a new object")) {
                        Selections.SelectedBondWithNewObject = false;
                    }
                }
                UmmUiRenderer.RenderLabelRow(
                    "You can enchant additional magic abilities to your bonded item as if you had the required Item Creation Feat, as long as you also " +
                    "meet the caster level prerequisite of the feat.  Abilities added in this fashion function only for you, and no-one else can add " +
                    "enchantments to your bonded item.");
                UmmUiRenderer.RenderLabelRow(new L10NString("craftMagicItems-bonded-item-glossary"));
                UmmUiRenderer.RenderLabelRow("Choose your bonded item.");
                var names = BondedItemSlots.Select(slot => new L10NString(GetSlotStringKey(slot, null)).ToString()).ToArray();
                var selectedItemSlotIndex = DrawSelectionUserInterfaceElements("Item type", names, 10);
                var selectedSlot = BondedItemSlots[selectedItemSlotIndex];
                var items = Game.Instance.Player.Inventory
                    .Where(item => item.Blueprint is BlueprintItemEquipment blueprint
                                   && DoesBlueprintMatchSlot(blueprint, selectedSlot)
                                   && CanEnchant(item)
                                   && CanRemove(item)
                                   && (bondedComponent == null || (bondedComponent.ownerItem != item && bondedComponent.everyoneElseItem != item))
                                   && item.Wielder == caster.Descriptor)
                    .OrderBy(item => item.Name)
                    .ToArray();
                if (items.Length == 0) {
                    UmmUiRenderer.RenderLabelRow("You do not have any item of that type currently equipped.");
                    return;
                }
                var itemNames = items.Select(item => item.Name).ToArray();
                var selectedUpgradeItemIndex = DrawSelectionUserInterfaceElements("Item: ", itemNames, 5);
                var selectedItem = items[selectedUpgradeItemIndex];
                var goldCost = !Selections.SelectedBondWithNewObject || ModSettings.CraftingCostsNoGold ? 0 : 200 * characterCasterLevel;
                var canAfford = BuildCostString(out var cost, null, goldCost);
                var label = $"Make {selectedItem.Name} your bonded item{(goldCost == 0 ? "" : " for " + cost)}";
                if (!canAfford) {
                    UmmUiRenderer.RenderLabelRow(label);
                } else if (GUILayout.Button(label)) {
                    if (goldCost > 0) {
                        Game.Instance.UI.Common.UISound.Play(UISoundType.LootCollectGold);
                        Game.Instance.Player.SpendMoney(goldCost);
                    }
                    if (Selections.SelectedBondWithNewObject) {
                        Selections.SelectedBondWithNewObject = false;
                        if (!ModSettings.CraftingTakesNoTime) {
                            // Create project
                            AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-begin-ritual-bonded-item", cost, selectedItem.Name));
                            var project = new CraftingProjectData(caster, ModSettings.MagicCraftingRate, goldCost, 0, selectedItem, BondedItemRitual);
                            AddNewProject(caster.Descriptor, project);
                            CalculateProjectEstimate(project);
                            Selections.CurrentSection = OpenSection.ProjectsSection;
                            return;
                        }
                    }
                    BondWithObject(caster, selectedItem);
                }
            } else {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>Your bonded item</b>: {bondedComponent.ownerItem.Name}");
                if (GUILayout.Button("Bond with a different item", GUILayout.ExpandWidth(false))) {
                    Selections.SelectedBondWithNewObject = true;
                }
                GUILayout.EndHorizontal();
                var craftingData = GetBondedItemCraftingData(bondedComponent);
                if (bondedComponent.ownerItem.Wielder != null && !IsPlayerInCapital()
                                                              && !Game.Instance.Player.PartyCharacters.Contains(bondedComponent.ownerItem.Wielder.Unit)) {
                    UmmUiRenderer.RenderLabelRow($"You cannot enchant {bondedComponent.ownerItem.Name} because you cannot currently access it.");
                } else if (!ModSettings.IgnoreFeatCasterLevelRestriction && characterCasterLevel < craftingData.MinimumCasterLevel) {
                    UmmUiRenderer.RenderLabelRow($"You will not be able to enchant your bonded item until your caster level reaches {craftingData.MinimumCasterLevel} " +
                                $"(currently {characterCasterLevel}).");
                } else {
                    RenderRecipeBasedCrafting(caster, craftingData, bondedComponent.ownerItem);
                }
            }
        }

        private static void RenderSpellBasedCrafting(UnitEntityData caster, SpellBasedItemCraftingData craftingData) {
            var spellbooks = caster.Descriptor.Spellbooks.Where(book => book.CasterLevel > 0).ToList();
            if (spellbooks.Count == 0) {
                UmmUiRenderer.RenderLabelRow($"{caster.CharacterName} is not yet able to cast spells.");
                return;
            }

            var selectedSpellbookIndex = 0;
            if (spellbooks.Count != 1) {
                var spellBookNames = spellbooks.Select(book => book.Blueprint.Name.ToString()).ToArray();
                selectedSpellbookIndex = DrawSelectionUserInterfaceElements("Class: ", spellBookNames, 10);
            }

            var spellbook = spellbooks[selectedSpellbookIndex];
            var maxLevel = Math.Min(spellbook.MaxSpellLevel, craftingData.MaxSpellLevel);
            var spellLevelNames = Enumerable.Range(0, maxLevel + 1).Select(index => $"Level {index}").ToArray();
            var spellLevel = DrawSelectionUserInterfaceElements("Spell level: ", spellLevelNames, 10);
            if (spellLevel > 0 && !spellbook.Blueprint.Spontaneous) {
                if (ModSettings.CraftingTakesNoTime) {
                    Selections.SelectedShowPreparedSpells = true;
                } else {
                    GUILayout.BeginHorizontal();
                    Selections.SelectedShowPreparedSpells = GUILayout.Toggle(Selections.SelectedShowPreparedSpells, " Show prepared spells only");
                    GUILayout.EndHorizontal();
                }
            } else {
                Selections.SelectedShowPreparedSpells = false;
            }

            List<AbilityData> spellOptions;
            if (Selections.SelectedShowPreparedSpells) {
                // Prepared spellcaster
                spellOptions = spellbook.GetMemorizedSpells(spellLevel).Where(slot => slot.Available).Select(slot => slot.Spell).ToList();
            } else {
                // Cantrips/Orisons or Spontaneous spellcaster or showing all known spells
                if (spellLevel > 0 && spellbook.Blueprint.Spontaneous) {
                    var spontaneousSlots = spellbook.GetSpontaneousSlots(spellLevel);
                    UmmUiRenderer.RenderLabelRow($"{caster.CharacterName} can cast {spontaneousSlots} more level {spellLevel} spells today.");
                    if (spontaneousSlots == 0 && ModSettings.CraftingTakesNoTime) {
                        return;
                    }
                }

                spellOptions = spellbook.GetSpecialSpells(spellLevel).Concat(spellbook.GetKnownSpells(spellLevel)).ToList();
            }

            if (!spellOptions.Any()) {
                UmmUiRenderer.RenderLabelRow($"{caster.CharacterName} does not know any level {spellLevel} spells.");
            } else {
                var minCasterLevel = Math.Max(1, 2 * spellLevel - 1);
                var maxCasterLevel = CharacterCasterLevel(caster.Descriptor, spellbook);
                if (minCasterLevel < maxCasterLevel) {
                    Selections.SelectedCasterLevel = UmmUiRenderer.RenderIntSlider("Caster level: ", Selections.SelectedCasterLevel, minCasterLevel, maxCasterLevel);
                } else {
                    Selections.SelectedCasterLevel = minCasterLevel;
                    UmmUiRenderer.RenderLabelRow($"Caster level: {Selections.SelectedCasterLevel}");
                }

                RenderCraftingSkillInformation(caster, StatType.SkillKnowledgeArcana, 5 + Selections.SelectedCasterLevel, Selections.SelectedCasterLevel);

                if (Selections.SelectedShowPreparedSpells && spellbook.GetSpontaneousConversionSpells(spellLevel).Any()) {
                    var firstSpell = spellbook.Blueprint.Spontaneous
                        ? spellbook.GetKnownSpells(spellLevel).First(spell => true)
                        : spellbook.GetMemorizedSpells(spellLevel).FirstOrDefault(slot => slot.Available)?.Spell;
                    if (firstSpell != null) {
                        foreach (var spontaneousBlueprint in spellbook.GetSpontaneousConversionSpells(spellLevel)) {
                            // Only add spontaneous spells that aren't already in the list.
                            if (!spellOptions.Any(spell => spell.Blueprint == spontaneousBlueprint)) {
                                spellOptions.Add(new AbilityData(firstSpell, spontaneousBlueprint));
                            }
                        }
                    }
                }

                foreach (var spell in spellOptions.OrderBy(spell => spell.Name).GroupBy(spell => spell.Name).Select(group => group.First()))
                {
                    if (spell.MetamagicData != null && spell.MetamagicData.NotEmpty)
                    {
                        GUILayout.Label($"Cannot craft {new L10NString(craftingData.NameId)} of {spell.Name} with metamagic applied.");
                    }
                    else if (spell.Blueprint.Variants != null)
                    {
                        // Spells with choices (e.g. Protection from Alignment, which can be Protection from Evil, Good, Chaos or Law)
                        foreach (var variant in spell.Blueprint.Variants)
                        {
                            AttemptSpellBasedCraftItemAndRender(caster, craftingData, spell, variant, spellLevel, Selections.SelectedCasterLevel);
                        }
                    }
                    else
                    {
                        AttemptSpellBasedCraftItemAndRender(caster, craftingData, spell, spell.Blueprint, spellLevel, Selections.SelectedCasterLevel);
                    }
                }
            }
        }

        private static string GetSlotStringKey(ItemsFilter.ItemType slot, SlotRestrictionEnum[] restrictions) {
            switch (slot) {
                case ItemsFilter.ItemType.Weapon:
                    return "e5e94f49-4bf6-4813-b4d7-8e4e9ede3d11";
                case ItemsFilter.ItemType.Shield:
                    return "dfa95469-ed91-4fc6-b5ef-89a466c50d71";
                case ItemsFilter.ItemType.Armor:
                    return (restrictions != null && restrictions.Contains(SlotRestrictionEnum.ArmorOnlyRobes)) ?
                        "craftMagicItems-item-category-name-robe" : "b43922c2-5435-45eb-bdf9-6e33e6bef0ae";
                case ItemsFilter.ItemType.Ring:
                    return "04d0daf3-ba89-44d5-8b6e-84b544e6748d";
                case ItemsFilter.ItemType.Belt:
                    return "ec07d8b6-9fca-4ba2-82b6-053e84ca9875";
                case ItemsFilter.ItemType.Feet:
                    return "1ea53023-2fd8-4fd7-a5ca-99cbe0d91728";
                case ItemsFilter.ItemType.Gloves:
                    return "628bab11-aeaf-449d-859e-3ccfeb25ebeb";
                case ItemsFilter.ItemType.Head:
                    return "45aa1b41-2392-4bc5-8e9b-400c5926cfce";
                case ItemsFilter.ItemType.Neck:
                    return "71cc03f0-aeb4-4c0b-b2da-9913b9cab8db";
                case ItemsFilter.ItemType.Shoulders:
                    return "823f1224-8a46-4a58-bcd6-2cce97cc1912";
                case ItemsFilter.ItemType.Wrist:
                    return "e43de05a-754c-4fa4-991d-0d33fcf1c767";
                case ItemsFilter.ItemType.Usable:
                    return "6f22a0fb-f0d5-47c2-aa03-a6c299e85251";
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
            }
        }

        public static IEnumerable<BlueprintItemEnchantment> GetEnchantments(BlueprintItem blueprint, RecipeData sourceRecipe = null) {
            if (blueprint is BlueprintItemShield shield) {
                // A shield can be treated as armor or as a weapon... assume armor unless being used by a recipe which applies to weapons.
                var weaponRecipe = sourceRecipe?.OnlyForSlots?.Contains(ItemsFilter.ItemType.Weapon) ?? false;
                return weaponRecipe
                    ? shield.WeaponComponent != null ? shield.WeaponComponent.Enchantments : Enumerable.Empty<BlueprintItemEnchantment>()
                    : shield.ArmorComponent.Enchantments;
            }

            return blueprint.Enchantments;
        }

        public static ItemsFilter.ItemType GetItemType(BlueprintItem blueprint) {
            return (blueprint is BlueprintItemArmor armor && armor.IsShield
                    || blueprint is BlueprintItemWeapon weapon && (
                        weapon.Category == WeaponCategory.WeaponLightShield
                        || weapon.Category == WeaponCategory.WeaponHeavyShield
                        || weapon.Category == WeaponCategory.SpikedLightShield
                        || weapon.Category == WeaponCategory.SpikedHeavyShield))
                ? ItemsFilter.ItemType.Shield
                : blueprint.ItemType;
        }

        public static BlueprintItem GetBaseBlueprint(BlueprintItem blueprint) {
            if (blueprint != null) {
                var assetGuid = CustomBlueprintBuilder.AssetGuidWithoutMatch(blueprint.AssetGuid);
                return ResourcesLibrary.TryGetBlueprint<BlueprintItem>(assetGuid);
            } else {
                return null;
            }
        }

        private static string GetBlueprintItemType(BlueprintItem blueprint) {
            string assetGuid = null;
            switch (blueprint) {
                case BlueprintItemArmor armor:   assetGuid = armor.Type.AssetGuid;  break;
                case BlueprintItemShield shield: assetGuid = shield.WeaponComponent != null ? shield.WeaponComponent.Type.AssetGuid : shield.ArmorComponent.Type.AssetGuid; break;
                case BlueprintItemWeapon weapon: assetGuid = weapon.Type.AssetGuid; break;
            }
            return assetGuid;
        }

        private static BlueprintItem GetStandardItem(BlueprintItem blueprint) {
            string assetGuid = GetBlueprintItemType(blueprint);
            return !string.IsNullOrEmpty(assetGuid) && LoadedData.TypeToItem.ContainsKey(assetGuid) ? LoadedData.TypeToItem[assetGuid] : null;
        }

        public static RecipeData FindSourceRecipe(string selectedEnchantmentId, BlueprintItem blueprint) {
            List<RecipeData> recipes = null;
            if (LoadedData.EnchantmentIdToRecipe.ContainsKey(selectedEnchantmentId)) {
                recipes = LoadedData.EnchantmentIdToRecipe[selectedEnchantmentId];
            } else {
                foreach (var material in blueprintPatcher.PhysicalDamageMaterialEnchantments.Keys) {
                    if (blueprintPatcher.PhysicalDamageMaterialEnchantments[material] == selectedEnchantmentId) {
                        recipes = LoadedData.MaterialToRecipe[material];
                    }
                }
            }
            if (recipes == null) {
                return null;
            }
            var slot = GetItemType(blueprint);
            return recipes.FirstOrDefault(recipe => (recipe.OnlyForSlots == null || recipe.OnlyForSlots.Contains(slot))
                                                    && (blueprint == null || RecipeAppliesToBlueprint(recipe, blueprint, true)));
        }

        private static string FindSupersededEnchantmentId(BlueprintItem blueprint, string selectedEnchantmentId) {
            if (blueprint != null) {
                var selectedRecipe = FindSourceRecipe(selectedEnchantmentId, blueprint);
                foreach (var enchantment in GetEnchantments(blueprint, selectedRecipe)) {
                    if (FindSourceRecipe(enchantment.AssetGuid, blueprint) == selectedRecipe) {
                        return enchantment.AssetGuid;
                    }
                }

                // Special case - enchanting a masterwork item supersedes the masterwork quality
                if (IsMasterwork(blueprint)) {
                    return ItemQualityBlueprints.MasterworkGuid;
                }
            }

            return null;
        }

        public static bool DoesItemMatchAllEnchantments(BlueprintItemEquipment blueprint, string selectedEnchantmentId,
            string selectedEnchantmentIdSecond = null, BlueprintItemEquipment upgradeItem = null, bool checkPrice = false) {
            var isNotable = upgradeItem && upgradeItem.IsNotable;
            var ability = upgradeItem ? upgradeItem.Ability : null;
            var activatableAbility = upgradeItem ? upgradeItem.ActivatableAbility : null;
            // If item is notable or has an ability that upgradeItem does not, it's not a match.
            if (blueprint.IsNotable != isNotable || blueprint.Ability != ability || blueprint.ActivatableAbility != activatableAbility) {
                return false;
            }

            var supersededEnchantmentId = string.IsNullOrEmpty(selectedEnchantmentId) ? null : FindSupersededEnchantmentId(upgradeItem, selectedEnchantmentId);
            var enchantmentCount = (upgradeItem ? GetEnchantments(upgradeItem).Count() : 0) + (selectedEnchantmentId == null ? 0 : supersededEnchantmentId == null ? 1 : 0);
            if (GetEnchantments(blueprint).Count() != enchantmentCount) {
                return false;
            }

            if (GetEnchantments(blueprint).Any(enchantment => enchantment.AssetGuid != selectedEnchantmentId
                                                              && enchantment.AssetGuid != supersededEnchantmentId
                                                              && (!upgradeItem || !GetEnchantments(upgradeItem).Contains(enchantment)))) {
                return false;
            }

            if (upgradeItem != null) {
                // If upgradeItem is armor or a shield or a weapon, item is not a match if it's not the same type of armor/shield/weapon
                switch (upgradeItem) {
                    case BlueprintItemArmor upgradeItemArmor when !(blueprint is BlueprintItemArmor itemArmor) || itemArmor.Type != upgradeItemArmor.Type:
                    case BlueprintItemShield upgradeItemShield when !(blueprint is BlueprintItemShield itemShield) || itemShield.Type != upgradeItemShield.Type:
                    case BlueprintItemWeapon upgradeItemWeapon when !(blueprint is BlueprintItemWeapon itemWeapon) || itemWeapon.Type != upgradeItemWeapon.Type
                                                                                                                   || itemWeapon.DamageType.Physical.Material !=
                                                                                                                   upgradeItemWeapon.DamageType.Physical
                                                                                                                       .Material:
                        return false;
                }

                // Special handler for heavy shield, because the game data has some very messed up shields in it.
                if (blueprint.AssetGuid == "6989ca8e0d28af643b908468ead16922") {
                    return false;
                }

                if (blueprint is BlueprintItemWeapon blueprintWeapon && upgradeItem is BlueprintItemWeapon upgradeWeapon) {
                    if ((blueprintWeapon.Double && !upgradeWeapon.Double) || (!blueprintWeapon.Double && upgradeWeapon.Double)) {
                        return false;
                    } else if (blueprintWeapon.Double && upgradeWeapon.Double) {
                        if (!DoesItemMatchAllEnchantments(blueprintWeapon.SecondWeapon, selectedEnchantmentIdSecond, null, upgradeWeapon.SecondWeapon, false)) {
                            return false;
                        }
                    }
                } else if (blueprint is BlueprintItemShield blueprintShield && upgradeItem is BlueprintItemShield upgradeShield) {
                    if ((blueprintShield.WeaponComponent != null && upgradeShield.WeaponComponent == null)
                        || (blueprintShield.WeaponComponent == null && upgradeShield.WeaponComponent != null)
                        || (blueprintShield.WeaponComponent != null && blueprintShield.WeaponComponent.IsDamageDiceOverridden)) {
                        return false;
                    } else if (blueprintShield.WeaponComponent != null && upgradeShield.WeaponComponent != null) {
                        if (!DoesItemMatchAllEnchantments(blueprintShield.WeaponComponent, selectedEnchantmentIdSecond, null, upgradeShield.WeaponComponent, false)) {
                            return false;
                        }
                    }
                }
            }

            // Verify the price of the vanilla item
            return !checkPrice || RulesRecipeItemCost(blueprint) == blueprint.Cost;
        }

        private static IEnumerable<T> PrependConditional<T>(this IEnumerable<T> target, bool prepend, params T[] items) {
            return prepend ? items.Concat(target ?? throw new ArgumentException(nameof(target))) : target;
        }

        private static string Join<T>(this IEnumerable<T> enumeration, string delimiter = ", ") {
            return enumeration.Aggregate("", (prev, curr) => prev + (prev != "" ? delimiter : "") + curr.ToString());
        }

        private static string BuildCommaList(this IEnumerable<string> list, bool or) {
            var array = list.ToArray();
            if (array.Length < 2) {
                return array.Join();
            }

            var commaList = "";
            for (var index = 0; index < array.Length - 1; ++index) {
                if (index > 0) {
                    commaList += ", " + array[index];
                } else {
                    commaList += array[index];
                }
            }

            var key = or ? "craftMagicItems-logMessage-comma-list-or" : "craftMagicItems-logMessage-comma-list-and";
            return LocalizationHelper.FormatLocalizedString(key, commaList, array[array.Length - 1]);
        }

        private static bool IsMasterwork(BlueprintItem blueprint) {
            return GetEnchantments(blueprint).Any(enchantment => enchantment.AssetGuid == ItemQualityBlueprints.MasterworkGuid);
        }

        public static bool IsOversized(BlueprintItem blueprint) {
            return GetEnchantments(blueprint).Any(enchantment => enchantment.AssetGuid.StartsWith(ItemQualityBlueprints.OversizedGuid) && !enchantment.GetComponent<Patches.Harmony.WeaponSizeChange>());
        }

        // Use instead of UIUtility.IsMagicItem.
        private static bool IsEnchanted(BlueprintItem blueprint, RecipeData recipe = null) {
            if (blueprint == null) {
                return false;
            }

            switch (blueprint) {
                case BlueprintItemArmor armor:
                    return ItemPlusEquivalent(armor) > 0 || !armor.IsArmor;
                case BlueprintItemWeapon weapon:
                    return ItemPlusEquivalent(weapon) > 0;
                case BlueprintItemShield shield:
                    var isWeaponEnchantmentRecipe = recipe?.OnlyForSlots?.Contains(ItemsFilter.ItemType.Weapon) ?? false;
                    return !isWeaponEnchantmentRecipe && ItemPlusEquivalent(shield.ArmorComponent) > 0
                           || isWeaponEnchantmentRecipe && ItemPlusEquivalent(shield.WeaponComponent) > 0;
                case BlueprintItemEquipmentUsable usable:
                    return !usable.SpendCharges || usable.RestoreChargesOnRest;
                case BlueprintItemEquipment equipment:
                    return GetEnchantments(blueprint).Any() || equipment.Ability != null || equipment.ActivatableAbility != null;
                default:
                    return GetEnchantments(blueprint).Any();
            }
        }

        private static bool CanEnchant(ItemEntity item) {
            // The game has no masterwork armor or shields, so I guess you can enchant any of them.
            return IsEnchanted(item.Blueprint)
                   || item.Blueprint is BlueprintItemArmor
                   || item.Blueprint is BlueprintItemShield
                   || IsMasterwork(item.Blueprint);
        }

        private static bool CanRemove(ItemEntity item) {
            return !(item.Blueprint is BlueprintItemEquipment blueprint && blueprint.IsNonRemovable)
                && (item.HoldingSlot == null || item.HoldingSlot.Lock.Count == 0);
        }

        private static bool IsMetalArmor(BlueprintArmorType armorType) {
            // Rely on the fact that the only light armor that is metal is a Chain Shirt, and the only medium armor that is not metal is Hide.
            return armorType.ProficiencyGroup == ArmorProficiencyGroup.Light && armorType.AssetGuid == "7467b0ab8641d8f43af7fc46f2108a1a"
                   || armorType.ProficiencyGroup == ArmorProficiencyGroup.Medium && armorType.AssetGuid != "7a01292cef39bf2408f7fae7a9f47594"
                   || armorType.ProficiencyGroup == ArmorProficiencyGroup.Heavy;
        }

        private static bool IsActuallyTwoHanded(BlueprintItemWeapon weapon) {
            if (weapon.Category == WeaponCategory.BastardSword
             || weapon.Category == WeaponCategory.DwarvenWaraxe
             || weapon.Category == WeaponCategory.Estoc
             || weapon.Category == WeaponCategory.DuelingSword) {
                return false;
            }
            return weapon.IsTwoHanded;
        }

        private static bool ItemMatchesRestrictions(BlueprintItem blueprint, IEnumerable<ItemRestrictions> restrictions) {
            if (restrictions != null) {
                var shield = blueprint as BlueprintItemShield;
                var weapon = shield?.WeaponComponent ?? blueprint as BlueprintItemWeapon;
                var armor = shield?.ArmorComponent ?? blueprint as BlueprintItemArmor;
                foreach (var restriction in restrictions) {
                    switch (restriction) {
                        case ItemRestrictions.Weapon when weapon == null:
                        case ItemRestrictions.WeaponMelee when weapon == null || weapon.AttackType != AttackType.Melee:
                        case ItemRestrictions.WeaponRanged when weapon == null || weapon.AttackType != AttackType.Ranged:
                        case ItemRestrictions.WeaponBludgeoning when weapon == null || (weapon.DamageType.Physical.Form & PhysicalDamageForm.Bludgeoning) == 0:
                        case ItemRestrictions.WeaponPiercing when weapon == null || (weapon.DamageType.Physical.Form & PhysicalDamageForm.Piercing) == 0:
                        case ItemRestrictions.WeaponSlashing when weapon == null || (weapon.DamageType.Physical.Form & PhysicalDamageForm.Slashing) == 0:
                        case ItemRestrictions.WeaponNotBludgeoning
                            when weapon == null || (weapon.DamageType.Physical.Form & PhysicalDamageForm.Bludgeoning) != 0:
                        case ItemRestrictions.WeaponNotPiercing when weapon == null || (weapon.DamageType.Physical.Form & PhysicalDamageForm.Piercing) != 0:
                        case ItemRestrictions.WeaponNotSlashing when weapon == null || (weapon.DamageType.Physical.Form & PhysicalDamageForm.Slashing) != 0:
                        case ItemRestrictions.WeaponFinessable when weapon == null || !weapon.Category.HasSubCategory(WeaponSubCategory.Finessable):
                        case ItemRestrictions.WeaponLight when weapon == null || !weapon.IsLight:
                        case ItemRestrictions.WeaponNotLight when weapon == null || weapon.IsLight:
                        case ItemRestrictions.WeaponMetal when weapon == null || !weapon.Category.HasSubCategory(WeaponSubCategory.Metal):
                        case ItemRestrictions.WeaponUseAmmunition when weapon == null || !AmmunitionWeaponCategories.Contains(weapon.Category):
                        case ItemRestrictions.WeaponNotUseAmmunition when weapon == null || AmmunitionWeaponCategories.Contains(weapon.Category):
                        case ItemRestrictions.WeaponTwoHanded when weapon == null || !(IsActuallyTwoHanded(weapon) || weapon.IsOneHandedWhichCanBeUsedWithTwoHands):
                        case ItemRestrictions.WeaponOneHanded when weapon == null || IsActuallyTwoHanded(weapon) || weapon.Double:
                        case ItemRestrictions.WeaponOversized when weapon == null || !IsOversized(weapon):
                        case ItemRestrictions.WeaponNotOversized when weapon == null || IsOversized(weapon):
                        case ItemRestrictions.WeaponDouble when weapon == null || !weapon.Double:
                        case ItemRestrictions.WeaponNotDouble when weapon == null || weapon.Double:
                        case ItemRestrictions.Armor when armor == null:
                        case ItemRestrictions.ArmorMetal when armor == null || !IsMetalArmor(armor.Type):
                        case ItemRestrictions.ArmorNotMetal when armor == null || IsMetalArmor(armor.Type):
                        case ItemRestrictions.ArmorLight when armor == null || armor.Type.ProficiencyGroup != ArmorProficiencyGroup.Light:
                        case ItemRestrictions.ArmorMedium when armor == null || armor.Type.ProficiencyGroup != ArmorProficiencyGroup.Medium:
                        case ItemRestrictions.ArmorHeavy when armor == null || armor.Type.ProficiencyGroup != ArmorProficiencyGroup.Heavy:
                        case ItemRestrictions.ShieldArmor when armor == null:
                        case ItemRestrictions.ShieldWeapon when weapon == null:
                        case ItemRestrictions.EnhancmentBonus2 when ItemPlus(blueprint) < 2:
                        case ItemRestrictions.EnhancmentBonus3 when ItemPlus(blueprint) < 3:
                        case ItemRestrictions.EnhancmentBonus4 when ItemPlus(blueprint) < 4:
                        case ItemRestrictions.EnhancmentBonus5 when ItemPlus(blueprint) < 5:
                            return false;
                    }
                }
            }

            return true;
        }

        private static bool RecipeAppliesToBlueprint(RecipeData recipe, BlueprintItem blueprint, bool skipEnchantedCheck = false, bool skipMaterialCheck = false) {
            var weapon = (blueprint as BlueprintItemShield)?.WeaponComponent ?? blueprint as BlueprintItemWeapon;
            return blueprint == null
                   || (skipEnchantedCheck || recipe.CanApplyToMundaneItem || IsEnchanted(blueprint, recipe))
                   && recipe.ResultItem == null
                   && ItemMatchesRestrictions(blueprint, recipe.Restrictions)
                   // Weapons with special materials can't apply recipes which apply different special materials
                   && (!weapon || recipe.Material == 0 || weapon.DamageType.Physical.Material == 0 || (!skipMaterialCheck && recipe.Material == weapon.DamageType.Physical.Material))
                   // Shields make this complicated.  A shield's armor component can match a recipe which is for shields but not weapons.
                   && (recipe.OnlyForSlots == null || recipe.OnlyForSlots.Contains(blueprint.ItemType)
                                                   || (blueprint is BlueprintItemArmor  && GetItemType(blueprint) == ItemsFilter.ItemType.Shield
                                                                                        && recipe.OnlyForSlots.Contains(ItemsFilter.ItemType.Shield)
                                                                                        && !recipe.OnlyForSlots.Contains(ItemsFilter.ItemType.Weapon))
                                                   || (blueprint is BlueprintItemWeapon && GetItemType(blueprint) == ItemsFilter.ItemType.Shield
                                                                                        && recipe.OnlyForSlots.Contains(ItemsFilter.ItemType.Shield)
                                                                                        && !recipe.OnlyForSlots.Contains(ItemsFilter.ItemType.Armor)))
                   // ... also, a top-level shield object should not match a weapon recipe if it has no weapon component.
                   && !(recipe.OnlyForSlots != null && blueprint is BlueprintItemShield shield
                                                    && shield.WeaponComponent == null
                                                    && recipe.OnlyForSlots.Contains(ItemsFilter.ItemType.Weapon))
                ;
        }

        public static ItemEntity BuildItemEntity(BlueprintItem blueprint, ItemCraftingData craftingData, UnitEntityData crafter) {
            var item = blueprint.CreateEntity();
            item.Identify();
            item.SetVendorIfNull(crafter);
            if (craftingData is SpellBasedItemCraftingData spellBased) {
                item.Charges = spellBased.Charges; // Set the charges, since wand blueprints have random values.
            }

            if (item is ItemEntityShield shield && item.IsIdentified) {
                shield.ArmorComponent.Identify();
                shield.WeaponComponent?.Identify();
            }

            item.PostLoad();
            if (craftingData.Count != 0) {
                item.SetCount(craftingData.Count);
            }
            return item;
        }

        private static bool DoesBlueprintMatchSlot(BlueprintItemEquipment blueprint, ItemsFilter.ItemType slot) {
            return blueprint.ItemType == slot || slot == ItemsFilter.ItemType.Usable && blueprint is BlueprintItemEquipmentUsable;
        }

        private static bool DoesBlueprintMatchRestrictions(BlueprintItemEquipment blueprint, ItemsFilter.ItemType slot, SlotRestrictionEnum[] restrictions) {
            if (restrictions != null) {
                foreach (var restriction in restrictions) {
                    switch (restriction) {
                        case SlotRestrictionEnum.ArmorOnlyRobes:
                        case SlotRestrictionEnum.ArmorExceptRobes:
                            if (slot == ItemsFilter.ItemType.Armor
                                && blueprint is BlueprintItemArmor armor
                                && armor.IsArmor == (restriction == SlotRestrictionEnum.ArmorOnlyRobes)) {
                                return false;
                            }
                            break;
                    }
                }
            }
            return true;
        }

        private static string GetBonusString(int bonus, RecipeData recipe) {
            bonus *= recipe.BonusMultiplier == 0 ? 1 : recipe.BonusMultiplier;
            return recipe.BonusDieSize != 0 ? new DiceFormula(bonus, recipe.BonusDieSize).ToString() : bonus.ToString();
        }

        private static bool IsAnotherCastersBondedItem(ItemEntity item, UnitEntityData caster) {
            var otherCharacters = UIUtility.GetGroup(true).Where(character => character != caster && character.IsPlayerFaction && !character.Descriptor.IsPet);
            return otherCharacters
                .Select(character => GetBondedItemComponentForCaster(character.Descriptor))
                .Any(bondedComponent => bondedComponent != null && (bondedComponent.ownerItem == item || bondedComponent.everyoneElseItem == item));
        }

        private static int GetPlusOfRecipe(RecipeData recipe, int level) {
            return recipe.CostFactor * level + recipe.CostAdjustment;
        }

        private static bool DoesItemMatchLocationFilter(ItemEntity item, UnitEntityData caster, bool playerInCapital, ItemLocationFilter filter) {
            switch (filter) {
                case ItemLocationFilter.All: return true;
                case ItemLocationFilter.Avaliable:
                    if ((!playerInCapital && Game.Instance.Player.Inventory.Contains(item))
                        || (playerInCapital && Game.Instance.Player.SharedStash.Contains(item))
                        || item.Wielder?.Unit == caster
                        || ItemCreationProjects.Count(project => project.Crafter == caster && project.ResultItem == item) > 0) {
                        return true;
                    } else {
                        return false;
                    }
                case ItemLocationFilter.Inventory:
                    if (Game.Instance.Player.Inventory.Contains(item) || item.Wielder?.Unit == caster) {
                        return true;
                    } else {
                        return false;
                    }
                case ItemLocationFilter.Stash:
                    if (Game.Instance.Player.SharedStash.Contains(item) || item.Wielder?.Unit == caster) {
                        return true;
                    } else {
                        return false;
                    }
            }
            return false;
        }

        private static void RenderRecipeBasedCrafting(UnitEntityData caster, RecipeBasedItemCraftingData craftingData, ItemEntity upgradeItem = null) {
            ItemsFilter.ItemType selectedSlot;

            //specific item
            if (upgradeItem != null) {
                selectedSlot = upgradeItem.Blueprint.ItemType;
                while (ItemUpgradeProjects.ContainsKey(upgradeItem)) {
                    upgradeItem = ItemUpgradeProjects[upgradeItem].ResultItem;
                }
                UmmUiRenderer.RenderLabelRow($"Enchanting {upgradeItem.Name}");
            }
            //slot
            else {
                // Choose slot/weapon type.
                var selectedItemSlotIndex = 0;
                if (craftingData.Slots.Length > 1) {
                    var names = craftingData.Slots.Select(slot => new L10NString(GetSlotStringKey(slot, craftingData.SlotRestrictions)).ToString()).ToArray();
                    selectedItemSlotIndex = DrawSelectionUserInterfaceElements("Item type", names, 10, ref Selections.SelectedCustomName);
                }

                var locationFilter = ItemLocationFilter.All;
                var locationNames = Enum.GetNames(typeof(ItemLocationFilter));
                locationFilter = (ItemLocationFilter)DrawSelectionUserInterfaceElements("Item location", locationNames, locationNames.Length, ref Selections.SelectedCustomName);

                selectedSlot = craftingData.Slots[selectedItemSlotIndex];
                var playerInCapital = IsPlayerInCapital();
                // Choose an existing or in-progress item of that type, or create a new one (if allowed).
                var items = Game.Instance.Player.Inventory
                    .Concat(ItemCreationProjects.Select(project => project.ResultItem))
                    .Concat(playerInCapital ? Game.Instance.Player.SharedStash : new ItemsCollection())
                    .Where(item => item.Blueprint is BlueprintItemEquipment blueprint
                                   && DoesItemMatchLocationFilter(item, caster, playerInCapital, locationFilter)
                                   && DoesBlueprintMatchSlot(blueprint, selectedSlot)
                                   && DoesBlueprintMatchRestrictions(blueprint, selectedSlot, craftingData.SlotRestrictions)
                                   && CanEnchant(item)
                                   && !IsAnotherCastersBondedItem(item, caster)
                                   && CanRemove(item)
                                   && (item.Wielder == null
                                       || playerInCapital
                                       || Game.Instance.Player.PartyCharacters.Contains(item.Wielder.Unit)))
                    .Select(item => {
                        while (ItemUpgradeProjects.ContainsKey(item)) {
                            item = ItemUpgradeProjects[item].ResultItem;
                        }

                        return item;
                    })
                    .OrderBy(item => item.Name)
                    .ToArray();
                var canCreateNew = craftingData.NewItemBaseIDs != null;
                var itemNames = items.Select(item => item.Name).PrependConditional(canCreateNew, new L10NString("craftMagicItems-label-craft-new-item"))
                    .ToArray();
                if (itemNames.Length == 0) {
                    UmmUiRenderer.RenderLabelRow($"{caster.CharacterName} can not access any items of that type.");
                    return;
                }

                var selectedUpgradeItemIndex = DrawSelectionUserInterfaceElements("Item: ", itemNames, 5, ref Selections.SelectedCustomName);
                // See existing item details and enchantments.
                var index = selectedUpgradeItemIndex - (canCreateNew ? 1 : 0);
                upgradeItem = index < 0 ? null : items[index];
            }
            var upgradeItemDoubleWeapon = upgradeItem as ItemEntityWeapon;
            var upgradeItemShield = upgradeItem as ItemEntityShield;
            var upgradeItemShieldWeapon = upgradeItemShield?.WeaponComponent;
            var upgradeItemShieldArmor = upgradeItemShield?.ArmorComponent;
            if (upgradeItem != null) {
                if (upgradeItemDoubleWeapon != null && upgradeItemDoubleWeapon.Blueprint.Double) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{upgradeItem.Name} is a double weapon; enchanting ", GUILayout.ExpandWidth(false));
                    var label = Selections.SelectedDoubleWeaponSecondEnd ? "Secondary end" : "Primary end";
                    if (GUILayout.Button(label, GUILayout.ExpandWidth(false))) {
                        Selections.SelectedDoubleWeaponSecondEnd = !Selections.SelectedDoubleWeaponSecondEnd;
                    }
                    if (Selections.SelectedDoubleWeaponSecondEnd) {
                        upgradeItem = upgradeItemDoubleWeapon.Second;
                    } else {
                        upgradeItemDoubleWeapon = null;
                    }
                    GUILayout.EndHorizontal();
                } else {
                    upgradeItemDoubleWeapon = null;
                }
                if (upgradeItemShieldWeapon != null) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{upgradeItem.Name} is a shield; enchanting ", GUILayout.ExpandWidth(false));
                    var label = Selections.SelectedShieldWeapon ? "Shield Bash" : "Shield";
                    if (GUILayout.Button(label, GUILayout.ExpandWidth(false))) {
                        Selections.SelectedShieldWeapon = !Selections.SelectedShieldWeapon;
                    }
                    if (Selections.SelectedShieldWeapon) {
                        upgradeItem = upgradeItemShieldWeapon;
                    } else {
                        upgradeItem = upgradeItemShieldArmor;
                    }
                    GUILayout.EndHorizontal();
                } else {
                    Selections.SelectedShieldWeapon = false;
                }
                if (upgradeItemShield != null) {
                    UmmUiRenderer.RenderLabelRow(BuildItemDescription(upgradeItemShield));
                } else {
                    UmmUiRenderer.RenderLabelRow(BuildItemDescription(upgradeItem));
                }
            }

            // Pick recipe to apply, but make any with the same ParentNameId appear in a second level menu under their parent name.
            var availableRecipes = craftingData.Recipes
                .Where(recipe => recipe.NameId != null
                                 && (recipe.ParentNameId == null || recipe == craftingData.SubRecipes[recipe.ParentNameId][0])
                                 && (recipe.OnlyForSlots == null || recipe.OnlyForSlots.Contains(selectedSlot))
                                 && RecipeAppliesToBlueprint(recipe, upgradeItem?.Blueprint))
                .OrderBy(recipe => recipe.ParentNameId ?? recipe.NameId)
                .ToArray();
            var recipeNames = availableRecipes.Select(recipe => recipe.ParentNameId ?? recipe.NameId)
                .Concat(upgradeItem == null && (craftingData.NewItemBaseIDs == null || craftingData.NewItemBaseIDs.Length == 0) || upgradeItemDoubleWeapon != null || (upgradeItemShield != null && upgradeItemShieldArmor != upgradeItem)
                    ? new string[0]
                    : new[] {new L10NString("craftMagicItems-label-cast-spell-n-times").ToString()})
                .ToArray();
            var selectedRecipeIndex = DrawSelectionUserInterfaceElements("Enchantment: ", recipeNames, 5, ref Selections.SelectedCustomName);
            if (selectedRecipeIndex == availableRecipes.Length) {
                // Cast spell N times
                RenderCastSpellNTimes(caster, craftingData, upgradeItemShield ?? upgradeItem, selectedSlot);
                return;
            }

            var selectedRecipe = availableRecipes[selectedRecipeIndex];
            if (selectedRecipe.ParentNameId != null) {
                var category = recipeNames[selectedRecipeIndex];
                var availableSubRecipes = craftingData.SubRecipes[selectedRecipe.ParentNameId]
                    .OrderBy(recipe => recipe.NameId)
                    .ToArray();
                recipeNames = availableSubRecipes.Select(recipe => recipe.NameId).ToArray();
                var selectedSubRecipeIndex = DrawSelectionUserInterfaceElements(category + ": ", recipeNames, 5, ref Selections.SelectedCustomName);
                selectedRecipe = availableSubRecipes[selectedSubRecipeIndex];
            }

            BlueprintItemEnchantment selectedEnchantment = null;
            BlueprintItemEnchantment[] availableEnchantments = null;
            var selectedEnchantmentIndex = 0;
            if (selectedRecipe.ResultItem == null) {
                // Pick specific enchantment from the recipe
                if (selectedRecipe.EnchantmentsCumulative && upgradeItem != null) {
                    var itemEnchantments = GetEnchantments(upgradeItem.Blueprint, selectedRecipe);
                    availableEnchantments = selectedRecipe.Enchantments.Where(enchantment => !itemEnchantments.Contains(enchantment)).ToArray();
                } else {
                    availableEnchantments = selectedRecipe.Enchantments;
                    var supersededEnchantment = upgradeItem != null ? FindSupersededEnchantmentId(upgradeItem.Blueprint, availableEnchantments[0].AssetGuid) : null;
                    if (supersededEnchantment != null) {
                        // Don't offer downgrade options.
                        var existingIndex = availableEnchantments.FindIndex(enchantment => enchantment.AssetGuid == supersededEnchantment);
                        availableEnchantments = availableEnchantments.Skip(existingIndex + 1).ToArray();
                    }
                }

                var component = selectedRecipe.Enchantments[0].GetComponent<AddStatBonusEquipment>();
                if (availableEnchantments.Length == 0 || (component != null && upgradeItem != null && upgradeItem.Blueprint.Enchantments.Any(enchantment => {
                    if (!selectedRecipe.Enchantments.Any(e => e == enchantment)) {
                        var component2 = enchantment.GetComponent<AddStatBonusEquipment>();

                        if (component2 && component.Stat == component2.Stat) {
                            return true;
                        }
                    }
                    return false;
                }))) {
                    UmmUiRenderer.RenderLabelRow("This item cannot be further upgraded with this enchantment.");
                    return;
                } else if (availableEnchantments.Length > 0 && selectedRecipe.Enchantments.Length > 1) {
                    var counter = selectedRecipe.Enchantments.Length - availableEnchantments.Length;
                    var enchantmentNames = availableEnchantments.Select(enchantment => {
                        counter++;
                        return enchantment.Name.Empty() ? GetBonusString(counter, selectedRecipe) : enchantment.Name;
                    });
                    selectedEnchantmentIndex = DrawSelectionUserInterfaceElements("", enchantmentNames.ToArray(), 6);
                }

                selectedEnchantment = availableEnchantments[selectedEnchantmentIndex];
            }

            var casterLevel = selectedRecipe.CasterLevelStart
                              + (selectedEnchantment == null
                                  ? 0
                                  : selectedRecipe.Enchantments.FindIndex(e => e == selectedEnchantment) * selectedRecipe.CasterLevelMultiplier);
            if (selectedEnchantment != null) {
                if (!string.IsNullOrEmpty(selectedEnchantment.Description)) {
                    UmmUiRenderer.RenderLabelRow(selectedEnchantment.Description);
                }
                if (selectedRecipe.CostType == RecipeCostType.EnhancementLevelSquared) {
                    UmmUiRenderer.RenderLabelRow($"Plus equivalent: +{GetPlusOfRecipe(selectedRecipe, selectedRecipe.Enchantments.FindIndex(e => e == selectedEnchantment) + 1)}");
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Prerequisites: ", GUILayout.ExpandWidth(false));
            var prerequisites = $"{LocalizedStringBlueprints.CasterLevelLocalized} {casterLevel}";
            if (selectedRecipe.PrerequisiteSpells != null && selectedRecipe.PrerequisiteSpells.Length > 0) {
                prerequisites += $"; {selectedRecipe.PrerequisiteSpells.Select(ability => ability.Name).BuildCommaList(selectedRecipe.AnyPrerequisite)}";
            }

            if (selectedRecipe.PrerequisiteFeats != null && selectedRecipe.PrerequisiteFeats.Length > 0) {
                prerequisites += $"; {selectedRecipe.PrerequisiteFeats.Select(feature => feature.Name).BuildCommaList(selectedRecipe.AnyPrerequisite)}";
            }

            if (selectedRecipe.CrafterPrerequisites != null) {
                prerequisites += "; " + LocalizationHelper.FormatLocalizedString("craftMagicItems-crafter-prerequisite-required", selectedRecipe.CrafterPrerequisites
                                     .Select(prerequisite => new L10NString($"craftMagicItems-crafter-prerequisite-{prerequisite}").ToString())
                                     .BuildCommaList(false));
            }

            GUILayout.Label(prerequisites, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            RenderCraftingSkillInformation(caster, StatType.SkillKnowledgeArcana, 5 + casterLevel, casterLevel, selectedRecipe.PrerequisiteSpells,
                selectedRecipe.PrerequisiteFeats, selectedRecipe.AnyPrerequisite, selectedRecipe.CrafterPrerequisites);

            if (selectedRecipe.ResultItem != null) {
                // Just craft the item resulting from the recipe.
                RenderRecipeBasedCraftItemControl(caster, craftingData, selectedRecipe, casterLevel, selectedRecipe.ResultItem);
                return;
            }

            // See if the selected enchantment (plus optional mundane base item) corresponds to a vanilla blueprint.
            var allItemBlueprintsWithEnchantment = FindItemBlueprintForEnchantmentId(selectedEnchantment.AssetGuid)?.Where(blueprint =>
                DoesBlueprintMatchSlot(blueprint, selectedSlot) && DoesBlueprintMatchRestrictions(blueprint, selectedSlot, craftingData.SlotRestrictions));
            BlueprintItemEquipment matchingItem = null;
            if (upgradeItemDoubleWeapon != null) {
                matchingItem = allItemBlueprintsWithEnchantment?.FirstOrDefault(blueprint =>
                    DoesItemMatchAllEnchantments(blueprint, null, selectedEnchantment.AssetGuid, upgradeItemDoubleWeapon?.Blueprint as BlueprintItemEquipment, false)
                );
            } else if (upgradeItemShield != null) {
                if (Selections.SelectedShieldWeapon) {
                    matchingItem = allItemBlueprintsWithEnchantment?.FirstOrDefault(blueprint =>
                        DoesItemMatchAllEnchantments(blueprint, null, selectedEnchantment.AssetGuid, upgradeItemShield?.Blueprint as BlueprintItemEquipment, false)
                    );
                } else {
                    matchingItem = allItemBlueprintsWithEnchantment?.FirstOrDefault(blueprint =>
                        DoesItemMatchAllEnchantments(blueprint, selectedEnchantment.AssetGuid, null, upgradeItemShield?.Blueprint as BlueprintItemEquipment, false)
                    );
                }
            } else {
                matchingItem = allItemBlueprintsWithEnchantment?.FirstOrDefault(blueprint =>
                    DoesItemMatchAllEnchantments(blueprint, selectedEnchantment.AssetGuid, null, upgradeItem?.Blueprint as BlueprintItemEquipment, false)
                );
            }
            BlueprintItemEquipment itemToCraft;
            var itemGuid = "[not set]";
            if (matchingItem) {
                // Crafting an existing blueprint.
                if (upgradeItemDoubleWeapon != null) {
                    upgradeItem = upgradeItemDoubleWeapon;
                }
                if (upgradeItemShield != null) {
                    upgradeItem = upgradeItemShield;
                }
                if (RulesRecipeItemCost(matchingItem) == matchingItem.Cost) {
                    itemToCraft = matchingItem;
                } else {
                    itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(matchingItem.AssetGuid, Enumerable.Empty<string>(),
                        priceAdjust: RulesRecipeItemCost(matchingItem) - matchingItem.Cost);
                    itemToCraft = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipment>(itemGuid);
                }
            } else if (upgradeItem != null) {
                // Upgrading to a custom blueprint
                var name = upgradeItemShield?.Blueprint?.Name ?? upgradeItem.Blueprint.Name;
                Selections.SelectedCustomName = UmmUiRenderer.RenderCustomNameField(name, Selections.SelectedCustomName);
                name = Selections.SelectedCustomName == name ? null : Selections.SelectedCustomName;
                IEnumerable<string> enchantments;
                string supersededEnchantmentId;
                if (selectedRecipe.EnchantmentsCumulative) {
                    enchantments = availableEnchantments.Take(selectedEnchantmentIndex + 1).Select(enchantment => enchantment.AssetGuid);
                    supersededEnchantmentId = null;
                } else {
                    enchantments = new List<string> {selectedEnchantment.AssetGuid};
                    supersededEnchantmentId = FindSupersededEnchantmentId(upgradeItem.Blueprint, selectedEnchantment.AssetGuid);
                }

                if (upgradeItemShield != null) {
                    upgradeItem = upgradeItemShield;
                }
                if (Selections.SelectedShieldWeapon) {
                    itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(upgradeItemShieldWeapon.Blueprint.AssetGuid, enchantments,
                        supersededEnchantmentId == null ? null : new[] {supersededEnchantmentId});
                    itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(upgradeItemShield.Blueprint.AssetGuid, Enumerable.Empty<string>(),
                        name: name, descriptionId: "null", secondEndGuid: itemGuid);
                } else {
                    itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(upgradeItem.Blueprint.AssetGuid, enchantments,
                        supersededEnchantmentId == null ? null : new[] {supersededEnchantmentId}, name: name, descriptionId: "null");
                }
                if (upgradeItemDoubleWeapon != null) {
                    // itemGuid is the blueprint GUID of the second end of upgradeItemWeapon - build the overall blueprint with the custom second end.
                    itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(upgradeItemDoubleWeapon.Blueprint.AssetGuid, Enumerable.Empty<string>(),
                        descriptionId: "null", secondEndGuid: itemGuid);
                    upgradeItem = upgradeItemDoubleWeapon;
                }
                itemToCraft = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipment>(itemGuid);
            } else {
                // Crafting a new custom blueprint from scratch.
                SelectRandomApplicableBaseGuid(craftingData, selectedSlot);
                var baseBlueprint = Selections.SelectedBaseBlueprint;
                Selections.SelectedCustomName = UmmUiRenderer.RenderCustomNameField($"{selectedRecipe.NameId} {new L10NString(GetSlotStringKey(selectedSlot, craftingData.SlotRestrictions))}", Selections.SelectedCustomName);
                var enchantmentsToRemove = GetEnchantments(baseBlueprint, selectedRecipe).Select(enchantment => enchantment.AssetGuid).ToArray();
                IEnumerable<string> enchantments;
                if (selectedRecipe.EnchantmentsCumulative) {
                    enchantments = availableEnchantments.Take(selectedEnchantmentIndex + 1).Select(enchantment => enchantment.AssetGuid);
                } else {
                    enchantments = new List<string> { selectedEnchantment.AssetGuid };
                }
                itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(Selections.SelectedBaseBlueprint.AssetGuid, enchantments, enchantmentsToRemove,
                    Selections.SelectedCustomName ?? "[custom item]", "null", "null");
                itemToCraft = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipment>(itemGuid);
            }

            if (!itemToCraft) {
                UmmUiRenderer.RenderLabelRow($"Error: null custom item from looking up blueprint ID {itemGuid}");
            } else {
                if (IsItemLegalEnchantmentLevel(itemToCraft)) {
                    RenderRecipeBasedCraftItemControl(caster, craftingData, selectedRecipe, casterLevel, itemToCraft, upgradeItem);
                } else {
                    var maxEnchantmentLevel = ItemMaxEnchantmentLevel(itemToCraft);
                    UmmUiRenderer.RenderLabelRow($"This would result in {itemToCraft.Name} having an equivalent enhancement bonus of more than +{maxEnchantmentLevel}");
                }
            }
        }

        private static void GetStatBonusName(AddStatBonusEquipment component, List<string> list) {
            string stat;
            switch (component.Stat) {
                case StatType.AC:
                    switch (component.Descriptor) {
                        case ModifierDescriptor.ArmorEnhancement: return;
                        case ModifierDescriptor.ShieldEnhancement: return;
                        case ModifierDescriptor.NaturalArmorEnhancement: stat = new L10NString("64f7fae4-a525-45ce-948d-338277b6a18e"); break;
                        default: stat = component.Descriptor.ToString(); break;
                    }
                    break;
                case StatType.SkillAthletics: stat = new L10NString("55b798bb-0bb9-4c55-8b2d-a02ed17bfd38"); break;
                case StatType.SkillKnowledgeArcana: stat = new L10NString("75941008-1ec4-4085-ab6d-17c18d15b662"); break;
                case StatType.SkillKnowledgeWorld: stat = new L10NString("c9a55fe4-eed1-47d7-9125-0687fec69a60"); break;
                case StatType.SkillLoreNature: stat = new L10NString("7eacb9bc-dc59-4f03-81dd-43ba4c0bf394"); break;
                case StatType.SkillLoreReligion: stat = new L10NString("379c76c7-7af4-4c0b-af9f-dfdcc99ba30b"); break;
                case StatType.SkillMobility: stat = new L10NString("afa39917-011f-4e09-b44c-d8451d923687"); break;
                case StatType.SkillPerception: stat = new L10NString("7cb007fe-69de-4ac7-939d-3c5514687bc4"); break;
                case StatType.SkillPersuasion: stat = new L10NString("2041dca2-e9bb-482f-b9ae-81712542f2ef"); break;
                case StatType.SkillStealth: stat = new L10NString("27537f23-85d7-4dad-a53b-f9e92cd43ff5"); break;
                case StatType.SkillThievery: stat = new L10NString("bc26493d-da03-4c9f-b674-00971706474c"); break;
                case StatType.SkillUseMagicDevice: stat = new L10NString("e9652d12-9c96-4e6e-b088-3b3709241896"); break;
                default: stat = component.Stat.ToString(); break;
            }
            list.Add($"{stat} {UIUtility.AddSign(component.Value)}");
        }

        private static string GetEnchantmentNames(BlueprintItem blueprint) {
            return blueprint.Enchantments
                .Where(enchantment => {
                    var keep = true;
                    if (string.IsNullOrEmpty(enchantment.Name)) {
                        keep = false;
                        enchantment.CallComponents<AddStatBonusEquipment>(c => {
                            if (c.Descriptor != ModifierDescriptor.ArmorEnhancement && c.Descriptor != ModifierDescriptor.ShieldEnhancement) {
                                keep = true;
                            }
                        });
                    }
                    return keep;
                }).Select(enchantment => {
                    var list = new List<string>();
                    if (string.IsNullOrEmpty(enchantment.Name)) {
                        enchantment.CallComponents<AddStatBonusEquipment>(c => GetStatBonusName(c, list));
                        return list;
                    } else {
                        return new List<string> { enchantment.Name };
                    }
                }).SelectMany(l => l).ToList().OrderBy(name => name).Join();
        }

        private static string BuildItemDescription(ItemEntity item) {
            var description = item.Description;
            if (CraftMagicItemsBlueprintPatcher.DoesBlueprintShowEnchantments(item.Blueprint)) {
                string qualities;
                if (item.Blueprint is BlueprintItemShield shield) {
                    qualities = GetEnchantmentNames(shield.ArmorComponent);
                    var weaponQualities = shield.WeaponComponent == null ? null : GetEnchantmentNames(shield.WeaponComponent);
                    if (!string.IsNullOrEmpty(weaponQualities)) {
                        qualities = $"{qualities}{(string.IsNullOrEmpty(qualities) ? "" : ", ")}{LocalizedStringBlueprints.ShieldBashLocalized}: {weaponQualities}";
                    }
                } else {
                    qualities = GetEnchantmentNames(item.Blueprint);
                }
                if (!string.IsNullOrEmpty(qualities)) {
                    description += $"{(string.IsNullOrEmpty(description) ? "" : "\n")}{LocalizedStringBlueprints.QualitiesLocalized}: {qualities}";
                }
            }
            return description;
        }

        private static void SelectRandomApplicableBaseGuid(ItemCraftingData craftingData, ItemsFilter.ItemType selectedSlot) {
            if (Selections.SelectedBaseBlueprint != null) {
                var baseBlueprint = Selections.SelectedBaseBlueprint;
                if (!baseBlueprint || !DoesBlueprintMatchSlot(baseBlueprint, selectedSlot)) {
                    Selections.SelectedBaseBlueprint = null;
                }
            }

            Selections.SelectedBaseBlueprint = Selections.SelectedBaseBlueprint ?? RandomBaseBlueprintId(craftingData,
                                   blueprint => DoesBlueprintMatchSlot(blueprint, selectedSlot));
        }

        private static void RenderCastSpellNTimes(UnitEntityData caster, RecipeBasedItemCraftingData craftingData, ItemEntity upgradeItem,
            ItemsFilter.ItemType selectedSlot) {
            BlueprintItemEquipment equipment = null;
            if (upgradeItem != null) {
                equipment = upgradeItem.Blueprint as BlueprintItemEquipment;
                if (equipment == null || equipment.Ability != null && equipment.SpendCharges && !equipment.RestoreChargesOnRest) {
                    UmmUiRenderer.RenderLabelRow($"{upgradeItem.Name} cannot cast a spell N times a day (this is unexpected - please let the mod author know)");
                    return;
                } else if (equipment.Ability != null && !equipment.Ability.IsSpell) {
                    UmmUiRenderer.RenderLabelRow($"{equipment.Ability.Name} is not a spell, so cannot be upgraded.");
                    return;
                }
            }

            BlueprintAbility ability;
            int spellLevel;
            if (equipment == null || equipment.Ability == null) {
                // Choose a spellbook known to the caster
                var spellbooks = caster.Descriptor.Spellbooks.ToList();
                var spellBookNames = spellbooks.Select(book => book.Blueprint.Name.ToString()).Concat(Enumerable.Repeat("From Items", 1)).ToArray();
                var selectedSpellbookIndex = DrawSelectionUserInterfaceElements("Source: ", spellBookNames, 10, ref Selections.SelectedCustomName);
                if (selectedSpellbookIndex < spellbooks.Count) {
                    var spellbook = spellbooks[selectedSpellbookIndex];
                    // Choose a spell level
                    var spellLevelNames = Enumerable.Range(0, spellbook.Blueprint.MaxSpellLevel + 1).Select(index => $"Level {index}").ToArray();
                    spellLevel = DrawSelectionUserInterfaceElements("Spell level: ", spellLevelNames, 10, ref Selections.SelectedCustomName);
                    var specialSpellLists = Accessors.GetSpellbookSpecialLists(spellbook);
                    var spellOptions = spellbook.Blueprint.SpellList.GetSpells(spellLevel)
                        .Concat(specialSpellLists.Aggregate(new List<BlueprintAbility>(), (allSpecial, spellList) => spellList.GetSpells(spellLevel)))
                        .Distinct()
                        .OrderBy(spell => spell.Name)
                        .ToArray();
                    if (!spellOptions.Any()) {
                        UmmUiRenderer.RenderLabelRow($"There are no level {spellLevel} {spellbook.Blueprint.Name} spells");
                        return;
                    }

                    var spellNames = spellOptions.Select(spell => spell.Name).ToArray();
                    var selectedSpellIndex = DrawSelectionUserInterfaceElements("Spell: ", spellNames, 4, ref Selections.SelectedCustomName);
                    ability = spellOptions[selectedSpellIndex];
                    if (ability.HasVariants && ability.Variants != null) {
                        var selectedVariantIndex =
                            DrawSelectionUserInterfaceElements("Variant: ", ability.Variants.Select(spell => spell.Name).ToArray(), 4, ref Selections.SelectedCustomName);
                        ability = ability.Variants[selectedVariantIndex];
                    }
                } else {
                    var itemBlueprints = Game.Instance.Player.Inventory
                        .Where(item => item.Wielder == caster.Descriptor)
                        .Select(item => item.Blueprint)
                        .OfType<BlueprintItemEquipment>()
                        .Where(blueprint => blueprint.Ability != null && blueprint.Ability.IsSpell
                                                                      && (!(blueprint is BlueprintItemEquipmentUsable usable) ||
                                                                          usable.Type != UsableItemType.Potion))
                        .OrderBy(item => item.Name)
                        .ToArray();
                    if (itemBlueprints.Length == 0) {
                        UmmUiRenderer.RenderLabelRow("You are not wielding any items that can cast spells.");
                        return;
                    }
                    var itemNames = itemBlueprints.Select(item => item.Name).ToArray();
                    var itemIndex = DrawSelectionUserInterfaceElements("Cast from item: ", itemNames, 5, ref Selections.SelectedCustomName);
                    var selectedItemBlueprint = itemBlueprints[itemIndex];
                    ability = selectedItemBlueprint.Ability;
                    spellLevel = selectedItemBlueprint.SpellLevel;
                    UmmUiRenderer.RenderLabelRow($"Spell: {ability.Name}");
                }
            } else {
                ability = equipment.Ability;
                spellLevel = equipment.SpellLevel;
                GameLogContext.Count = equipment.Charges;
                UmmUiRenderer.RenderLabelRow($"Current: {LocalizationHelper.FormatLocalizedString("craftMagicItems-label-cast-spell-n-times-details", ability.Name, equipment.CasterLevel)}");
                GameLogContext.Clear();
            }

            // Choose a caster level
            var minCasterLevel = Math.Max(equipment == null ? 0 : equipment.CasterLevel, Math.Max(1, 2 * spellLevel - 1));
            Selections.SelectedCasterLevel = UmmUiRenderer.RenderIntSlider("Caster level: ", Selections.SelectedCasterLevel, minCasterLevel, 20);
            // Choose number of times per day
            var maxCastsPerDay = equipment == null ? 10 : ((equipment.Charges + 10) / 10) * 10;
            Selections.SelectedCastsPerDay = UmmUiRenderer.RenderIntSlider("Casts per day: ", Selections.SelectedCastsPerDay, equipment == null ? 1 : equipment.Charges, maxCastsPerDay);
            if (equipment != null && ability == equipment.Ability && Selections.SelectedCasterLevel == equipment.CasterLevel && Selections.SelectedCastsPerDay == equipment.Charges) {
                UmmUiRenderer.RenderLabelRow($"No changes made to {equipment.Name}");
                return;
            }

            // Show skill info
            RenderCraftingSkillInformation(caster, StatType.SkillKnowledgeArcana, 5 + Selections.SelectedCasterLevel, Selections.SelectedCasterLevel, new[] {ability});

            string itemGuid;
            if (upgradeItem == null) {
                // Option to rename item
                Selections.SelectedCustomName = UmmUiRenderer.RenderCustomNameField($"{ability.Name} {new L10NString(GetSlotStringKey(selectedSlot, craftingData.SlotRestrictions))}", Selections.SelectedCustomName);
                // Pick random base item
                SelectRandomApplicableBaseGuid(craftingData, selectedSlot);
                // Create customised item GUID
                var baseBlueprint = Selections.SelectedBaseBlueprint;
                var enchantmentsToRemove = GetEnchantments(baseBlueprint).Select(enchantment => enchantment.AssetGuid).ToArray();
                itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(Selections.SelectedBaseBlueprint.AssetGuid, new List<string>(), enchantmentsToRemove, Selections.SelectedCustomName,
                    ability.AssetGuid, "null", casterLevel: Selections.SelectedCasterLevel, spellLevel: spellLevel, perDay: Selections.SelectedCastsPerDay);
            } else {
                // Option to rename item
                Selections.SelectedCustomName = UmmUiRenderer.RenderCustomNameField(upgradeItem.Blueprint.Name, Selections.SelectedCustomName);
                // Create customised item GUID
                itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(upgradeItem.Blueprint.AssetGuid, new List<string>(), null,
                    Selections.SelectedCustomName == upgradeItem.Blueprint.Name ? null : Selections.SelectedCustomName, ability.AssetGuid,
                    casterLevel: Selections.SelectedCasterLevel == equipment.CasterLevel ? -1 : Selections.SelectedCasterLevel,
                    spellLevel: spellLevel == equipment.SpellLevel ? -1 : spellLevel,
                    perDay: Selections.SelectedCastsPerDay == equipment.Charges ? -1 : Selections.SelectedCastsPerDay);
            }

            var itemToCraft = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipment>(itemGuid);

            // Render craft button
            GameLogContext.Count = Selections.SelectedCastsPerDay;
            UmmUiRenderer.RenderLabelRow(LocalizationHelper.FormatLocalizedString("craftMagicItems-label-cast-spell-n-times-details", ability.Name, Selections.SelectedCasterLevel));
            GameLogContext.Clear();
            var recipe = new RecipeData {
                PrerequisiteSpells = new[] {ability},
                PrerequisitesMandatory = true
            };
            RenderRecipeBasedCraftItemControl(caster, craftingData, recipe, Selections.SelectedCasterLevel, itemToCraft, upgradeItem);
        }

        public static int CharacterCasterLevel(UnitDescriptor character, Spellbook forSpellbook = null) {
            // There can be modifiers to Caster Level beyond what's set in a character's Spellbooks (e.g Magical Knack) - use event system.
            var casterLevel = 0;
            var booksToCheck = forSpellbook == null ? character.Spellbooks : Enumerable.Repeat(forSpellbook, 1);
            foreach (var spellbook in booksToCheck) {
                if (spellbook.CasterLevel > 0) {
#if PATCH21_BETA
                    var blueprintAbility = SerializedScriptableObject.CreateInstance<BlueprintAbility>();
#else
                    var blueprintAbility = ScriptableObject.CreateInstance<BlueprintAbility>();
#endif
                    var rule = new RuleCalculateAbilityParams(character.Unit, blueprintAbility, spellbook);
                    RulebookEventBus.OnEventAboutToTrigger(rule);
                    rule.OnTrigger(null);
                    casterLevel = rule.Result.CasterLevel > casterLevel ? rule.Result.CasterLevel : casterLevel;
                }
            }

            return casterLevel;
        }

        private static SpellSchool CheckForOppositionSchool(UnitDescriptor crafter, BlueprintAbility[] prerequisiteSpells) {
            if (prerequisiteSpells != null) {
                foreach (var spell in prerequisiteSpells) {
                    if (crafter.Spellbooks.Any(spellbook => spellbook.Blueprint.SpellList.Contains(spell)
                                                            && spellbook.OppositionSchools.Contains(spell.School))) {
                        return spell.School;
                    }
                }
            }
            return SpellSchool.None;
        }

        private static int RenderCraftingSkillInformation(UnitEntityData crafter, StatType skill, int dc, int casterLevel = 0,
            BlueprintAbility[] prerequisiteSpells = null, BlueprintFeature[] prerequisiteFeats = null, bool anyPrerequisite = false,
            CrafterPrerequisiteType[] crafterPrerequisites = null,
            bool render = true) {
            if (render) {
                UmmUiRenderer.RenderLabelRow($"Base Crafting DC: {dc}");
            }
            // ReSharper disable once UnusedVariable
            var missing = CheckSpellPrerequisites(prerequisiteSpells, anyPrerequisite, crafter.Descriptor, false, out var missingSpells,
                // ReSharper disable once UnusedVariable
                out var spellsToCast);
            missing += CheckFeatPrerequisites(prerequisiteFeats, anyPrerequisite, crafter.Descriptor, out var missingFeats);
            missing += GetMissingCrafterPrerequisites(crafterPrerequisites, crafter.Descriptor).Count;
            var crafterCasterLevel = CharacterCasterLevel(crafter.Descriptor);
            var casterLevelShortfall = Math.Max(0, casterLevel - crafterCasterLevel);
            if (casterLevelShortfall > 0 && ModSettings.CasterLevelIsSinglePrerequisite) {
                missing++;
                casterLevelShortfall = 0;
            }
            if (missing > 0 && render) {
                UmmUiRenderer.RenderLabelRow(
                    $"{crafter.CharacterName} is unable to meet {missing} of the prerequisites, raising the DC by {DifficultyClass.MissingPrerequisiteDCModifier * missing}");
            }
            if (casterLevelShortfall > 0 && render) {
                UmmUiRenderer.RenderLabelRow(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-low-caster-level", casterLevel, DifficultyClass.MissingPrerequisiteDCModifier * casterLevelShortfall));
            }
            // Rob's ruling... if you're below the prerequisite caster level, you're considered to be missing a prerequisite for each
            // level you fall short.
            dc += DifficultyClass.MissingPrerequisiteDCModifier * (missing + casterLevelShortfall);
            var oppositionSchool = CheckForOppositionSchool(crafter.Descriptor, prerequisiteSpells);
            if (oppositionSchool != SpellSchool.None) {
                dc += DifficultyClass.OppositionSchoolDCModifier;
                if (render) {
                    UmmUiRenderer.RenderLabelRow(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-opposition-school", LocalizedTexts.Instance.SpellSchoolNames.GetText(oppositionSchool),
                        DifficultyClass.OppositionSchoolDCModifier));
                }
            }
            var skillCheck = 10 + crafter.Stats.GetStat(skill).ModifiedValue;
            if (render) {
                UmmUiRenderer.RenderLabelRow(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-made-progress-check", LocalizedTexts.Instance.Stats.GetText(skill), skillCheck, dc));
            }

            var skillMargin = skillCheck - dc;
            if (skillMargin < 0 && render) {
                UmmUiRenderer.RenderLabelRow(ModSettings.CraftingTakesNoTime
                    ? $"This project would be too hard for {crafter.CharacterName} if \"Crafting Takes No Time\" cheat was disabled."
                    : $"<color=red>Warning:</color> This project will be too hard for {crafter.CharacterName}");
            }

            return skillMargin;
        }

        private static int GetMaterialComponentMultiplier(ItemCraftingData craftingData, BlueprintItem resultBlueprint = null,
            BlueprintItem upgradeBlueprint = null) {
            if (craftingData is SpellBasedItemCraftingData spellBased) {
                return spellBased.Charges;
            }

            var upgradeEquipment = upgradeBlueprint as BlueprintItemEquipment;
            if (resultBlueprint is BlueprintItemEquipment resultEquipment && resultEquipment.RestoreChargesOnRest
                                                                          && resultEquipment.Ability !=
                                                                          (upgradeEquipment == null ? null : upgradeEquipment.Ability)) {
                // Cast a Spell N times a day costs material components as if it has 50 charges.
                return 50;
            }

            return 0;
        }

        private static void CancelCraftingProject(CraftingProjectData project) {
            // Refund gold and material components.
            if (!ModSettings.CraftingCostsNoGold) {
                Game.Instance.UI.Common.UISound.Play(UISoundType.LootCollectGold);
                var goldRefund = project.GoldSpent >= 0 ? project.GoldSpent : project.TargetCost;
                Game.Instance.Player.GainMoney(goldRefund);
                var craftingData = LoadedData.ItemCraftingData.FirstOrDefault(data => data.Name == project.ItemType);
                BuildCostString(out var cost, craftingData, goldRefund, project.SpellPrerequisites, project.ResultItem.Blueprint, project.UpgradeItem?.Blueprint);
                var factor = GetMaterialComponentMultiplier(craftingData, project.ResultItem.Blueprint, project.UpgradeItem?.Blueprint);
                if (factor > 0) {
                    foreach (var prerequisiteSpell in project.SpellPrerequisites) {
                        if (prerequisiteSpell.MaterialComponent.Item) {
                            var number = prerequisiteSpell.MaterialComponent.Count * factor;
                            Game.Instance.Player.Inventory.Add(prerequisiteSpell.MaterialComponent.Item, number);
                        }
                    }
                }

                AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-crafting-cancelled", project.ResultItem.Name, cost));
            }

            var timer = GetCraftingTimerComponentForCaster(project.Crafter.Descriptor);
            timer.CraftingProjects.Remove(project);
            if (project.UpgradeItem == null) {
                ItemCreationProjects.Remove(project);
            } else {
                ItemUpgradeProjects.Remove(project.UpgradeItem);
                if (ItemUpgradeProjects.ContainsKey(project.ResultItem)) {
                    CancelCraftingProject(ItemUpgradeProjects[project.ResultItem]);
                }
            }
        }

        private static int CalculateBaseMundaneCraftingDC(RecipeBasedItemCraftingData craftingData, BlueprintItem blueprint, UnitDescriptor crafter) {
            var dc = craftingData.MundaneBaseDC;
            switch (blueprint) {
                case BlueprintItemArmor armor:
                    return dc + armor.ArmorBonus;
                case BlueprintItemShield shield:
                    return dc + shield.ArmorComponent.ArmorBonus;
                case BlueprintItemWeapon weapon:
                    if (weapon.Category.HasSubCategory(WeaponSubCategory.Exotic)) {
                        var martialWeaponProficiencies = ResourcesLibrary.TryGetBlueprint(Features.MartialWeaponProficiencies);
                        if (martialWeaponProficiencies != null && martialWeaponProficiencies.GetComponents<AddProficiencies>()
                                .Any(addProficiencies => addProficiencies.RaceRestriction != null
                                                         && addProficiencies.RaceRestriction == crafter.Progression.Race
                                                         && addProficiencies.WeaponProficiencies.Contains(weapon.Category))) {
                            // The crafter treats this exotic weapon as if it's martial.  Hard code the difference in DC.
                            dc -= 3;
                        }
                    }
                    break;
                case BlueprintItemEquipmentUsable usable when usable.AssetGuid == "fd56596e273d1ff49a8c29cc9802ae6e":
                    // Alchemist's Fire has a DC of 20
                    dc += 5;
                    break;
            }

            return dc;
        }

        private static int CalculateMundaneCraftingDC(RecipeBasedItemCraftingData craftingData, BlueprintItem blueprint, UnitDescriptor crafter,
            RecipeData recipe = null) {
            var dc = CalculateBaseMundaneCraftingDC(craftingData, blueprint, crafter);
            return dc + (recipe?.MundaneDC ?? blueprint.Enchantments
                             .Select(enchantment => FindSourceRecipe(enchantment.AssetGuid, blueprint)?.MundaneDC ?? 0)
                             .DefaultIfEmpty(0)
                             .Max()
                   );
        }

        private static void RenderCraftMundaneItemsSection() {
            var crafter = GetSelectedCrafter(false);

            // Choose crafting data
            var itemTypes = LoadedData.ItemCraftingData
                .Where(data => data.NameId != null && data.FeatGuid == null
                               && (data.ParentNameId == null || LoadedData.SubCraftingData[data.ParentNameId][0] == data))
                .ToArray();
            var itemTypeNames = itemTypes.Select(data => new L10NString(data.ParentNameId ?? data.NameId).ToString()).ToArray();
            var selectedItemTypeIndex = Selections.UpgradingBlueprint == null
                ? DrawSelectionUserInterfaceElements("Mundane Crafting: ", itemTypeNames, 6, ref Selections.SelectedCustomName)
                : Selections.GetSelectionIndex("Mundane Crafting: ");

            var selectedCraftingData = itemTypes[selectedItemTypeIndex];
            if (selectedCraftingData.ParentNameId != null) {
                itemTypeNames = LoadedData.SubCraftingData[selectedCraftingData.ParentNameId].Select(data => new L10NString(data.NameId).ToString()).ToArray();
                var label = new L10NString(selectedCraftingData.ParentNameId) + ": ";
                var selectedItemSubTypeIndex = Selections.UpgradingBlueprint == null
                    ? DrawSelectionUserInterfaceElements(label, itemTypeNames, 6)
                    : Selections.GetSelectionIndex(label);

                selectedCraftingData = LoadedData.SubCraftingData[selectedCraftingData.ParentNameId][selectedItemSubTypeIndex];
            }

            if (!(selectedCraftingData is RecipeBasedItemCraftingData craftingData)) {
                UmmUiRenderer.RenderLabelRow("Unable to find mundane crafting recipe.");
                return;
            }

            BlueprintItem baseBlueprint;

            if (Selections.UpgradingBlueprint != null) {
                baseBlueprint = Selections.UpgradingBlueprint;
                UmmUiRenderer.RenderLabelRow($"Applying upgrades to {baseBlueprint.Name}");
            } else {
                // Choose mundane item of selected type to create
                var blueprints = craftingData.NewItemBaseIDs
                    .Where(blueprint => blueprint != null
                                        && (!(blueprint is BlueprintItemWeapon weapon) || !weapon.Category.HasSubCategory(WeaponSubCategory.Disabled)))
                    .OrderBy(blueprint => blueprint.Name)
                    .ToArray();
                var blueprintNames = blueprints.Select(item => item.Name).ToArray();
                if (blueprintNames.Length == 0) {
                    UmmUiRenderer.RenderLabelRow("No known items of that type.");
                    return;
                }

                var selectedUpgradeItemIndex = DrawSelectionUserInterfaceElements("Item: ", blueprintNames, 5, ref Selections.SelectedCustomName);
                baseBlueprint = blueprints[selectedUpgradeItemIndex];
                // See existing item details and enchantments.
                UmmUiRenderer.RenderLabelRow(baseBlueprint.Description);
            }

            // Assume only one slot type per crafting data
            var selectedSlot = craftingData.Slots[0];

            // Pick recipe to apply.
            var availableRecipes = craftingData.Recipes
                .Where(recipe => (recipe.OnlyForSlots == null || recipe.OnlyForSlots.Contains(selectedSlot))
                                 && RecipeAppliesToBlueprint(recipe, baseBlueprint, skipMaterialCheck: true)
                                 && (recipe.Enchantments.Length != 1 || !baseBlueprint.Enchantments.Contains(recipe.Enchantments[0])))
                .OrderBy(recipe => recipe.NameId)
                .ToArray();
            var recipeNames = availableRecipes.Select(recipe => recipe.NameId).ToArray();
            var selectedRecipeIndex = DrawSelectionUserInterfaceElements("Craft: ", recipeNames, 6, ref Selections.SelectedCustomName);
            var selectedRecipe = availableRecipes.Any() ? availableRecipes[selectedRecipeIndex] : null;
            var selectedEnchantment = selectedRecipe?.Enchantments.Length == 1 ? selectedRecipe.Enchantments[0] : null;
            if (selectedRecipe != null && selectedRecipe.Material != 0) {
                UmmUiRenderer.RenderLabelRow(GetWeaponMaterialDescription(selectedRecipe.Material));
            } else if (selectedEnchantment != null && !string.IsNullOrEmpty(selectedEnchantment.Description)) {
                UmmUiRenderer.RenderLabelRow(selectedEnchantment.Description);
            }

            var dc = craftingData.MundaneEnhancementsStackable
                ? CalculateMundaneCraftingDC(craftingData, baseBlueprint, crafter.Descriptor)
                : CalculateMundaneCraftingDC(craftingData, baseBlueprint, crafter.Descriptor, selectedRecipe);

            RenderCraftingSkillInformation(crafter, StatType.SkillKnowledgeWorld, dc);

            // Upgrading to a custom blueprint, rather than use the standard mithral/adamantine blueprints.
            var upgradeName = selectedRecipe != null && selectedRecipe.Material != 0
                ? selectedRecipe.NameId
                : selectedEnchantment == null
                    ? null
                    : selectedEnchantment.Name;
            var name = upgradeName == null ? baseBlueprint.Name : $"{upgradeName} {baseBlueprint.Name}";
            var visual = ApplyVisualMapping(selectedRecipe, baseBlueprint);
            var animation = ApplyAnimationMapping(selectedRecipe, baseBlueprint);
            name = ApplyNameMapping(selectedRecipe, baseBlueprint) ?? name;
            var itemToCraft = baseBlueprint;
            var itemGuid = "[not set]";
            if (selectedRecipe != null) {
                if (baseBlueprint is BlueprintItemWeapon doubleWeapon) {
                    if (doubleWeapon) {
                        if (doubleWeapon.Double) {
                            baseBlueprint = doubleWeapon.SecondWeapon;
                        } else {
                            doubleWeapon = null;
                        }
                    }
                    var enchantments = selectedEnchantment == null ? new List<string>() : new List<string> { selectedEnchantment.AssetGuid };
                    itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(baseBlueprint.AssetGuid, enchantments, name: name,
                        material: selectedRecipe.Material, visual: visual, animation: animation);
                    if (doubleWeapon) {
                        baseBlueprint = doubleWeapon;
                        itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(baseBlueprint.AssetGuid, enchantments, name: name,
                            material: selectedRecipe.Material, visual: visual, animation: animation, secondEndGuid: itemGuid);
                    }
                } else if (baseBlueprint is BlueprintItemShield shield) {
                    if (shield.WeaponComponent != null) {
                        PhysicalDamageMaterial material = selectedRecipe.Material;
                        if ((shield.WeaponComponent.DamageType.Physical.Form & PhysicalDamageForm.Bludgeoning) != 0
                            && selectedEnchantment != null && selectedEnchantment.AssetGuid == ItemQualityBlueprints.MithralArmorEnchantmentGuid) {
                            material = PhysicalDamageMaterial.Silver;
                        }
                        var weaponEnchantments = selectedEnchantment != null && selectedRecipe.Restrictions.Contains(ItemRestrictions.ShieldWeapon) ?
                            new List<string> { selectedEnchantment.AssetGuid } : new List<string>();
                        itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(shield.WeaponComponent.AssetGuid, weaponEnchantments,
                            material: material);
                    }
                    var armorEnchantments = selectedEnchantment != null && selectedRecipe.Restrictions.Contains(ItemRestrictions.ShieldArmor) ?
                        new List<string> { selectedEnchantment.AssetGuid } : new List<string>();
                    itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(baseBlueprint.AssetGuid, armorEnchantments, name: name,
                            visual: visual, animation: animation, secondEndGuid: shield.WeaponComponent != null ? itemGuid : null);
                } else {
                    var enchantments = selectedEnchantment == null ? new List<string>() : new List<string> { selectedEnchantment.AssetGuid };
                    itemGuid = blueprintPatcher.BuildCustomRecipeItemGuid(baseBlueprint.AssetGuid, enchantments, name: name,
                        material: selectedRecipe.Material, visual: visual, animation: animation);
                }
                itemToCraft = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(itemGuid);
            }

            if (!itemToCraft) {
                UmmUiRenderer.RenderLabelRow($"Error: null custom item from looking up blueprint ID {itemGuid}");
            } else {
                if (Selections.UpgradingBlueprint != null && GUILayout.Button($"Cancel {baseBlueprint.Name}", GUILayout.ExpandWidth(false))) {
                    Selections.UpgradingBlueprint = null;
                }

                if (craftingData.MundaneEnhancementsStackable) {
                    if (upgradeName != null && GUILayout.Button($"Add {upgradeName} to {baseBlueprint.Name}", GUILayout.ExpandWidth(false))) {
                        Selections.UpgradingBlueprint = itemToCraft;
                    }

                    RenderRecipeBasedCraftItemControl(crafter, craftingData, null, 0, baseBlueprint);
                } else {
                    RenderRecipeBasedCraftItemControl(crafter, craftingData, selectedRecipe, 0, itemToCraft);
                }
            }

            UmmUiRenderer.RenderLabelRow($"Current Money: {Game.Instance.Player.Money}");
        }

        private static string GetWeaponMaterialDescription(PhysicalDamageMaterial material) {
            var guid = blueprintPatcher.PhysicalDamageMaterialEnchantments[material];
            var enchantment = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponEnchantment>(guid);
            return enchantment != null ? enchantment.Description : "";
        }

        private static string ApplyVisualMapping(RecipeData recipe, BlueprintItem blueprint) {
            if (recipe?.VisualMappings != null) {
                foreach (var mapping in recipe.VisualMappings) {
                    if (blueprint.AssetGuid.StartsWith(mapping.Split(':')[0])) {
                        return mapping.Split(':')[1];
                    }
                }
            }

            return null;
        }

        private static string ApplyAnimationMapping(RecipeData recipe, BlueprintItem blueprint) {
            if (recipe?.AnimationMappings != null) {
                foreach (var mapping in recipe.AnimationMappings) {
                    if (blueprint.AssetGuid.StartsWith(mapping.Split(':')[0])) {
                        return mapping.Split(':')[1];
                    }
                }
            }

            return null;
        }

        private static string ApplyNameMapping(RecipeData recipe, BlueprintItem blueprint) {
            if (recipe?.NameMappings != null) {
                foreach (var mapping in recipe.NameMappings) {
                    var match = Regex.Match(blueprint.AssetGuid, mapping.Split(':')[0]);
                    if (match.Success) {
                        return new L10NString(mapping.Split(':')[1]).ToString();
                    }
                }
            }

            return null;
        }

        private static void RenderProjectsSection() {
            var caster = GetSelectedCrafter(false);
            if (caster == null) {
                return;
            }

            var timer = GetCraftingTimerComponentForCaster(caster.Descriptor);
            if (timer == null || timer.CraftingProjects.Count == 0) {
                UmmUiRenderer.RenderLabelRow($"{caster.CharacterName} is not currently working on any crafting projects.");
                return;
            }

            UmmUiRenderer.RenderLabelRow($"{caster.CharacterName} currently has {timer.CraftingProjects.Count} crafting projects in progress.");
            var firstItem = true;
            foreach (var project in timer.CraftingProjects.ToArray()) {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"   <b>{project.ResultItem.Name}</b> is {100 * project.Progress / project.TargetCost}% complete.",
                    GUILayout.Width(600f));
                if (GUILayout.Button("<color=red>✖</color>", GUILayout.ExpandWidth(false))) {
                    CancelCraftingProject(project);
                }

                if (firstItem) {
                    firstItem = false;
                } else if (GUILayout.Button("Move To Top", GUILayout.ExpandWidth(false))) {
                    timer.CraftingProjects.Remove(project);
                    timer.CraftingProjects.Insert(0, project);
                }
                GUILayout.EndHorizontal();
                UmmUiRenderer.RenderLabelRow($"       {BuildItemDescription(project.ResultItem).Replace("\n", "\n       ")}");
                UmmUiRenderer.RenderLabelRow($"       {project.LastMessage}");
            }
        }

        private static bool IsPlayerSomewhereSafe() {
            if (Game.Instance.CurrentlyLoadedArea != null && Areas.SafeBlueprintAreaGuids.Contains(Game.Instance.CurrentlyLoadedArea.AssetGuid)) {
                return true;
            }
            // Otherwise, check if they're in the capital.
            return IsPlayerInCapital();
        }

        private static bool IsPlayerInCapital() {
            // Detect if the player is in the capital, or in kingdom management from the throne room.
            return (Game.Instance.CurrentlyLoadedArea != null && Game.Instance.CurrentlyLoadedArea.IsCapital) ||
                   (Game.Instance.CurrentMode == GameModeType.Kingdom && KingdomTimelineManager.CanAdvanceTime());
        }

        public static UnitEntityData GetSelectedCrafter(bool render) {
            Selections.CurrentCaster = null;
            // Only allow remote companions if the player is in the capital.
            var remote = IsPlayerInCapital();
            var characters = UIUtility.GetGroup(remote).Where(character => character != null
                                                                           && (character.IsPlayerFaction
                                                                           && !character.Descriptor.IsPet
                                                                           && !character.Descriptor.State.IsDead
                                                                           && !character.Descriptor.State.IsFinallyDead))
                .ToArray();
            if (characters.Length == 0) {
                if (render) {
                    UmmUiRenderer.RenderLabelRow("No living characters available.");
                }

                return null;
            }

            const string label = "Crafter: ";
            var selectedSpellcasterIndex = Selections.GetSelectionIndex(label);
            if (render) {
                var partyNames = characters.Select(entity => $"{entity.CharacterName}" +
                                                             $"{((GetCraftingTimerComponentForCaster(entity.Descriptor)?.CraftingProjects.Any() ?? false) ? "*" : "")}")
                    .ToArray();
                selectedSpellcasterIndex = DrawSelectionUserInterfaceElements(label, partyNames, 8, ref Selections.UpgradingBlueprint);
            }
            if (selectedSpellcasterIndex >= characters.Length) {
                selectedSpellcasterIndex = 0;
            }
            return characters[selectedSpellcasterIndex];
        }

        /// <summary>Renders a selection of <typeparamref name="T" /> to Unity Mod Manager</summary>
        /// <typeparam name="T">Type of item being rendered</typeparam>
        /// <param name="label">Label for the selection</param>
        /// <param name="options">Options for the selection</param>
        /// <param name="horizontalCount">How many elements to fit in the horizontal direction</param>
        public static int DrawSelectionUserInterfaceElements(string label, string[] options, int horizontalCount)
        {
            var dummy = "";
            return DrawSelectionUserInterfaceElements(label, options, horizontalCount, ref dummy);
        }

        public static int DrawSelectionUserInterfaceElements<T>(string label, string[] options, int horizontalCount, ref T emptyOnChange, bool addSpace = true)
        {
            var index = Selections.GetSelectionIndex(label);
            if (index >= options.Length)
            {
                index = 0;
            }

            var newIndex = UmmUiRenderer.RenderSelection(label, options, index, horizontalCount, addSpace);

            if (index != newIndex)
            {
                emptyOnChange = default(T);
            }

            Selections.SetSelectionIndex(label, newIndex);

            return newIndex;
        }

        public static void AddItemBlueprintForSpell(UsableItemType itemType, BlueprintItemEquipment itemBlueprint) {
            if (!LoadedData.SpellIdToItem.ContainsKey(itemType)) {
                LoadedData.SpellIdToItem.Add(itemType, new Dictionary<string, List<BlueprintItemEquipment>>());
            }

            if (!LoadedData.SpellIdToItem[itemType].ContainsKey(itemBlueprint.Ability.AssetGuid)) {
                LoadedData.SpellIdToItem[itemType][itemBlueprint.Ability.AssetGuid] = new List<BlueprintItemEquipment>();
            }

            LoadedData.SpellIdToItem[itemType][itemBlueprint.Ability.AssetGuid].Add(itemBlueprint);
        }

        public static List<BlueprintItemEquipment> FindItemBlueprintsForSpell(BlueprintScriptableObject spell, UsableItemType itemType) {
            if (!LoadedData.SpellIdToItem.ContainsKey(itemType)) {
#if PATCH21_BETA
                var allUsableItems = ResourcesLibrary.GetBlueprints<BlueprintItemEquipmentUsable>();
#else
                var allUsableItems = Resources.FindObjectsOfTypeAll<BlueprintItemEquipmentUsable>();
#endif
                foreach (var item in allUsableItems) {
                    if (item.Type == itemType) {
                        AddItemBlueprintForSpell(itemType, item);
                    }
                }
            }

            return LoadedData.SpellIdToItem[itemType].ContainsKey(spell.AssetGuid) ? LoadedData.SpellIdToItem[itemType][spell.AssetGuid] : null;
        }

        public static void AddItemForType(BlueprintItem blueprint) {
            string assetGuid = GetBlueprintItemType(blueprint);
            if (!string.IsNullOrEmpty(assetGuid)) {
                LoadedData.TypeToItem.Add(assetGuid, blueprint);
            }
        }

        public static void AddItemIdForEnchantment(BlueprintItemEquipment itemBlueprint) {
            if (itemBlueprint != null) {
                foreach (var enchantment in GetEnchantments(itemBlueprint)) {
                    if (!LoadedData.EnchantmentIdToItem.ContainsKey(enchantment.AssetGuid)) {
                        LoadedData.EnchantmentIdToItem[enchantment.AssetGuid] = new List<BlueprintItemEquipment>();
                    }

                    LoadedData.EnchantmentIdToItem[enchantment.AssetGuid].Add(itemBlueprint);
                }
            }
        }

        public static void AddRecipeForEnchantment(string enchantmentId, RecipeData recipe) {
            if (!LoadedData.EnchantmentIdToRecipe.ContainsKey(enchantmentId)) {
                LoadedData.EnchantmentIdToRecipe.Add(enchantmentId, new List<RecipeData>());
            }

            if (!LoadedData.EnchantmentIdToRecipe[enchantmentId].Contains(recipe)) {
                LoadedData.EnchantmentIdToRecipe[enchantmentId].Add(recipe);
            }
        }

        public static void AddRecipeForMaterial(PhysicalDamageMaterial material, RecipeData recipe) {
            if (!LoadedData.MaterialToRecipe.ContainsKey(material)) {
                LoadedData.MaterialToRecipe.Add(material, new List<RecipeData>());
            }
            if (!LoadedData.MaterialToRecipe[material].Contains(recipe)) {
                LoadedData.MaterialToRecipe[material].Add(recipe);
            }
        }

        private static IEnumerable<BlueprintItemEquipment> FindItemBlueprintForEnchantmentId(string assetGuid) {
            return LoadedData.EnchantmentIdToItem.ContainsKey(assetGuid) ? LoadedData.EnchantmentIdToItem[assetGuid] : null;
        }

        public static bool CharacterHasFeat(UnitEntityData caster, string featGuid) {
            return caster.Descriptor.Progression.Features.Enumerable.Any(feat => feat.Blueprint.AssetGuid == featGuid);
        }

        private static BlueprintItemEquipment RandomBaseBlueprintId(ItemCraftingData itemData, Func<BlueprintItemEquipment, bool> selector = null) {
            var blueprintIds = selector == null ? itemData.NewItemBaseIDs : itemData.NewItemBaseIDs.Where(selector).ToArray();
            return blueprintIds[RandomGenerator.Next(blueprintIds.Length)];
        }

        private static void CraftItem(ItemEntity resultItem, ItemEntity upgradeItem = null) {
            var characters = UIUtility.GetGroup(true).Where(character => character.IsPlayerFaction && !character.Descriptor.IsPet);
            foreach (var character in characters) {
                var bondedComponent = GetBondedItemComponentForCaster(character.Descriptor);
                if (bondedComponent && bondedComponent.ownerItem == upgradeItem) {
                    bondedComponent.ownerItem = resultItem;
                }
            }

            using (new DisableBattleLog(!ModSettings.CraftingTakesNoTime)) {
                var holdingSlot = upgradeItem?.HoldingSlot;
                var slotIndex = upgradeItem?.InventorySlotIndex;
                var inventory = true;
                if (upgradeItem != null) {
                    if (Game.Instance.Player.Inventory.Contains(upgradeItem)) {
                        Game.Instance.Player.Inventory.Remove(upgradeItem);
                    } else {
                        Game.Instance.Player.SharedStash.Remove(upgradeItem);
                        inventory = false;
                    }
                }
                if (holdingSlot == null) {
                    if (inventory) {
                        Game.Instance.Player.Inventory.Add(resultItem);
                    } else {
                        Game.Instance.Player.SharedStash.Add(resultItem);
                    }
                    if (slotIndex is int value) {
                        resultItem.SetSlotIndex(value);
                    }
                } else {
                    holdingSlot.InsertItem(resultItem);
                }
            }

            if (resultItem is ItemEntityUsable usable) {
                switch (usable.Blueprint.Type) {
                    case UsableItemType.Scroll:
                        Game.Instance.UI.Common.UISound.Play(UISoundType.NewInformation);
                        break;
                    case UsableItemType.Potion:
                        Game.Instance.UI.Common.UISound.PlayItemSound(SlotAction.Take, resultItem, false);
                        break;
                    default:
                        Game.Instance.UI.Common.UISound.Play(UISoundType.SettlementBuildStart);
                        break;
                }
            } else {
                Game.Instance.UI.Common.UISound.Play(UISoundType.SettlementBuildStart);
            }
        }

        private static int CalculateSpellBasedGoldCost(SpellBasedItemCraftingData craftingData, int spellLevel, int casterLevel) {
            return spellLevel == 0 ? craftingData.BaseItemGoldCost * casterLevel / 8 : craftingData.BaseItemGoldCost * spellLevel * casterLevel / 4;
        }

        private static bool BuildCostString(out string cost, ItemCraftingData craftingData, int goldCost,
            IEnumerable<BlueprintAbility> spellBlueprintArray = null,
            BlueprintItem resultBlueprint = null, BlueprintItem upgradeBlueprint = null) {
            var canAfford = true;
            if (ModSettings.CraftingCostsNoGold) {
                cost = new L10NString("craftMagicItems-label-cost-free");
            } else {
                canAfford = (Game.Instance.Player.Money >= goldCost);
                var notAffordGold = canAfford ? "" : new L10NString("craftMagicItems-label-cost-gold-too-much");
                cost = LocalizationHelper.FormatLocalizedString("craftMagicItems-label-cost-gold", goldCost, notAffordGold);
                var itemTotals = new Dictionary<BlueprintItem, int>();
                if (spellBlueprintArray != null) {
                    foreach (var spellBlueprint in spellBlueprintArray) {
                        if (spellBlueprint.MaterialComponent.Item) {
                            var count = spellBlueprint.MaterialComponent.Count *
                                        GetMaterialComponentMultiplier(craftingData, resultBlueprint, upgradeBlueprint);
                            if (count > 0) {
                                if (itemTotals.ContainsKey(spellBlueprint.MaterialComponent.Item)) {
                                    itemTotals[spellBlueprint.MaterialComponent.Item] += count;
                                } else {
                                    itemTotals[spellBlueprint.MaterialComponent.Item] = count;
                                }
                            }
                        }
                    }
                }

                foreach (var pair in itemTotals) {
                    var notAffordItems = "";
                    if (!Game.Instance.Player.Inventory.Contains(pair.Key, pair.Value)) {
                        canAfford = false;
                        notAffordItems = new L10NString("craftMagicItems-label-cost-items-too-much");
                    }

                    cost += LocalizationHelper.FormatLocalizedString("craftMagicItems-label-cost-gold-and-items", pair.Value, pair.Key.Name, notAffordItems);
                }
            }

            return canAfford;
        }

        private static void AddNewProject(UnitDescriptor casterDescriptor, CraftingProjectData project) {
            var craftingProjects = GetCraftingTimerComponentForCaster(casterDescriptor, true);
            craftingProjects.AddProject(project);
            if (project.UpgradeItem == null) {
                ItemCreationProjects.Add(project);
            } else {
                ItemUpgradeProjects[project.UpgradeItem] = project;
            }
        }

        private static void CalculateProjectEstimate(CraftingProjectData project) {
            var craftingData = LoadedData.ItemCraftingData.FirstOrDefault(data => data.Name == project.ItemType);
            StatType craftingSkill;
            int dc;
            int progressRate;
            if (project.ItemType == BondedItemRitual) {
                craftingSkill = StatType.SkillKnowledgeArcana;
                dc = 10 + project.Crafter.Stats.GetStat(craftingSkill).ModifiedValue;
                progressRate = ModSettings.MagicCraftingRate;
            } else if (IsMundaneCraftingData(craftingData)) {
                craftingSkill = StatType.SkillKnowledgeWorld;
                var recipeBasedItemCraftingData = (RecipeBasedItemCraftingData) craftingData;
                dc = CalculateMundaneCraftingDC(recipeBasedItemCraftingData, project.ResultItem.Blueprint, project.Crafter.Descriptor);
                progressRate = ModSettings.MundaneCraftingRate;
            } else {
                craftingSkill = StatType.SkillKnowledgeArcana;
                dc = 5 + project.CasterLevel;
                progressRate = ModSettings.MagicCraftingRate;
            }

            var skillMargin = RenderCraftingSkillInformation(project.Crafter, craftingSkill, dc, project.CasterLevel, project.SpellPrerequisites,
                project.FeatPrerequisites, project.AnyPrerequisite, project.CrafterPrerequisites, false);
            var progressPerDayCapital = (int) (progressRate * (1 + (float) skillMargin / 5));
            GameLogContext.Count = (project.TargetCost + progressPerDayCapital - 1) / progressPerDayCapital;
            if (ModSettings.CraftAtFullSpeedWhileAdventuring) {
                project.AddMessage(new L10NString("craftMagicItems-time-estimate-single-rate"));
            } else {
                var progressPerDayAdventuring = (int) (progressRate * (1 + (float) skillMargin / 5) / DifficultyClass.AdventuringProgressPenalty);
                var adventuringDayCount = (project.TargetCost + progressPerDayAdventuring - 1) / progressPerDayAdventuring;
                project.AddMessage(adventuringDayCount == 1
                    ? new L10NString("craftMagicItems-time-estimate-one-day")
                    : LocalizationHelper.FormatLocalizedString("craftMagicItems-time-estimate-adventuring-capital", adventuringDayCount));
            }
            GameLogContext.Clear();

            AddBattleLogMessage(project.LastMessage);
        }

        private static void AttemptSpellBasedCraftItemAndRender(UnitEntityData caster, SpellBasedItemCraftingData craftingData,
            AbilityData spell, BlueprintAbility spellBlueprint, int spellLevel, int casterLevel)
        {
            var itemBlueprintList = FindItemBlueprintsForSpell(spellBlueprint, craftingData.UsableItemType);
            if (itemBlueprintList == null && craftingData.NewItemBaseIDs == null)
            {
                var message = LocalizationHelper.FormatLocalizedString("craftMagicItems-label-no-item-exists", new L10NString(craftingData.NamePrefixId), spellBlueprint.Name);
                UmmUiRenderer.RenderLabel(message);
                return;
            }

            var existingItemBlueprint = itemBlueprintList?.Find(bp => bp.SpellLevel == spellLevel && bp.CasterLevel == casterLevel);
            var requiredProgress = CalculateSpellBasedGoldCost(craftingData, spellLevel, casterLevel);
            var goldCost = (int) Mathf.Round(requiredProgress * ModSettings.CraftingPriceScale);
            var canAfford = BuildCostString(out var cost, craftingData, goldCost, new[] {spellBlueprint});
            var custom = existingItemBlueprint == null || existingItemBlueprint.AssetGuid.Contains(CraftMagicItemsBlueprintPatcher.BlueprintPrefix)
                ? new L10NString("craftMagicItems-label-custom").ToString()
                : "";
            var label = LocalizationHelper.FormatLocalizedString("craftMagicItems-label-craft-spell-item", custom, new L10NString(craftingData.NamePrefixId), spellBlueprint.Name, cost);

            //if the player cannot afford the time (not enough gold), alert them
            if (!canAfford)
            {
                UmmUiRenderer.RenderLabel(label);
            }
            // ... otherwise let them spend their money
            else if (GUILayout.Button(label, GUILayout.ExpandWidth(false)))
            {
                BeginCraftingSpellBasedItem(caster, craftingData, spell, spellBlueprint, spellLevel, casterLevel, itemBlueprintList, existingItemBlueprint, requiredProgress, goldCost, cost);
            }
        }

        private static void BeginCraftingSpellBasedItem(UnitEntityData caster, SpellBasedItemCraftingData craftingData,
            AbilityData spell, BlueprintAbility spellBlueprint, Int32 spellLevel, Int32 casterLevel,
            List<BlueprintItemEquipment> itemBlueprintList, BlueprintItemEquipment existingItemBlueprint,
            Int32 requiredProgress, Int32 goldCost, String cost)
        {
            BlueprintItem itemBlueprint;
            if (itemBlueprintList == null)
            {
                // No items for that spell exist at all - create a custom blueprint with casterLevel, spellLevel and spellId
                var blueprintId = blueprintPatcher.BuildCustomSpellItemGuid(RandomBaseBlueprintId(craftingData).AssetGuid,
                    casterLevel, spellLevel, spellBlueprint.AssetGuid);
                itemBlueprint = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(blueprintId);
            }
            else if (existingItemBlueprint == null)
            {
                // No item for this spell & caster level - create a custom blueprint with casterLevel and optionally SpellLevel
                var blueprintId = blueprintPatcher.BuildCustomSpellItemGuid(itemBlueprintList[0].AssetGuid, casterLevel,
                    itemBlueprintList[0].SpellLevel == spellLevel ? -1 : spellLevel);
                itemBlueprint = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(blueprintId);
            }
            else
            {
                // Item with matching spell, level and caster level exists.  Use that.
                itemBlueprint = existingItemBlueprint;
            }

            if (itemBlueprint == null)
            {
                throw new Exception(
                    $"Unable to build blueprint for spellId {spellBlueprint.AssetGuid}, spell level {spellLevel}, caster level {casterLevel}");
            }

            // Pay gold and material components up front.
            if (ModSettings.CraftingCostsNoGold)
            {
                goldCost = 0;
            }
            else
            {
                Game.Instance.UI.Common.UISound.Play(UISoundType.LootCollectGold);
                Game.Instance.Player.SpendMoney(goldCost);
                if (spellBlueprint.MaterialComponent.Item != null)
                {
                    Game.Instance.Player.Inventory.Remove(spellBlueprint.MaterialComponent.Item,
                        spellBlueprint.MaterialComponent.Count * craftingData.Charges);
                }
            }

            var resultItem = BuildItemEntity(itemBlueprint, craftingData, caster);
            AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-begin-crafting", cost, itemBlueprint.Name), resultItem);
            if (ModSettings.CraftingTakesNoTime)
            {
                AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-expend-spell", spell.Name));
                spell.SpendFromSpellbook();
                CraftItem(resultItem);
            }
            else
            {
                var project = new CraftingProjectData(caster, requiredProgress, goldCost, casterLevel, resultItem, craftingData.Name, null,
                    new[] { spellBlueprint });
                AddNewProject(caster.Descriptor, project);
                CalculateProjectEstimate(project);
                Selections.CurrentSection = OpenSection.ProjectsSection;
            }
        }

        private static bool IsMundaneCraftingData(ItemCraftingData craftingData) {
            return craftingData.FeatGuid == null;
        }

        private static void RenderRecipeBasedCraftItemControl(UnitEntityData caster, ItemCraftingData craftingData, RecipeData recipe, int casterLevel,
            BlueprintItem itemBlueprint, ItemEntity upgradeItem = null) {
            int baseCost = (craftingData.Count != 0 ? craftingData.Count : 1) * itemBlueprint.Cost;
            int upgradeCost = (craftingData.Count != 0 ? craftingData.Count : 1) * (upgradeItem?.Blueprint.Cost ?? 0);
            int materialComponentCost = recipe?.MaterialComponentCost ?? 0;
            var requiredProgress = (baseCost - upgradeCost - materialComponentCost) / 4;
            var goldCost = materialComponentCost + (int)Mathf.Round(requiredProgress * ModSettings.CraftingPriceScale);
            if (IsMundaneCraftingData(craftingData)) {
                // For mundane crafting, the gold cost is less, and the cost of the recipes don't increase the required progress.
                goldCost = Math.Max(1, (goldCost * 2 + 2) / 3);
                var recipeCost = 0;
                foreach (var enchantment in itemBlueprint.Enchantments) {
                    var enchantmentRecipe = FindSourceRecipe(enchantment.AssetGuid, itemBlueprint);
                    recipeCost += enchantmentRecipe?.CostFactor ?? 0;
                }

                if (itemBlueprint is BlueprintItemEquipmentUsable) {
                    if (craftingData.Count > 0) {
                        recipeCost = craftingData.Count * (itemBlueprint.Cost - 2);
                    }
                } else if (itemBlueprint is BlueprintItemShield shield) {
                    if (shield.WeaponComponent && shield.WeaponComponent.DamageType.Physical.Material != 0 && (shield.WeaponComponent.DamageType.Physical.Form & PhysicalDamageForm.Piercing) != 0) {
                        recipeCost += GetSpecialMaterialCost(shield.WeaponComponent.DamageType.Physical.Material, shield.WeaponComponent, 10, 5.0f);
                    }
                } else {
                    if (itemBlueprint is BlueprintItemWeapon weapon && weapon.DamageType.Physical.Material != 0) {
                        var standardBlueprint = GetStandardItem(itemBlueprint);
                        var blueprintCost = standardBlueprint.Cost;
                        var blueprintWeight = standardBlueprint.Weight;
                        foreach (var enchantment in itemBlueprint.Enchantments) {
                            if (enchantment.AssetGuid.StartsWith(ItemQualityBlueprints.OversizedGuid)) {
                                var weaponBaseSizeChange = enchantment.GetComponent<WeaponBaseSizeChange>();
                                if (weaponBaseSizeChange != null) {
                                    var sizeCategoryChange = weaponBaseSizeChange.SizeCategoryChange;
                                    if (sizeCategoryChange > 0) {
                                        blueprintCost *= 2;
                                        blueprintWeight *= 2;
                                    } else if (sizeCategoryChange < 0) {
                                        blueprintWeight /= 2.0f;
                                    }
                                }
                            }
                        }
                        recipeCost += GetSpecialMaterialCost(weapon.DamageType.Physical.Material, weapon, blueprintCost, blueprintWeight);
                    }

                    if (itemBlueprint is BlueprintItemWeapon doubleWeapon && doubleWeapon.Double) {
                        foreach (var enchantment in doubleWeapon.SecondWeapon.Enchantments) {
                            var enchantmentRecipe = FindSourceRecipe(enchantment.AssetGuid, itemBlueprint);
                            recipeCost += enchantmentRecipe?.CostFactor ?? 0;
                        }

                        if (doubleWeapon.SecondWeapon.DamageType.Physical.Material != 0) {
                            recipeCost += GetSpecialMaterialCost(doubleWeapon.DamageType.Physical.Material, doubleWeapon.SecondWeapon, 0, 0.0f);
                        }
                    }
                }

                requiredProgress = Math.Max(1, requiredProgress - recipeCost / 4);
            }

            var canAfford = BuildCostString(out var cost, craftingData, goldCost, recipe?.PrerequisiteSpells ?? new BlueprintAbility[0],
                itemBlueprint, upgradeItem?.Blueprint);
            var custom = itemBlueprint.AssetGuid.Contains(CraftMagicItemsBlueprintPatcher.BlueprintPrefix)
                ? new L10NString("craftMagicItems-label-custom").ToString()
                : "";
            var label = upgradeItem == null
                ? LocalizationHelper.FormatLocalizedString("craftMagicItems-label-craft-item", custom, itemBlueprint.Name, cost)
                : itemBlueprint is BlueprintItemWeapon otherWeapon && otherWeapon.Double
                ? LocalizationHelper.FormatLocalizedString("craftMagicItems-label-upgrade-weapon-double", upgradeItem.Blueprint.Name, custom, itemBlueprint.Name, otherWeapon.SecondWeapon.Name, cost)
                : LocalizationHelper.FormatLocalizedString("craftMagicItems-label-upgrade-item", upgradeItem.Blueprint.Name, custom, itemBlueprint.Name, cost);
            if (!canAfford) {
                GUILayout.Label(label);
            } else if (GUILayout.Button(label, GUILayout.ExpandWidth(false))) {
                // Pay gold and material components up front.
                if (ModSettings.CraftingCostsNoGold) {
                    goldCost = 0;
                } else {
                    Game.Instance.UI.Common.UISound.Play(UISoundType.LootCollectGold);
                    Game.Instance.Player.SpendMoney(goldCost);
                    var factor = GetMaterialComponentMultiplier(craftingData, itemBlueprint, upgradeItem?.Blueprint);
                    if (factor > 0 && recipe != null) {
                        foreach (var prerequisite in recipe.PrerequisiteSpells) {
                            if (prerequisite.MaterialComponent.Item != null) {
                                Game.Instance.Player.Inventory.Remove(prerequisite.MaterialComponent.Item, prerequisite.MaterialComponent.Count * factor);
                            }
                        }
                    }
                }

                var resultItem = BuildItemEntity(itemBlueprint, craftingData, caster);
                AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-begin-crafting", cost, itemBlueprint.Name), resultItem);
                if (ModSettings.CraftingTakesNoTime) {
                    CraftItem(resultItem, upgradeItem);
                } else {
                    var project = new CraftingProjectData(caster, requiredProgress, goldCost, casterLevel, resultItem, craftingData.Name, recipe?.Name,
                        recipe?.PrerequisiteSpells ?? new BlueprintAbility[0], recipe?.PrerequisiteFeats ?? Array.Empty<BlueprintFeature>(), recipe?.PrerequisitesMandatory ?? false,
                        recipe?.AnyPrerequisite ?? false, upgradeItem, recipe?.CrafterPrerequisites ?? new CrafterPrerequisiteType[0]);
                    AddNewProject(caster.Descriptor, project);
                    CalculateProjectEstimate(project);
                    Selections.CurrentSection = OpenSection.ProjectsSection;
                }

                // Reset base blueprint for next item
                Selections.SelectedBaseBlueprint = null;
                // And stop upgrading the item, if relevant.
                Selections.UpgradingBlueprint = null;
            }
        }

        public static LocalizedString BuildCustomRecipeItemDescription(BlueprintItem blueprint, IList<BlueprintItemEnchantment> enchantments,
            IList<BlueprintItemEnchantment> skipped, IList<BlueprintItemEnchantment> removed, bool replaceAbility, string ability, int casterLevel, int perDay) {
            var extraDescription = enchantments
                .Select(enchantment => {
                    var recipe = FindSourceRecipe(enchantment.AssetGuid, blueprint);
                    if (recipe == null) {
                        if (skipped.Contains(enchantment)) {
                            return "";
                        } else if (!string.IsNullOrEmpty(enchantment.Name)) {
                            return enchantment.Name;
                        } else {
                            return "Unknown";
                        }
                    } else if (recipe.Enchantments.Length <= 1) {
                        if (skipped.Contains(enchantment)) {
                            return "";
                        } else {
                            if (!string.IsNullOrEmpty(enchantment.Name)) {
                                return enchantment.Name;
                            } else {
                                return recipe.NameId;
                            }
                        }
                    }
                    var newBonus = recipe.Enchantments.FindIndex(e => e == enchantment) + 1;
                    var bonusString = GetBonusString(newBonus, recipe);
                    var bonusDescription = recipe.BonusTypeId != null
                        ? LocalizationHelper.FormatLocalizedString("craftMagicItems-custom-description-bonus-to", new L10NString(recipe.BonusTypeId), recipe.NameId)
                        : recipe.BonusToId != null
                            ? LocalizationHelper.FormatLocalizedString("craftMagicItems-custom-description-bonus-to", recipe.NameId, new L10NString(recipe.BonusToId))
                            : LocalizationHelper.FormatLocalizedString("craftMagicItems-custom-description-bonus", recipe.NameId);
                    var upgradeFrom = removed.FirstOrDefault(remove => FindSourceRecipe(remove.AssetGuid, blueprint) == recipe);
                    var oldBonus = int.MaxValue;
                    if (upgradeFrom != null) {
                        oldBonus = recipe.Enchantments.FindIndex(e => e == upgradeFrom) + 1;
                    }
                    if (oldBonus > newBonus) {
                        if (skipped.Contains(enchantment)) {
                            return new L10NString("");
                        } else {
                            return LocalizationHelper.FormatLocalizedString("craftMagicItems-custom-description-enchantment-template", bonusString, bonusDescription);
                        }
                    } else {
                        removed.Remove(upgradeFrom);
                    }
                    return LocalizationHelper.FormatLocalizedString("craftMagicItems-custom-description-enchantment-upgrade-template", bonusDescription,
                        GetBonusString(oldBonus, recipe), bonusString);
                })
                .OrderBy(enchantmentDescription => enchantmentDescription)
                .Select(enchantmentDescription => string.IsNullOrEmpty(enchantmentDescription) ? "" : "\n* " + enchantmentDescription)
                .Join("");
            if (blueprint is BlueprintItemEquipment equipment && (ability != null && ability != "null" || casterLevel > -1 || perDay > -1)) {
                GameLogContext.Count = equipment.Charges;
                extraDescription += "\n* " + (equipment.Charges == 1 ? LocalizationHelper.FormatLocalizedString("craftMagicItems-label-cast-spell-n-times-details-single", equipment.Ability.Name, equipment.CasterLevel) :
                    LocalizationHelper.FormatLocalizedString("craftMagicItems-label-cast-spell-n-times-details-multiple", equipment.Ability.Name, equipment.CasterLevel, equipment.Charges));
                GameLogContext.Clear();
            }

            string description;
            if (removed.Count == 0 && !replaceAbility) {
                description = blueprint.Description;
                if (extraDescription.Length > 0) {
                    description += new L10NString("craftMagicItems-custom-description-additional") + extraDescription;
                }
            } else if (extraDescription.Length > 0) {
                description = new L10NString("craftMagicItems-custom-description-start") + extraDescription;
            } else {
                description = "";
            }

            return new FakeL10NString(description);
        }

        private static int ItemPlus(BlueprintItem blueprint) {
            switch (blueprint) {
                case BlueprintItemWeapon weapon:
                    foreach (var enchantment in weapon.Enchantments) {
                        var weaponBonus = enchantment.GetComponent<WeaponEnhancementBonus>();
                        if (weaponBonus != null) {
                            return weaponBonus.EnhancementBonus;
                        }
                    }

                    break;
                case BlueprintItemArmor armor:
                    foreach (var enchantment in armor.Enchantments) {
                        var armorBonus = enchantment.GetComponent<ArmorEnhancementBonus>();
                        if (armorBonus != null) {
                            return armorBonus.EnhancementValue;
                        }
                    }

                    break;
                case BlueprintItemShield shield:
                    return Math.Max(ItemPlus(shield.ArmorComponent), ItemPlus(shield.WeaponComponent));
            }

            return 0;
        }
        private static int ItemMaxEnchantmentLevel(BlueprintItem blueprint) {
            if (blueprint is BlueprintItemWeapon || blueprint is BlueprintItemArmor || blueprint is BlueprintItemShield) {
                return 10;
            } else {
                return 5;
            }
        }

        public static int ItemPlusEquivalent(BlueprintItem blueprint) {
            if (blueprint == null || blueprint.Enchantments == null) {
                return 0;
            }

            var enhancementLevel = 0;
            var cumulative = new Dictionary<RecipeData, int>();
            foreach (var enchantment in blueprint.Enchantments) {
                if (LoadedData.EnchantmentIdToRecipe.ContainsKey(enchantment.AssetGuid)) {
                    var recipe = FindSourceRecipe(enchantment.AssetGuid, blueprint);
                    if (recipe != null && recipe.CostType == RecipeCostType.EnhancementLevelSquared) {
                        var level = recipe.Enchantments.FindIndex(e => e == enchantment) + 1;
                        if (recipe.EnchantmentsCumulative) {
                            cumulative[recipe] = cumulative.ContainsKey(recipe) ? Math.Max(level, cumulative[recipe]) : level;
                        } else {
                            enhancementLevel += GetPlusOfRecipe(recipe, level);
                        }
                    }
                }
            }

            foreach (var recipeLevelPair in cumulative) {
                enhancementLevel += GetPlusOfRecipe(recipeLevelPair.Key, recipeLevelPair.Value);
            }

            return enhancementLevel;
        }

        private static bool IsItemLegalEnchantmentLevel(BlueprintItem blueprint) {
            if (blueprint == null || ModSettings.IgnorePlusTenItemMaximum) {
                return true;
            }

            if (blueprint is BlueprintItemShield shield) {
                return IsItemLegalEnchantmentLevel(shield.ArmorComponent) && IsItemLegalEnchantmentLevel(shield.WeaponComponent);
            }

            return ItemPlusEquivalent(blueprint) <= ItemMaxEnchantmentLevel(blueprint);
        }

        private static int GetEnchantmentCost(string enchantmentId, BlueprintItem blueprint) {
            var recipe = FindSourceRecipe(enchantmentId, blueprint);
            if (recipe != null) {
                var index = recipe.Enchantments.FindIndex(enchantment => enchantment.AssetGuid == enchantmentId);
                var casterLevel = recipe.CasterLevelStart + index * recipe.CasterLevelMultiplier;
                var epicFactor = casterLevel > 20 ? 2 : 1;
                switch (recipe.CostType) {
                    case RecipeCostType.Flat:
                        return recipe.CostFactor * epicFactor;
                    case RecipeCostType.Mult:
                        var baseBlueprint = GetBaseBlueprint(blueprint);
                        return baseBlueprint.Cost * (recipe.CostFactor - 1) * epicFactor;
                    case RecipeCostType.CasterLevel:
                        return recipe.CostFactor * casterLevel * epicFactor;
                    case RecipeCostType.LevelSquared:
                        return recipe.CostFactor * (index + 1) * (index + 1) * epicFactor;
                    default:
                        return 0;
                }
            }

            return LoadedData.EnchantmentIdToCost.ContainsKey(enchantmentId) ? LoadedData.EnchantmentIdToCost[enchantmentId] : 0;
        }

        private static int GetSpecialMaterialCost(PhysicalDamageMaterial material, BlueprintItemWeapon weapon, int baseCost, float weight) {
            switch (material) {
                case PhysicalDamageMaterial.Adamantite:
                    return DefaultCosts.Adamantine - DefaultCosts.WeaponMasterworkCost; // Cost of masterwork is subsumed by the cost of adamantite
                case PhysicalDamageMaterial.ColdIron:
                    var enhancementLevel = ItemPlusEquivalent(weapon);
                    // Cold Iron weapons cost double, excluding the masterwork component and 2000 extra for enchanting the first +1
                    // double weapon
                    return baseCost + (enhancementLevel > 0 ? DefaultCosts.WeaponPlusCost : 0);
                case PhysicalDamageMaterial.Silver:
                    // PhysicalDamageMaterial.Silver is really Mithral.
                    // Non-armor Mithral items cost 500 gp per pound of the original, non-Mithral item, which
                    // translates to 1000 gp per pound of Mithral.  See https://paizo.com/paizo/faq/v5748nruor1fm#v5748eaic9r9u
                    // Only charge for weight on the primary half

                    // TODO: this is wrong and only applies to weapons and wondrous items.
                    //      see: https://www.d20pfsrd.com/equipment/special-materials/#Mithral
                    //      Type of Item	Item Cost Modifier
                    //      Light armor	    +1,000 gp
                    //      Medium armor	+4,000 gp
                    //      Heavy armor	    +9,000 gp
                    //      Shield	        +1,000 gp
                    //      Other items	    +500 gp/lb.
                    return (int)(DefaultCosts.MithralPerPound * weight) - DefaultCosts.WeaponMasterworkCost; // Cost of masterwork is subsumed by the cost of mithral
                default:
                    return 0;
            }
        }

        public static int RulesRecipeItemCost(BlueprintItem blueprint, int baseCost = -1, float weight = 0.0f) {
            if (blueprint == null) {
                return 0;
            }

            if (baseCost == -1) {
                var standardBlueprint = GetStandardItem(blueprint);
                if (standardBlueprint != null) {
                    baseCost = standardBlueprint.Cost;
                    weight = standardBlueprint.Weight;
                } else {
                    baseCost = 0;
                    weight = 0;
                }
            }

            if (blueprint is BlueprintItemShield shield) {
                var armorCost = RulesRecipeItemCost(shield.ArmorComponent, baseCost: baseCost);
                if (shield.WeaponComponent != null && (shield.WeaponComponent.DamageType.Physical.Form & PhysicalDamageForm.Piercing) != 0) {
                    return armorCost + RulesRecipeItemCost(shield.WeaponComponent, 10, 5.0f);
                } else {
                    return armorCost + RulesRecipeItemCost(shield.WeaponComponent, 0, 0.6f);
                }
            }

            var mostExpensiveEnchantmentCost = 0;
            var mithralArmorEnchantmentGuid = false;
            var cost = 0;
            foreach (var enchantment in blueprint.Enchantments) {
                if (enchantment.AssetGuid == ItemQualityBlueprints.MithralArmorEnchantmentGuid) {
                    mithralArmorEnchantmentGuid = true;
                } else if (enchantment.AssetGuid.StartsWith(ItemQualityBlueprints.OversizedGuid)) {
                    var weaponBaseSizeChange = enchantment.GetComponent<WeaponBaseSizeChange>();
                    if (weaponBaseSizeChange != null) {
                        var sizeCategoryChange = weaponBaseSizeChange.SizeCategoryChange;
                        if (sizeCategoryChange > 0) {
                            baseCost *= 2;
                            weight *= 2.0f;
                        } else if (sizeCategoryChange < 0) {
                            weight /= 2.0f;
                        }
                    }
                }
                var recipe = FindSourceRecipe(enchantment.AssetGuid, blueprint);
                if (recipe != null && recipe.CostType != RecipeCostType.EnhancementLevelSquared && RecipeAppliesToBlueprint(recipe, blueprint)) {
                    var enchantmentCost = GetEnchantmentCost(enchantment.AssetGuid, blueprint);
                    cost += enchantmentCost;
                    if (mostExpensiveEnchantmentCost < enchantmentCost) {
                        mostExpensiveEnchantmentCost = enchantmentCost;
                    }
                }
            }

            if (blueprint is BlueprintItemEquipment equipment && equipment.Ability != null && equipment.Ability.IsSpell && equipment.RestoreChargesOnRest) {
                var castSpellCost = (int)(equipment.Charges * equipment.CasterLevel * 360 * (equipment.SpellLevel == 0 ? 0.5 : equipment.SpellLevel));
                cost += castSpellCost;
                if (mostExpensiveEnchantmentCost < castSpellCost) {
                    mostExpensiveEnchantmentCost = castSpellCost;
                }
            }

            if (blueprint is BlueprintItemArmor || blueprint is BlueprintItemWeapon) {
                var enhancementLevel = ItemPlusEquivalent(blueprint);
                if (blueprint is BlueprintItemWeapon weapon) {
                    if (enhancementLevel > 0) {
                        cost += DefaultCosts.WeaponMasterworkCost;
                    }
                    if (weapon.DamageType.Physical.Material != 0) {
                        cost += GetSpecialMaterialCost(weapon.DamageType.Physical.Material, weapon, baseCost, weight);
                    }
                } else if (blueprint is BlueprintItemArmor) {
                    if (enhancementLevel > 0 && !mithralArmorEnchantmentGuid) {
                        cost += DefaultCosts.ArmorMasterworkCost;
                    }
                }

                var factor = blueprint is BlueprintItemWeapon ? DefaultCosts.WeaponPlusCost : DefaultCosts.ArmorPlusCost;
                cost += enhancementLevel * enhancementLevel * factor;
                if (blueprint is BlueprintItemWeapon doubleWeapon && doubleWeapon.Double) {
                    return baseCost + cost + RulesRecipeItemCost(doubleWeapon.SecondWeapon, 0, 0.0f);
                }
                return baseCost + cost;
            }

            if (ItemPlusEquivalent(blueprint) > 0) {
                var enhancementLevel = ItemPlusEquivalent(blueprint);
                var enhancementCost = enhancementLevel * enhancementLevel * DefaultCosts.UnarmedPlusCost;
                cost += enhancementCost;
                if (mostExpensiveEnchantmentCost < enhancementCost) {
                    mostExpensiveEnchantmentCost = enhancementCost;
                }
            }

            // Usable (belt slot) items cost double.
            return (3 * (baseCost + cost) - mostExpensiveEnchantmentCost) / (blueprint is BlueprintItemEquipmentUsable ? 1 : 2);
        }

        // Attempt to work out the cost of enchantments which aren't in recipes by checking if blueprint, which contains the enchantment, contains only other
        // enchantments whose cost is known.
        public static bool ReverseEngineerEnchantmentCost(BlueprintItemEquipment blueprint, string enchantmentId) {
            if (blueprint == null || blueprint.IsNotable || blueprint.Ability != null || blueprint.ActivatableAbility != null) {
                return false;
            }

            if (blueprint is BlueprintItemShield || blueprint is BlueprintItemWeapon || blueprint is BlueprintItemArmor) {
                // Cost of enchantments on arms and armor is different, and can be treated as a straight delta.
                return true;
            }

            var mostExpensiveEnchantmentCost = 0;
            var costSum = 0;
            foreach (var enchantment in blueprint.Enchantments) {
                if (enchantment.AssetGuid == enchantmentId) {
                    continue;
                }

                if (!LoadedData.EnchantmentIdToRecipe.ContainsKey(enchantment.AssetGuid) && !LoadedData.EnchantmentIdToCost.ContainsKey(enchantment.AssetGuid)) {
                    return false;
                }

                var enchantmentCost = GetEnchantmentCost(enchantment.AssetGuid, blueprint);
                costSum += enchantmentCost;
                if (mostExpensiveEnchantmentCost < enchantmentCost) {
                    mostExpensiveEnchantmentCost = enchantmentCost;
                }
            }

            var remainder = blueprint.Cost - 3 * costSum / 2;
            if (remainder >= mostExpensiveEnchantmentCost) {
                // enchantmentId is the most expensive enchantment
                LoadedData.EnchantmentIdToCost[enchantmentId] = remainder;
            } else {
                // mostExpensiveEnchantmentCost is the most expensive enchantment
                LoadedData.EnchantmentIdToCost[enchantmentId] = (2 * remainder + mostExpensiveEnchantmentCost) / 3;
            }

            return true;
        }

        public static void AddBattleLogMessage(string message, object tooltip = null, Color? color = null) {
#if PATCH21
            var data = new LogItemData(message, color ?? GameLogStrings.Instance.DefaultColor, tooltip, PrefixIcon.None, new List<LogChannel> { LogChannel.Combat });
#else
            var data = new LogDataManager.LogItemData(message, color ?? GameLogStrings.Instance.DefaultColor, tooltip, PrefixIcon.None);
#endif
            if (Game.Instance.UI.BattleLogManager) {
                Game.Instance.UI.BattleLogManager.LogView.AddLogEntry(data);
            } else {
                PendingLogItems.Add(data);
            }
        }

        private static AbilityData FindCasterSpell(UnitDescriptor caster, BlueprintAbility spellBlueprint, bool mustHavePrepared,
            IReadOnlyCollection<AbilityData> spellsToCast) {
            foreach (var spellbook in caster.Spellbooks) {
                var spellLevel = spellbook.GetSpellLevel(spellBlueprint);
                if (spellLevel > spellbook.MaxSpellLevel || spellLevel < 0) {
                    continue;
                }

                if (mustHavePrepared && spellLevel > 0) {
                    if (spellbook.Blueprint.Spontaneous) {
                        // Count how many other spells of this class and level they're going to cast, to ensure they don't double-dip on spell slots.
                        var toCastCount = spellsToCast.Count(ability => ability.Spellbook == spellbook && spellbook.GetSpellLevel(ability) == spellLevel);
                        // Spontaneous spellcaster must have enough spell slots of the required level.
                        if (spellbook.GetSpontaneousSlots(spellLevel) <= toCastCount) {
                            continue;
                        }
                    } else {
                        // Prepared spellcaster must have memorized the spell...
                        var spellSlot = spellbook.GetMemorizedSpells(spellLevel).FirstOrDefault(slot =>
                            slot.Available && (slot.Spell?.Blueprint == spellBlueprint ||
                                               spellBlueprint.Parent && slot.Spell?.Blueprint == spellBlueprint.Parent));
                        if (spellSlot == null && (spellbook.GetSpontaneousConversionSpells(spellLevel).Contains(spellBlueprint) ||
                                                  (spellBlueprint.Parent &&
                                                   spellbook.GetSpontaneousConversionSpells(spellLevel).Contains(spellBlueprint.Parent)))) {
                            // ... or be able to convert, in which case any available spell of the given level will do.
                            spellSlot = spellbook.GetMemorizedSpells(spellLevel).FirstOrDefault(slot => slot.Available);
                        }

                        if (spellSlot == null) {
                            continue;
                        }

                        return spellSlot.Spell;
                    }
                }

                return spellbook.GetKnownSpells(spellLevel).Concat(spellbook.GetSpecialSpells(spellLevel))
                    .First(known => known.Blueprint == spellBlueprint ||
                                    (spellBlueprint.Parent && known.Blueprint == spellBlueprint.Parent));
            }

            // Try casting the spell from an item
            ItemEntity fromItem = null;
            var fromItemCharges = 0;
            foreach (var item in caster.Inventory) {
                // Check (non-potion) items wielded by the caster to see if they can cast the required spell
                if (item.Wielder == caster && (!(item.Blueprint is BlueprintItemEquipmentUsable usable) || usable.Type != UsableItemType.Potion)
                                           && (item.Ability?.Blueprint == spellBlueprint ||
                                               (spellBlueprint.Parent && item.Ability?.Blueprint == spellBlueprint.Parent))) {
                    // Choose the item with the most available charges, with a multiplier if the item restores charges on rest.
                    var charges = item.Charges * (((BlueprintItemEquipment) item.Blueprint).RestoreChargesOnRest ? 50 : 1);
                    if (charges > fromItemCharges) {
                        fromItem = item;
                        fromItemCharges = charges;
                    }
                }
            }

            return fromItem?.Ability?.Data;
        }

        private static int CheckSpellPrerequisites(CraftingProjectData project, UnitDescriptor caster, bool mustPrepare,
            out List<BlueprintAbility> missingSpells, out List<AbilityData> spellsToCast) {
            return CheckSpellPrerequisites(project.SpellPrerequisites, project.AnyPrerequisite, caster, mustPrepare, out missingSpells, out spellsToCast);
        }

        private static int CheckSpellPrerequisites(BlueprintAbility[] prerequisites, bool anyPrerequisite, UnitDescriptor caster, bool mustPrepare,
            out List<BlueprintAbility> missingSpells, out List<AbilityData> spellsToCast) {
            spellsToCast = new List<AbilityData>();
            missingSpells = new List<BlueprintAbility>();
            if (prerequisites != null) {
                foreach (var spellBlueprint in prerequisites) {
                    var spell = FindCasterSpell(caster, spellBlueprint, mustPrepare, spellsToCast);
                    if (spell != null) {
                        spellsToCast.Add(spell);
                        if (anyPrerequisite) {
                            missingSpells.Clear();
                            return 0;
                        }
                    } else {
                        missingSpells.Add(spellBlueprint);
                    }
                }
            }

            return anyPrerequisite ? Math.Min(1, missingSpells.Count) : missingSpells.Count;
        }

        private static int CheckFeatPrerequisites(CraftingProjectData project, UnitDescriptor caster, out List<BlueprintFeature> missingFeats) {
            return CheckFeatPrerequisites(project.FeatPrerequisites, project.AnyPrerequisite, caster, out missingFeats);
        }

        private static int CheckFeatPrerequisites(BlueprintFeature[] prerequisites, bool anyPrerequisite, UnitDescriptor caster,
            out List<BlueprintFeature> missingFeats) {
            missingFeats = new List<BlueprintFeature>();
            if (prerequisites != null) {
                foreach (var featBlueprint in prerequisites) {
                    var feat = caster.GetFeature(featBlueprint);
                    if (feat != null) {
                        if (anyPrerequisite) {
                            missingFeats.Clear();
                            return 0;
                        }
                    } else {
                        missingFeats.Add(featBlueprint);
                    }
                }
            }

            return anyPrerequisite ? Math.Min(1, missingFeats.Count) : missingFeats.Count;
        }

        private static int CheckCrafterPrerequisites(CraftingProjectData project, UnitDescriptor caster) {
            var missing = GetMissingCrafterPrerequisites(project.CrafterPrerequisites, caster);
            foreach (var prerequisite in missing) {
                AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-crafter-prerequisite",
                    new L10NString($"craftMagicItems-crafter-prerequisite-{prerequisite}"), DifficultyClass.MissingPrerequisiteDCModifier));
            }

            return missing.Count;
        }

        private static List<CrafterPrerequisiteType> GetMissingCrafterPrerequisites(CrafterPrerequisiteType[] prerequisites, UnitDescriptor caster) {
            var missingCrafterPrerequisites = new List<CrafterPrerequisiteType>();
            if (prerequisites != null) {
                missingCrafterPrerequisites.AddRange(prerequisites.Where(prerequisite =>
                    prerequisite == CrafterPrerequisiteType.AlignmentLawful && (caster.Alignment.Value.ToMask() & AlignmentMaskType.Lawful) == 0
                    || prerequisite == CrafterPrerequisiteType.AlignmentGood && (caster.Alignment.Value.ToMask() & AlignmentMaskType.Good) == 0
                    || prerequisite == CrafterPrerequisiteType.AlignmentChaotic && (caster.Alignment.Value.ToMask() & AlignmentMaskType.Chaotic) == 0
                    || prerequisite == CrafterPrerequisiteType.AlignmentEvil && (caster.Alignment.Value.ToMask() & AlignmentMaskType.Evil) == 0
                    || prerequisite == CrafterPrerequisiteType.FeatureChannelEnergy &&
                    caster.GetFeature(ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Features.ChannelEnergyFeatureGuid)) == null
                ));
            }

            return missingCrafterPrerequisites;
        }

        public static void WorkOnProjects(UnitDescriptor caster, bool returningToCapital) {
            if (!caster.IsPlayerFaction || caster.State.IsDead || caster.State.IsFinallyDead) {
                return;
            }

            Selections.CurrentCaster = caster.Unit;
            var withPlayer = Game.Instance.Player.PartyCharacters.Contains(caster.Unit);
            var playerInCapital = IsPlayerInCapital();
            // Only update characters in the capital when the player is also there.
            if (!withPlayer && !playerInCapital) {
                // Character is back in the capital - skipping them for now.
                return;
            }

            var isAdventuring = withPlayer && !IsPlayerSomewhereSafe();
            var timer = GetCraftingTimerComponentForCaster(caster);
            if (timer == null || timer.CraftingProjects.Count == 0) {
                // Character is not doing any crafting
                return;
            }

            // Round up the number of days, so caster makes some progress on a new project the first time they rest.
            var interval = Game.Instance.Player.GameTime.Subtract(timer.LastUpdated);
            var daysAvailableToCraft = (int) Math.Ceiling(interval.TotalDays);
            if (daysAvailableToCraft <= 0) {
                if (isAdventuring) {
                    AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-not-full-day"));
                }

                return;
            }

            // Time passes for this character even if they end up making no progress on their projects.  LastUpdated can go up to
            // a day into the future, due to the rounding up of daysAvailableToCraft.
            timer.LastUpdated += TimeSpan.FromDays(daysAvailableToCraft);
            // Work on projects sequentially, skipping any that can't progress due to missing items, missing prerequisites or having too high a DC.
            foreach (var project in timer.CraftingProjects.ToList()) {
                if (project.UpgradeItem != null) {
                    // Check if the item has been dropped and picked up again, which apparently creates a new object with the same blueprint.
                    if (project.UpgradeItem.Collection != Game.Instance.Player.Inventory && project.UpgradeItem.Collection != Game.Instance.Player.SharedStash) {
                        var itemInStash = Game.Instance.Player.SharedStash.FirstOrDefault(item => item.Blueprint == project.UpgradeItem.Blueprint);
                        if (itemInStash != null) {
                            ItemUpgradeProjects.Remove(project.UpgradeItem);
                            ItemUpgradeProjects[itemInStash] = project;
                            project.UpgradeItem = itemInStash;
                        } else {
                            var itemInInventory = Game.Instance.Player.Inventory.FirstOrDefault(item => item.Blueprint == project.UpgradeItem.Blueprint);
                            if (itemInInventory != null) {
                                ItemUpgradeProjects.Remove(project.UpgradeItem);
                                ItemUpgradeProjects[itemInInventory] = project;
                                project.UpgradeItem = itemInInventory;
                            }
                        }
                    }

                    // Check that the caster can get at the item they're upgrading... it must be in the party inventory, and either un-wielded, or the crafter
                    // must be with the wielder (together in the capital or out in the party together).
                    var wieldedInParty = (project.UpgradeItem.Wielder == null || Game.Instance.Player.PartyCharacters.Contains(project.UpgradeItem.Wielder.Unit));
                    if ((!playerInCapital || returningToCapital)
                        && (project.UpgradeItem.Collection != Game.Instance.Player.SharedStash || withPlayer)
                        && (project.UpgradeItem.Collection != Game.Instance.Player.Inventory || ((!withPlayer || !wieldedInParty) && (withPlayer || wieldedInParty)))) {
                        project.AddMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-upgrade-item", project.UpgradeItem.Blueprint.Name));
                        AddBattleLogMessage(project.LastMessage);
                        continue;
                    }
                }

                var craftingData = LoadedData.ItemCraftingData.FirstOrDefault(data => data.Name == project.ItemType);
                StatType craftingSkill;
                int dc;
                int progressRate;

                if (project.ItemType == BondedItemRitual) {
                    craftingSkill = StatType.SkillKnowledgeArcana;
                    dc = 10 + project.Crafter.Stats.GetStat(craftingSkill).ModifiedValue;
                    progressRate = ModSettings.MagicCraftingRate;
                } else if (IsMundaneCraftingData(craftingData)) {
                    craftingSkill = StatType.SkillKnowledgeWorld;
                    dc = CalculateMundaneCraftingDC((RecipeBasedItemCraftingData) craftingData, project.ResultItem.Blueprint, caster);
                    progressRate = ModSettings.MundaneCraftingRate;
                } else {
                    craftingSkill = StatType.SkillKnowledgeArcana;
                    dc = 5 + project.CasterLevel;
                    progressRate = ModSettings.MagicCraftingRate;
                }

                var missing = CheckSpellPrerequisites(project, caster, isAdventuring, out var missingSpells, out var spellsToCast);
                if (missing > 0) {
                    var missingSpellNames = missingSpells.Select(ability => ability.Name).BuildCommaList(project.AnyPrerequisite);
                    if (craftingData.PrerequisitesMandatory || project.PrerequisitesMandatory) {
                        project.AddMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-prerequisite",
                            project.ResultItem.Name, missingSpellNames));
                        AddBattleLogMessage(project.LastMessage);
                        // If the item type has mandatory prerequisites and some are missing, move on to the next project.
                        continue;
                    }

                    AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-spell", missingSpellNames,
                        DifficultyClass.MissingPrerequisiteDCModifier * missing));
                }
                var missing2 = CheckFeatPrerequisites(project, caster, out var missingFeats);
                if (missing2 > 0) {
                    var missingFeatNames = missingFeats.Select(ability => ability.Name).BuildCommaList(project.AnyPrerequisite);
                    AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-feat", missingFeatNames,
                        DifficultyClass.MissingPrerequisiteDCModifier * missing2));
                }
                missing += missing2;
                missing += CheckCrafterPrerequisites(project, caster);
                dc += DifficultyClass.MissingPrerequisiteDCModifier * missing;
                var casterLevel = CharacterCasterLevel(caster);
                if (casterLevel < project.CasterLevel) {
                    // Rob's ruling... if you're below the prerequisite caster level, you're considered to be missing a prerequisite for each
                    // level you fall short, unless ModSettings.CasterLevelIsSinglePrerequisite is true.
                    var casterLevelPenalty = ModSettings.CasterLevelIsSinglePrerequisite
                        ? DifficultyClass.MissingPrerequisiteDCModifier
                        : DifficultyClass.MissingPrerequisiteDCModifier * (project.CasterLevel - casterLevel);
                    dc += casterLevelPenalty;
                    AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-low-caster-level", project.CasterLevel, casterLevelPenalty));
                }
                var oppositionSchool = CheckForOppositionSchool(caster, project.SpellPrerequisites);
                if (oppositionSchool != SpellSchool.None) {
                    dc += DifficultyClass.OppositionSchoolDCModifier;
                    AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-opposition-school",
                        LocalizedTexts.Instance.SpellSchoolNames.GetText(oppositionSchool), DifficultyClass.OppositionSchoolDCModifier));
                }

                var skillCheck = 10 + caster.Stats.GetStat(craftingSkill).ModifiedValue;
                if (skillCheck < dc) {
                    // Can't succeed by taking 10... move on to the next project.
                    project.AddMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-dc-too-high", project.ResultItem.Name,
                        LocalizedTexts.Instance.Stats.GetText(craftingSkill), skillCheck, dc));
                    AddBattleLogMessage(project.LastMessage);
                    continue;
                }

                // Cleared the last hurdle, so caster is going to make progress on this project.
                // You only work at 1/4 speed if you're crafting while adventuring.
                var adventuringPenalty = !isAdventuring || ModSettings.CraftAtFullSpeedWhileAdventuring ? 1 : DifficultyClass.AdventuringProgressPenalty;
                // Each 1 by which the skill check exceeds the DC increases the crafting rate by 20% of the base progressRate
                var progressPerDay = (int) (progressRate * (1 + (float) (skillCheck - dc) / 5) / adventuringPenalty);
                var daysUntilProjectFinished = (int) Math.Ceiling(1.0 * (project.TargetCost - project.Progress) / progressPerDay);
                var daysCrafting = Math.Min(daysUntilProjectFinished, daysAvailableToCraft);
                var progressGold = daysCrafting * progressPerDay;
                foreach (var spell in spellsToCast) {
                    if (spell.SourceItem != null) {
                        // Use items whether we're adventuring or not, one charge per day of daysCrafting.  We might run out of charges...
                        if (spell.SourceItem.IsSpendCharges && !((BlueprintItemEquipment) spell.SourceItem.Blueprint).RestoreChargesOnRest) {
                            var itemSpell = spell;
                            for (var day = 0; day < daysCrafting; ++day) {
                                if (itemSpell.SourceItem.Charges <= 0) {
                                    // This item is exhausted and we haven't finished crafting - find another item.
                                    itemSpell = FindCasterSpell(caster, spell.Blueprint, isAdventuring, spellsToCast);
                                }

                                if (itemSpell == null) {
                                    // We've run out of items that can cast the spell...crafting progress is going to slow, if not stop.
                                    progressGold -= progressPerDay * (daysCrafting - day);
                                    skillCheck -= DifficultyClass.MissingPrerequisiteDCModifier;
                                    if (craftingData.PrerequisitesMandatory || project.PrerequisitesMandatory) {
                                        AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-prerequisite", project.ResultItem.Name, spell.Name));
                                        daysCrafting = day;
                                        break;
                                    }

                                    AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-spell", spell.Name, DifficultyClass.MissingPrerequisiteDCModifier));
                                    if (skillCheck < dc) {
                                        // Can no longer make progress
                                        AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-dc-too-high", project.ResultItem.Name,
                                            LocalizedTexts.Instance.Stats.GetText(craftingSkill), skillCheck, dc));
                                        daysCrafting = day;
                                    } else {
                                        // Progress will be slower, but they don't need to cast this spell any longer.
                                        progressPerDay = (int) (progressRate * (1 + (float) (skillCheck - dc) / 5) / adventuringPenalty);
                                        daysUntilProjectFinished =
                                            day + (int) Math.Ceiling(1.0 * (project.TargetCost - project.Progress - progressGold) / progressPerDay);
                                        daysCrafting = Math.Min(daysUntilProjectFinished, daysAvailableToCraft);
                                        progressGold += (daysCrafting - day) * progressPerDay;
                                    }

                                    break;
                                }

                                GameLogContext.SourceUnit = caster.Unit;
                                GameLogContext.Text = itemSpell.SourceItem.Name;
                                AddBattleLogMessage(LocalizedStringBlueprints.CharacterUsedItemLocalized);
                                GameLogContext.Clear();
                                itemSpell.SourceItem.SpendCharges(caster);
                            }
                        }
                    } else if (isAdventuring) {
                        // Actually cast the spells if we're adventuring.
                        AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-expend-spell", spell.Name));
                        spell.SpendFromSpellbook();
                    }
                }

                var progressKey = project.ItemType == BondedItemRitual
                    ? "craftMagicItems-logMessage-made-progress-bondedItem"
                    : "craftMagicItems-logMessage-made-progress";
                var progress = LocalizationHelper.FormatLocalizedString(progressKey, progressGold, project.TargetCost - project.Progress, project.ResultItem.Name);
                var checkResult = LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-made-progress-check", LocalizedTexts.Instance.Stats.GetText(craftingSkill),
                    skillCheck, dc);
                AddBattleLogMessage(progress, checkResult);
                daysAvailableToCraft -= daysCrafting;
                project.Progress += progressGold;
                if (project.Progress >= project.TargetCost) {
                    // Completed the project!
                    if (project.ItemType == BondedItemRitual) {
                        AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-bonding-ritual-complete", project.ResultItem.Name), project.ResultItem);
                        BondWithObject(project.Crafter, project.ResultItem);
                    } else {
                        AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-crafting-complete", project.ResultItem.Name), project.ResultItem);
                        CraftItem(project.ResultItem, project.UpgradeItem);
                    }
                    timer.CraftingProjects.Remove(project);
                    if (project.UpgradeItem == null) {
                        ItemCreationProjects.Remove(project);
                    } else {
                        ItemUpgradeProjects.Remove(project.UpgradeItem);
                    }
                } else {
                    var completeKey = project.ItemType == BondedItemRitual
                        ? "craftMagicItems-logMessage-made-progress-bonding-ritual-amount-complete"
                        : "craftMagicItems-logMessage-made-progress-amount-complete";
                    var amountComplete = LocalizationHelper.FormatLocalizedString(completeKey, project.ResultItem.Name, 100 * project.Progress / project.TargetCost);
                    AddBattleLogMessage(amountComplete, project.ResultItem);
                    project.AddMessage($"{progress} {checkResult}");
                }

                if (daysAvailableToCraft <= 0) {
                    return;
                }
            }

            if (daysAvailableToCraft > 0) {
                // They didn't use up all available days - reset the time they can start crafting to now.
                timer.LastUpdated = Game.Instance.Player.GameTime;
            }
        }

        public static bool EquipmentEnchantmentValid(ItemEntityWeapon weapon, ItemEntity owner) {
            if ((weapon == owner) ||
                (weapon != null && (weapon.Blueprint.IsNatural || weapon.Blueprint.IsUnarmed))) {
                return true;
            } else {
                return false;
            }
        }
    }
}