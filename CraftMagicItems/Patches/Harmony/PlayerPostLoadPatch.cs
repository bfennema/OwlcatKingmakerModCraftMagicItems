using System;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UI.Common;

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
                        Main.UpgradeSave(string.IsNullOrEmpty(timer.Version) ? null : Version.Parse(timer.Version));
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
    }
}