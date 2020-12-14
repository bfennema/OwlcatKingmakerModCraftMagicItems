using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace CraftMagicItems.Patches.Harmony
{
    // Owlcat's code doesn't correctly detect that a variant spell is in a spellList when its parent spell is.
    [HarmonyLib.HarmonyPatch(typeof(BlueprintAbility), "IsInSpellList")]
    // ReSharper disable once UnusedMember.Global
    public static class BlueprintAbilityIsInSpellListPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(BlueprintAbility __instance, BlueprintSpellList spellList, ref bool __result)
        {
            if (!__result && __instance.Parent != null && __instance.Parent != __instance)
            {
                __result = __instance.Parent.IsInSpellList(spellList);
            }
        }
    }
}