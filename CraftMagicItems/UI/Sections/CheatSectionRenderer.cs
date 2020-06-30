using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftMagicItems.UI.Sections
{
    public class CheatSectionRenderer
    {
        /// <summary>Renders a checkbox indicating whether crafting costs any gold and returns its current UI value</summary>
        /// <param name="currentSetting">Current value</param>
        /// <returns>The value of the UI checkbox</returns>
        public bool Evaluate_CraftingCostsNoGold(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Crafting costs no gold and no material components.", currentSetting);
        }

        public int Evaluate_CraftingCostSelection(string priceLabel, string[] craftingPriceStrings, Action<string, int> SetSelectionIndex)
        {
            throw new NotImplementedException("TODO");
        }

        public int Evaluate_CustomCraftingCostSlider(int currentSetting)
        {
            throw new NotImplementedException("TODO");
        }

        public void RenderOnly_WarningAboutCustomItemVanillaItemCostDisparity()
        {
            throw new NotImplementedException("TODO");
        }

        public bool Evaluate_IgnoreCraftingFeats(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Crafting does not require characters to take crafting feats.", currentSetting);
        }

        public bool Evaluate_CraftingTakesNoTime(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Crafting takes no time to complete.", currentSetting);
        }

        public bool Evaluate_CustomCraftRate(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Craft at a non-standard rate.", currentSetting);
        }

        public int Evaluate_MagicCraftingRateSlider(int currentSetting)
        {
            throw new NotImplementedException("TODO");
        }

        public int Evaluate_MundaneCraftingRateSlider(int currentSetting)
        {
            throw new NotImplementedException("TODO");
        }

        public bool Evaluate_CasterLevelIsSinglePrerequisite(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("When crafting, a Caster Level less than the prerequisite counts as a single missing prerequisite.", currentSetting);
        }

        public bool Evaluate_CraftAtFullSpeedWhileAdventuring(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Characters craft at full speed while adventuring (instead of 25% speed).", currentSetting);
        }

        public bool Evaluate_IgnorePlusTenItemMaximum(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Ignore the rule that limits arms and armor to a maximum of +10 equivalent.", currentSetting);
        }

        public bool Evaluate_IgnoreFeatCasterLevelRestriction(bool currentSetting)
        {
            return UmmUiRenderer.RenderCheckbox("Ignore the crafting feat Caster Level prerequisites when learning feats.", currentSetting);
        }
    }
}