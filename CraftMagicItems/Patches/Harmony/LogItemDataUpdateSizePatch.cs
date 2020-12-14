#if !PATCH21
using Kingmaker;
using Kingmaker.Items.Slots;
using Kingmaker.UI.Log;
using Kingmaker.UnitLogic.ActivatableAbilities;
#endif

namespace CraftMagicItems.Patches.Harmony
{
#if !PATCH21
    [HarmonyLib.HarmonyPatch(typeof(LogDataManager.LogItemData), "UpdateSize")]
    public static class LogItemDataUpdateSizePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix()
        {
            // Avoid null pointer exception when BattleLogManager not set.
            return Game.Instance.UI.BattleLogManager != null;
        }
    }
#endif
}