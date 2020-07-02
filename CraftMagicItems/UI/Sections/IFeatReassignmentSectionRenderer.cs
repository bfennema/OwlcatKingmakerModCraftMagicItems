namespace CraftMagicItems.UI.Sections
{
    /// <summary>Interface defining the I/O around the user interface for the feat reassignment section and its returned user input</summary>
    public interface IFeatReassignmentSectionRenderer
    {
        /// <summary>Renders a label and button for selecting a current feat to be replaced by the selected replacement crafting feat</summary>
        /// <param name="existingFeat">Name of the existing feat to potentially be replaced</param>
        /// <param name="replacementFeat">Name of the feat to potentially replace <paramref name="existingFeat" /></param>
        /// <returns>True if the button is clicked, otherwise false</returns>
        bool Evaluate_LearnFeatButton(string existingFeat, string replacementFeat);

        /// <summary>Renders a selection for the options for selecting a missing casting feat</summary>
        /// <param name="featOptions">Collection of feat names that are missing from the currently selected character</param>
        /// <returns>The selected index of the feats</returns>
        int Evaluate_MissingFeatSelection(string[] featOptions);

        /// <summary>Renders a message describing how to use the section</summary>
        void RenderOnly_UsageExplanation();

        /// <summary>Renders a warning that the current character does not qualify for any crafting feats</summary>
        /// <param name="characterName">Name of character currently being evaluated</param>
        void RenderOnly_Warning_NoCraftingFeatQualifications(string characterName);
    }
}