using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using UnityEngine;

namespace CraftMagicItems.Patches.Harmony
{
    public static class PlayerPostLoadPatch
    {
        private static void Postfix()
        {
            Main.ItemUpgradeProjects.Clear();
            Main.ItemCreationProjects.Clear();

            var characterList = UIUtility.GetGroup(true);
            foreach (var character in characterList)
            {
                // If the mod is disabled, this will clean up crafting timer "buff" from all casters.
                var timer = Main.GetCraftingTimerComponentForCaster(character.Descriptor, character.IsMainCharacter);
                var bondedItemComponent = Main.GetBondedItemComponentForCaster(character.Descriptor);

                if (!Main.modEnabled)
                {
                    continue;
                }

                if (timer != null)
                {
                    foreach (var project in timer.CraftingProjects)
                    {
                        if (project.ItemBlueprint != null)
                        {
                            // Migrate all projects using ItemBlueprint to use ResultItem
                            var craftingData = Main.LoadedData.ItemCraftingData.First(data => data.Name == project.ItemType);
                            project.ResultItem = Main.BuildItemEntity(project.ItemBlueprint, craftingData, character);
                            project.ItemBlueprint = null;
                        }

                        project.Crafter = character;
                        if (!project.ResultItem.HasUniqueVendor)
                        {
                            // Set "vendor" of item if it's already in progress
                            project.ResultItem.SetVendorIfNull(character);
                        }
                        project.ResultItem.PostLoad();

                        if (project.UpgradeItem == null)
                        {
                            Main.ItemCreationProjects.Add(project);
                        }
                        else
                        {
                            Main.ItemUpgradeProjects[project.UpgradeItem] = project;
                            project.UpgradeItem.PostLoad();
                        }
                    }

                    if (character.IsMainCharacter)
                    {
                        UpgradeSave(string.IsNullOrEmpty(timer.Version) ? null : Version.Parse(timer.Version));
                        timer.Version = Main.ModEntry.Version.ToString();
                    }
                }

                if (bondedItemComponent != null)
                {
                    bondedItemComponent.ownerItem?.PostLoad();
                    bondedItemComponent.everyoneElseItem?.PostLoad();
                }

                // Retroactively give character any crafting feats in their past progression data which they don't actually have
                // (e.g. Alchemists getting Brew Potion)
                foreach (var characterClass in character.Descriptor.Progression.Classes)
                {
                    foreach (var levelData in characterClass.CharacterClass.Progression.LevelEntries)
                    {
                        if (levelData.Level <= characterClass.Level)
                        {
                            foreach (var feature in levelData.Features.OfType<BlueprintFeature>())
                            {
                                if (feature.AssetGuid.Contains("#CraftMagicItems(feat=") && !Main.CharacterHasFeat(character, feature.AssetGuid))
                                {
                                    character.Descriptor.Progression.Features.AddFeature(feature);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AddToLootTables(BlueprintItem blueprint, string[] tableNames, bool firstTime)
        {
            var tableCount = tableNames.Length;
            foreach (var loot in ResourcesLibrary.GetBlueprints<BlueprintLoot>())
            {
                if (tableNames.Contains(loot.name))
                {
                    tableCount--;
                    if (!loot.Items.Any(entry => entry.Item == blueprint))
                    {
                        var lootItems = loot.Items.ToList();
                        lootItems.Add(new LootEntry { Count = 1, Item = blueprint });
                        loot.Items = lootItems.ToArray();
                    }
                }
            }
            foreach (var unitLoot in ResourcesLibrary.GetBlueprints<BlueprintUnitLoot>())
            {
                if (tableNames.Contains(unitLoot.name))
                {
                    tableCount--;
                    if (unitLoot is BlueprintSharedVendorTable vendor)
                    {
                        if (firstTime)
                        {
                            var vendorTable = Game.Instance.Player.SharedVendorTables.GetTable(vendor);
                            vendorTable.Add(blueprint.CreateEntity());
                        }
                    }
                    else if (!unitLoot.ComponentsArray.Any(component => component is LootItemsPackFixed pack && pack.Item.Item == blueprint))
                    {
                        var lootItem = new LootItem();
                        Main.Accessors.SetLootItemItem(lootItem) = blueprint;
#if PATCH21_BETA
                        var lootComponent = SerializedScriptableObject.CreateInstance<LootItemsPackFixed>();
#else
                        var lootComponent = ScriptableObject.CreateInstance<LootItemsPackFixed>();
#endif
                        Main.Accessors.SetLootItemsPackFixedItem(lootComponent) = lootItem;
                        Main.blueprintPatcher.EnsureComponentNameUnique(lootComponent, unitLoot.ComponentsArray);
                        var components = unitLoot.ComponentsArray.ToList();
                        components.Add(lootComponent);
                        unitLoot.ComponentsArray = components.ToArray();
                    }
                }
            }
            if (tableCount > 0)
            {
                HarmonyLib.FileLog.Log($"!!! Failed to match all loot table names for {blueprint.Name}.  {tableCount} table names not found.");
            }
        }

        public static void UpgradeSave(Version version)
        {
            foreach (var lootItem in Main.LoadedData.CustomLootItems)
            {
                var firstTime = (version == null || version.CompareTo(lootItem.AddInVersion) < 0);
                var item = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(lootItem.AssetGuid);
                if (item == null)
                {
                    HarmonyLib.FileLog.Log($"!!! Loot item not created: {lootItem.AssetGuid}");
                }
                else
                {
                    AddToLootTables(item, lootItem.LootTables, firstTime);
                }
            }
        }
    }
}