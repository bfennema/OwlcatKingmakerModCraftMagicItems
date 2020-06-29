using UnityModManagerNet;

namespace CraftMagicItems
{
    /// <summary>Settings for the Crafting Mod</summary>
    public class Settings : UnityModManager.ModSettings
    {
        public const int MagicCraftingProgressPerDay = 500;
        public const int MundaneCraftingProgressPerDay = 5;

        public bool CraftingCostsNoGold;
        public bool IgnoreCraftingFeats;
        public bool CraftingTakesNoTime;
        public float CraftingPriceScale = 1;
        public bool CraftAtFullSpeedWhileAdventuring;
        public bool CasterLevelIsSinglePrerequisite;
        public bool IgnoreFeatCasterLevelRestriction;
        public bool IgnorePlusTenItemMaximum;
        public bool CustomCraftRate;
        public int MagicCraftingRate = MagicCraftingProgressPerDay;
        public int MundaneCraftingRate = MundaneCraftingProgressPerDay;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}