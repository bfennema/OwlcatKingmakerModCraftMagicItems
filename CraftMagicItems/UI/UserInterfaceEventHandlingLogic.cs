using System;
using System.Linq;
using CraftMagicItems.UI.Sections;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UI.ActionBar;
using Kingmaker.UnitLogic.FactLogic;

namespace CraftMagicItems.UI
{
    /// <summary>Class that performs User Interface rendering and takes the results freom such and performs logical operations based on the UI state</summary>
    public static class UserInterfaceEventHandlingLogic
    {
        /// <summary>Renders the Cheats section and retrieves the values specified by its rendered UI</summary>
        /// <param name="renderer"><see cref="ICheatSectionRenderer" /> instance used to render controls and return current values</param>
        /// <param name="modSettings"><see cref="Settings" /> to default to and to read from</param>
        /// <param name="priceLabel">Text to render for the price</param>
        /// <param name="craftingPriceStrings">Collection of <see cref="String" /> containing the display text for various pricing guidelines</param>
        public static void RenderCheatsSectionAndUpdateSettings(ICheatSectionRenderer renderer, Settings modSettings, string priceLabel, string[] craftingPriceStrings)
        {
            modSettings.CraftingCostsNoGold = renderer.Evaluate_CraftingCostsNoGold(modSettings.CraftingCostsNoGold);
            if (!modSettings.CraftingCostsNoGold)
            {
                var selectedCustomPriceScaleIndex = renderer.Evaluate_CraftingCostSelection(priceLabel, craftingPriceStrings);
                if (selectedCustomPriceScaleIndex == 2) //if user selected "Custom"
                {
                    modSettings.CraftingPriceScale = renderer.Evaluate_CustomCraftingCostSlider(modSettings.CraftingPriceScale);
                }
                else
                {
                    //index 0 = 100%; index 1 = 200%
                    modSettings.CraftingPriceScale = 1 + selectedCustomPriceScaleIndex;
                }

                if (selectedCustomPriceScaleIndex != 0)
                {
                    renderer.RenderOnly_WarningAboutCustomItemVanillaItemCostDisparity();
                }
            }

            modSettings.IgnoreCraftingFeats = renderer.Evaluate_IgnoreCraftingFeats(modSettings.IgnoreCraftingFeats);
            modSettings.CraftingTakesNoTime = renderer.Evaluate_CraftingTakesNoTime(modSettings.CraftingTakesNoTime);
            if (!modSettings.CraftingTakesNoTime)
            {
                modSettings.CustomCraftRate = renderer.Evaluate_CustomCraftRate(modSettings.CustomCraftRate);
                if (modSettings.CustomCraftRate)
                {
                    modSettings.MagicCraftingRate = renderer.Evaluate_MagicCraftingRateSlider(modSettings.MagicCraftingRate);
                    modSettings.MundaneCraftingRate = renderer.Evaluate_MundaneCraftingRateSlider(modSettings.MundaneCraftingRate);
                }
                else
                {
                    modSettings.MagicCraftingRate = Settings.MagicCraftingProgressPerDay;
                    modSettings.MundaneCraftingRate = Settings.MundaneCraftingProgressPerDay;
                }
            }

            modSettings.CasterLevelIsSinglePrerequisite = renderer.Evaluate_CasterLevelIsSinglePrerequisite(modSettings.CasterLevelIsSinglePrerequisite);
            modSettings.CraftAtFullSpeedWhileAdventuring = renderer.Evaluate_CraftAtFullSpeedWhileAdventuring(modSettings.CraftAtFullSpeedWhileAdventuring);
            modSettings.IgnorePlusTenItemMaximum = renderer.Evaluate_IgnorePlusTenItemMaximum(modSettings.IgnorePlusTenItemMaximum);
            modSettings.IgnoreFeatCasterLevelRestriction = renderer.Evaluate_IgnoreFeatCasterLevelRestriction(modSettings.IgnoreFeatCasterLevelRestriction);
        }

        /// <summary>Renders the section for feat reassignment and handles user selections</summary>
        /// <param name="renderer"><see cref="FeatReassignmentSectionRenderer" /> instance that handles rendering of controls</param>
        public static void RenderFeatReassignmentSection(FeatReassignmentSectionRenderer renderer)
        {
            var caster = Main.GetSelectedCrafter(false);
            if (caster == null)
            {
                return;
            }

            var casterLevel = Main.CharacterCasterLevel(caster.Descriptor);
            var missingFeats = Main.ItemCraftingData
                .Where(data => data.FeatGuid != null && !Main.CharacterHasFeat(caster, data.FeatGuid) && data.MinimumCasterLevel <= casterLevel)
                .ToArray();
            if (missingFeats.Length == 0)
            {
                renderer.RenderOnly_Warning_NoCraftingFeatQualifications(caster.CharacterName);
                return;
            }

            renderer.RenderOnly_UsageExplanation();
            var featOptions = missingFeats.Select(data => new L10NString(data.NameId).ToString()).ToArray();
            var selectedFeatToLearn = renderer.Evaluate_MissingFeatSelection(featOptions);
            var learnFeatData = missingFeats[selectedFeatToLearn];
            var learnFeat = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(learnFeatData.FeatGuid);
            if (learnFeat == null)
            {
                throw new Exception($"Unable to find feat with guid {learnFeatData.FeatGuid}");
            }

            var removedFeatIndex = 0;
            foreach (var feature in caster.Descriptor.Progression.Features)
            {
                if (!feature.Blueprint.HideInUI && feature.Blueprint.HasGroup(Main.CraftingFeatGroups)
                                                && (feature.SourceProgression != null || feature.SourceRace != null))
                {
                    if (renderer.Evaluate_LearnFeatButton(feature.Name, learnFeat.Name))
                    {
                        var currentRank = feature.Rank;
                        caster.Descriptor.Progression.ReplaceFeature(feature.Blueprint, learnFeat);
                        if (currentRank == 1)
                        {
                            foreach (var addFact in feature.SelectComponents((AddFacts addFacts) => true))
                            {
                                addFact.OnFactDeactivate();
                            }

                            caster.Descriptor.Progression.Features.RemoveFact(feature);
                        }

                        var addedFeature = caster.Descriptor.Progression.Features.AddFeature(learnFeat);
                        addedFeature.Source = feature.Source;

                        var mFacts = Main.Accessors.GetFeatureCollectionFacts(caster.Descriptor.Progression.Features);
                        if (removedFeatIndex < mFacts.Count)
                        {
                            // Move the new feat to the place in the list originally occupied by the removed one.
                            mFacts.Remove(addedFeature);
                            mFacts.Insert(removedFeatIndex, addedFeature);
                        }

                        ActionBarManager.Instance.HandleAbilityRemoved(null);
                    }
                }

                removedFeatIndex++;
            }
        }
    }
}