using System;
using CraftMagicItems.UI.Sections;

namespace CraftMagicItems.UI
{
    /// <summary>Class that performs User Interface rendering and takes the results freom such and performs logical operations based on the UI state</summary>
    public static class UserInterfaceEventHandlingLogic
    {
        /// <summary>Renders the Cheats section and retrieves the values specified by its rendered UI</summary>
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
    }
}