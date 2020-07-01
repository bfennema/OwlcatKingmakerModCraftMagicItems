namespace CraftMagicItems.UI.Sections
{
    /// <summary>User Interface renderer into Unity Mod Manager for the cheats section</summary>
    public class CheatSectionRenderer : ICheatSectionRenderer
    {
        /// <summary>Renders a checkbox indicating whether crafting costs any gold and returns its current UI value</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_CraftingCostsNoGold(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Crafting costs no gold and no material components.", currentSetting);
        }

        /// <summary>Renders a selection to Unity Mod Manager for the options for custom crafting price adjustments and returns the currently selected value</summary>
        /// <param name="priceLabel">Label to display for the selection UI control</param>
        /// <param name="craftingPriceStrings">Collection of options that the user can select from</param>
        /// <returns>The index of the selection that Unity currently has registered</returns>
        public int Evaluate_CraftingCostSelection(string priceLabel, string[] craftingPriceStrings)
        {
            return Main.DrawSelectionUserInterfaceElements(priceLabel, craftingPriceStrings, 4);
        }

        /// <summary>Renders a slider to Unity Mod Manager for the custom crafting % cost.</summary>
        /// <param name="currentSetting">Current setting to display on the control</param>
        /// <returns>The selection that the UI currently has registered</returns>
        public float Evaluate_CustomCraftingCostSlider(float currentSetting)
        {
            float result = UmmUiRenderer.RenderFloatSlider("Custom Cost Factor: ", currentSetting * 100, 0, 500);
            return result / 100;
        }

        /// <summary>Renders a warning that price disparity between custom items crafts to non-custom items will have a selling cost disparity between crafting and sale.</summary>
        public void RenderOnly_WarningAboutCustomItemVanillaItemCostDisparity()
        {
            UmmUiRenderer.RenderLabelRow(
                "<b>Note:</b> The sale price of custom crafted items will also be scaled by this factor, but vanilla items crafted by this mod" +
                " will continue to use Owlcat's sale price, creating a price difference between the cost of crafting and sale price.");
        }

        /// <summary>Renders a checkbox indicating whether crafting should ignore feats</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_IgnoreCraftingFeats(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Crafting does not require characters to take crafting feats.", currentSetting);
        }

        /// <summary>Renders a checkbox indicating whether crafting should take time to complete or not</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_CraftingTakesNoTime(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Crafting takes no time to complete.", currentSetting);
        }

        /// <summary>Renders a checkbox indicating whether crafting should take a non-standard rate or time</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_CustomCraftRate(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Craft at a non-standard rate.", currentSetting);
        }

        /// <summary>Renders a slider to Unity Mod Manager for the custom magic crafting rate.</summary>
        /// <param name="currentSetting">Current setting to display on the control</param>
        /// <returns>The selection that the UI currently has registered</returns>
        public int Evaluate_MagicCraftingRateSlider(int currentSetting)
        {
            var maxMagicRate = ((currentSetting + 1000) / 1000) * 1000;
            return UmmUiRenderer.RenderIntSlider("Magic Item Crafting Rate", currentSetting, 1, maxMagicRate);
        }

        /// <summary>Renders a slider to Unity Mod Manager for the custom mundane crafting rate.</summary>
        /// <param name="currentSetting">Current setting to display on the control</param>
        /// <returns>The selection that the UI currently has registered</returns>
        public int Evaluate_MundaneCraftingRateSlider(int currentSetting)
        {
            var maxMundaneRate = ((currentSetting + 10) / 10) * 10;
            return UmmUiRenderer.RenderIntSlider("Mundane Item Crafting Rate", currentSetting, 1, maxMundaneRate);
        }

        /// <summary>Renders a checkbox indicating whether missing caster levels should combine into a single prerequisite (compared to the default of 1 prerequisite per missing level)</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_CasterLevelIsSinglePrerequisite(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("When crafting, a Caster Level less than the prerequisite counts as a single missing prerequisite.", currentSetting);
        }

        /// <summary>Renders a checkbox indicating whether characters should craft at full rate while travelling (compared to 25% rate while travelling)</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_CraftAtFullSpeedWhileAdventuring(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Characters craft at full speed while adventuring (instead of 25% speed).", currentSetting);
        }

        /// <summary>Renders a checkbox indicating whether weapons and armor should be allowed to exceed the +10 enchantment value</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_IgnorePlusTenItemMaximum(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Ignore the rule that limits arms and armor to a maximum of +10 equivalent.", currentSetting);
        }

        /// <summary>Renders a checkbox indicating whether crafting feats can ignore caster level prerequisites</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_IgnoreFeatCasterLevelRestriction(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Ignore the crafting feat Caster Level prerequisites when learning feats.", currentSetting);
        }
    }
}