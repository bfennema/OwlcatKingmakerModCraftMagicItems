namespace CraftMagicItems.Constants
{
    /// <summary>Constants class containing modifiers and penalties to crafting DC</summary>
    public static class DifficultyClass
    {
        /// <summary>Additional DC modifier for missing prerequisites to crafting</summary>
        public const int MissingPrerequisiteDCModifier = 5;

        /// <summary>Additional DC modifier for a required spell from a Wizard's opposition school of magic</summary>
        public const int OppositionSchoolDCModifier = 4;

        /// <summary>Penalty to progress that can be made while adventuring</summary>
        public const int AdventuringProgressPenalty = 4;
    }
}