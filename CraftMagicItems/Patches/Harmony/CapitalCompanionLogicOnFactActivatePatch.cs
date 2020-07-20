using Kingmaker;
using Kingmaker.Designers.TempMapCode.Capital;

namespace CraftMagicItems.Patches.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(CapitalCompanionLogic), "OnFactActivate")]
    // ReSharper disable once UnusedMember.Local
    public static class CapitalCompanionLogicOnFactActivatePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Prefix()
        {
            // Trigger project work on companions left behind in the capital, with a flag saying the party wasn't around while they were working.
            foreach (var companion in Game.Instance.Player.RemoteCompanions)
            {
                if (companion.Value != null)
                {
                    CraftingLogic.WorkOnProjects(companion.Value.Descriptor, true);
                }
            }
        }
    }
}