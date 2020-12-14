using System;
using System.Linq;
using Kingmaker.UI.Tooltip;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(DescriptionTemplatesItem), "ItemEnhancement")]
    public static class DescriptionTemplatesItemItemEnhancementPatch
    {
        private static void Postfix(TooltipData data)
        {
            if (data.Texts.ContainsKey(Enum.GetValues(typeof(TooltipElement)).Cast<TooltipElement>().Max() + 1))
            {
                data.Texts[TooltipElement.Enhancement] = data.Texts[Enum.GetValues(typeof(TooltipElement)).Cast<TooltipElement>().Max() + 1];
            }
            else if (data.Texts.ContainsKey(TooltipElement.Enhancement))
            {
                data.Texts.Remove(TooltipElement.Enhancement);
            }
        }
    }
}