using Kingmaker.UI.Tooltip;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(DescriptionTemplatesItem), "ItemEnergy")]
    public static class DescriptionTemplatesItemItemEnergyPatch
    {
        private static void Postfix(TooltipData data, bool __result)
        {
            if (__result)
            {
                if (data.Energy.Count > 0)
                {
                    data.Energy.Clear();
                }
            }
        }
    }
}