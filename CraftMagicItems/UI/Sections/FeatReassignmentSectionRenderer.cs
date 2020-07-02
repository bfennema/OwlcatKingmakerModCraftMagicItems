using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CraftMagicItems.UI.UnityModManager;

namespace CraftMagicItems.UI.Sections
{
    public class FeatReassignmentSectionRenderer
    {
        /// <summary>Renders a warning that the current character does not qualify for any crafting feats</summary>
        /// <param name="characterName">Name of character currently being evaluated</param>
        public void RenderOnly_Warning_NoCraftingFeatQualifications(string characterName)
        {
            UmmUiRenderer.RenderLabelRow($"{characterName} does not currently qualify for any crafting feats.");
        }

        /// <summary>Renders a message describing how to use the section</summary>
        public void RenderOnly_UsageExplanation()
        {
            UmmUiRenderer.RenderLabelRow("Use this section to reassign previous feat choices for this character to crafting feats.  <color=red>Warning:</color> This is a one-way assignment!");
        }

        /// <summary>Renders a selection to Unity Mod Manager for the options for selecting a missing casting feat</summary>
        /// <param name="featOptions">Collection of feat names that are missing from the currently selected character</param>
        /// <returns>The selected index of the feats</returns>
        public int Evaluate_MissingFeatSelection(string[] featOptions)
        {
            return Main.DrawSelectionUserInterfaceElements("Feat to learn", featOptions, 6);
        }

        /// <summary>Renders a label and button on their own line for selecting a current feat to be replaced by the selected replacement crafting feat</summary>
        /// <param name="existingFeat">Name of the existing feat to potentially replace</param>
        /// <param name="replacementFeat">Name of the feat to potentially replace <paramref name="existingFeat" /></param>
        /// <returns>True if the button is clicked, otherwise false</returns>
        public bool Evaluate_LearnFeatButton(string existingFeat, string replacementFeat)
        {
            UmmUiRenderer.RenderHorizontalStart();
            UmmUiRenderer.RenderLabel($"Feat: {existingFeat}", false);
            var selection = UmmUiRenderer.RenderButton($"<- {replacementFeat}", false);
            UmmUiRenderer.RenderHorizontalEnd();

            return selection;
        }
    }
}