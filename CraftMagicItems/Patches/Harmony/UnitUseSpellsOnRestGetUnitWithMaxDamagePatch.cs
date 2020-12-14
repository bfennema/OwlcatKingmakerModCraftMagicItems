using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;

namespace CraftMagicItems.Patches.Harmony
{
    /// <remarks>
    ///     Owlcat's code doesn't filter out undamaged characters, so it will always return someone.  This meant that with the "auto-cast healing" camping
    ///     option enabled on, healers would burn all their spell slots healing undamaged characters when they started resting, leaving them no spells to cast
    ///     when crafting.  Change it so it returns null if the most damaged character is undamaged.
    /// </remarks>
    [HarmonyLib.HarmonyPatch(typeof(UnitUseSpellsOnRest), "GetUnitWithMaxDamage")]
    // ReSharper disable once UnusedMember.Local
    public static class UnitUseSpellsOnRestGetUnitWithMaxDamagePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(ref UnitEntityData __result)
        {
            if (__result.Damage == 0 && (UnitPartDualCompanion.GetPair(__result)?.Damage ?? 0) == 0)
            {
                __result = null;
            }
        }
    }
}