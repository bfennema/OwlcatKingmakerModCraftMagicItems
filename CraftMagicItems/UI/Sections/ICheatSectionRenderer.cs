using System;

namespace CraftMagicItems.UI.Sections
{
    /// <summary>Interface defining the I/O around the user interface for the cheats section and its returned user input values</summary>
    public interface ICheatSectionRenderer
    {
        /// <summary>Renders a checkbox indicating whether missing caster levels should combine into a single prerequisite (compared to the default of 1 prerequisite per missing level)</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        bool Evaluate_CasterLevelIsSinglePrerequisite(bool currentSetting);

        /// <summary>Renders a checkbox indicating whether characters should craft at full rate while travelling (compared to 25% rate while travelling)</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        bool Evaluate_CraftAtFullSpeedWhileAdventuring(bool currentSetting);

        /// <summary>Renders a selection for the options for custom crafting price adjustments and returns the currently selected value</summary>
        /// <param name="priceLabel">Label to display for the selection UI control</param>
        /// <param name="craftingPriceStrings">Collection of options that the user can select from</param>
        /// <returns>The index of the current selection in the UI control</returns>
        int Evaluate_CraftingCostSelection(string priceLabel, string[] craftingPriceStrings);

        /// <summary>Renders a checkbox indicating whether crafting costs any gold and returns its current UI value</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        bool Evaluate_CraftingCostsNoGold(bool currentSetting);

        /// <summary>Renders a checkbox indicating whether crafting should take time to complete or not</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        bool Evaluate_CraftingTakesNoTime(bool currentSetting);

        /// <summary>Renders a slider to Unity Mod Manager for the custom crafting % cost.</summary>
        /// <param name="currentSetting">Current setting to display on the control</param>
        /// <returns>The selection that the UI currently has registered</returns>
        float Evaluate_CustomCraftingCostSlider(float currentSetting);

        /// <summary>Renders a checkbox indicating whether crafting should take a non-standard rate or time</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        bool Evaluate_CustomCraftRate(bool currentSetting);

        /// <summary>Renders a checkbox indicating whether crafting should ignore feats</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        bool Evaluate_IgnoreCraftingFeats(bool currentSetting);

        /// <summary>Renders a checkbox indicating whether crafting feats can ignore caster level prerequisites</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        bool Evaluate_IgnoreFeatCasterLevelRestriction(bool currentSetting);

        /// <summary>Renders a checkbox indicating whether weapons and armor should be allowed to exceed the +10 enchantment value</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        bool Evaluate_IgnorePlusTenItemMaximum(bool currentSetting);

        /// <summary>Renders a slider to Unity Mod Manager for the custom magic crafting rate.</summary>
        /// <param name="currentSetting">Current setting to display on the control</param>
        /// <returns>The selection that the UI currently has registered</returns>
        int Evaluate_MagicCraftingRateSlider(int currentSetting);

        /// <summary>Renders a slider to Unity Mod Manager for the custom mundane crafting rate.</summary>
        /// <param name="currentSetting">Current setting to display on the control</param>
        /// <returns>The selection that the UI currently has registered</returns>
        int Evaluate_MundaneCraftingRateSlider(int currentSetting);

        /// <summary>Renders a warning that price disparity between custom items crafts to non-custom items will have a selling cost disparity between crafting and sale.</summary>
        void RenderOnly_WarningAboutCustomItemVanillaItemCostDisparity();
    }
}