using Kingmaker.Controllers.Rest;
using Kingmaker.UnitLogic;

namespace CraftMagicItems.Patches.Harmony
{
    // Make characters in the party work on their crafting projects when they rest.
    [HarmonyLib.HarmonyPatch(typeof(RestController), "ApplyRest")]
    // ReSharper disable once UnusedMember.Local
    public static class RestControllerApplyRestPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Prefix(UnitDescriptor unit)
        {
            CraftingLogic.WorkOnProjects(unit, false);
        }
    }
}