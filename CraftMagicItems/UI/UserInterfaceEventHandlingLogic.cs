using System;
using System.Globalization;
using UnityEngine;

namespace CraftMagicItems.UI
{
    /// <summary>Class that performs User Interface rendering and takes the results freom such and performs logical operations based on the UI state</summary>
    public static class UserInterfaceEventHandlingLogic
    {
        /// <summary>Renders the Cheats section and retrieves the values specified by its rendered UI</summary>
        /// <param name="modSettings"><see cref="Settings" /> to default to and to read from</param>
        /// <param name="priceLabel">Text to render for the price</param>
        /// <param name="craftingPriceStrings">Collection of <see cref="String" /> containing the display text for various pricing guidelines</param>
        /// <param name="GetSelectionIndex"><see cref="Func{String, Int32}" /> that retrieves the index for <paramref name="priceLabel" /></param>
        /// <param name="SetSelectionIndex"><see cref="Action{String, Int32}" /> callback that updates the currently selected index from the UI</param>
        public static void RenderCheatsSectionAndUpdateSettings(Settings modSettings, string priceLabel, string[] craftingPriceStrings,
            Func<string, int> GetSelectionIndex, Action<string, int> SetSelectionIndex)
        {
            modSettings.CraftingCostsNoGold = UmmUiRenderer.RenderCheckbox("Crafting costs no gold and no material components.", modSettings.CraftingCostsNoGold);
            if (!modSettings.CraftingCostsNoGold)
            {
                var selectedCustomPriceScaleIndex = UmmUiRenderer.RenderSelection(priceLabel, craftingPriceStrings, 4, GetSelectionIndex, SetSelectionIndex);
                if (selectedCustomPriceScaleIndex == 2)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Custom Cost Factor: ", GUILayout.ExpandWidth(false));
                    modSettings.CraftingPriceScale = GUILayout.HorizontalSlider(modSettings.CraftingPriceScale * 100, 0, 500, GUILayout.Width(300)) / 100;
                    GUILayout.Label(Mathf.Round(modSettings.CraftingPriceScale * 100).ToString(CultureInfo.InvariantCulture));
                    GUILayout.EndHorizontal();
                }
                else
                {
                    modSettings.CraftingPriceScale = 1 + selectedCustomPriceScaleIndex;
                }

                if (selectedCustomPriceScaleIndex != 0)
                {
                    UmmUiRenderer.RenderLabel(
                        "<b>Note:</b> The sale price of custom crafted items will also be scaled by this factor, but vanilla items crafted by this mod" +
                        " will continue to use Owlcat's sale price, creating a price difference between the cost of crafting and sale price.");
                }
            }

            modSettings.IgnoreCraftingFeats = UmmUiRenderer.RenderCheckbox("Crafting does not require characters to take crafting feats.", modSettings.IgnoreCraftingFeats);
            modSettings.CraftingTakesNoTime = UmmUiRenderer.RenderCheckbox("Crafting takes no time to complete.", modSettings.CraftingTakesNoTime);
            if (!modSettings.CraftingTakesNoTime)
            {
                modSettings.CustomCraftRate = UmmUiRenderer.RenderCheckbox("Craft at a non-standard rate.", modSettings.CustomCraftRate);
                if (modSettings.CustomCraftRate)
                {
                    var maxMagicRate = ((modSettings.MagicCraftingRate + 1000) / 1000) * 1000;
                    modSettings.MagicCraftingRate = UmmUiRenderer.RenderIntSlider("Magic Item Crafting Rate", modSettings.MagicCraftingRate, 1, maxMagicRate);
                    var maxMundaneRate = ((modSettings.MundaneCraftingRate + 10) / 10) * 10;
                    modSettings.MundaneCraftingRate = UmmUiRenderer.RenderIntSlider("Mundane Item Crafting Rate", modSettings.MundaneCraftingRate, 1, maxMundaneRate);
                }
                else
                {
                    modSettings.MagicCraftingRate = Settings.MagicCraftingProgressPerDay;
                    modSettings.MundaneCraftingRate = Settings.MundaneCraftingProgressPerDay;
                }
            }

            modSettings.CasterLevelIsSinglePrerequisite = UmmUiRenderer.RenderCheckbox("When crafting, a Caster Level less than the prerequisite counts as a single missing prerequisite.",
                modSettings.CasterLevelIsSinglePrerequisite);
            modSettings.CraftAtFullSpeedWhileAdventuring = UmmUiRenderer.RenderCheckbox("Characters craft at full speed while adventuring (instead of 25% speed).", modSettings.CraftAtFullSpeedWhileAdventuring);
            modSettings.IgnorePlusTenItemMaximum = UmmUiRenderer.RenderCheckbox("Ignore the rule that limits arms and armor to a maximum of +10 equivalent.", modSettings.IgnorePlusTenItemMaximum);
            modSettings.IgnoreFeatCasterLevelRestriction = UmmUiRenderer.RenderCheckbox("Ignore the crafting feat Caster Level prerequisites when learning feats.", modSettings.IgnoreFeatCasterLevelRestriction);
        }
    }
}