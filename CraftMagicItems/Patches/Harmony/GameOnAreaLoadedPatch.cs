using Kingmaker.UI;
using Kingmaker.UI.Common;

namespace CraftMagicItems.Patches.Harmony
{
    public static class GameOnAreaLoadedPatch
    {
        private static void Postfix()
        {
            if (CustomBlueprintBuilder.DidDowngrade)
            {
                UIUtility.ShowMessageBox("Craft Magic Items is disabled.  All your custom enchanted items and crafting feats have been replaced with vanilla versions.",
#if PATCH21
                    DialogMessageBoxBase.BoxType.Message,
#else
                    DialogMessageBox.BoxType.Message,
#endif
                    null);

                CustomBlueprintBuilder.Reset();
            }
        }
    }
}