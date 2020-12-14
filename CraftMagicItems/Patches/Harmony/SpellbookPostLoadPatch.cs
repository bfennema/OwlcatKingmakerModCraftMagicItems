using Kingmaker.UnitLogic;

namespace CraftMagicItems.Patches.Harmony
{
    // Load Variant spells into m_KnownSpellLevels
    [HarmonyLib.HarmonyPatch(typeof(Spellbook), "PostLoad")]
    // ReSharper disable once UnusedMember.Local
    public static class SpellbookPostLoadPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(Spellbook __instance)
        {
            if (!Main.modEnabled)
            {
                return;
            }

            var mKnownSpells = Main.Accessors.GetSpellbookKnownSpells(__instance);
            var mKnownSpellLevels = Main.Accessors.GetSpellbookKnownSpellLevels(__instance);
            for (var level = 0; level < mKnownSpells.Length; ++level)
            {
                foreach (var spell in mKnownSpells[level])
                {
                    if (spell.Blueprint.Variants != null)
                    {
                        foreach (var variant in spell.Blueprint.Variants)
                        {
                            mKnownSpellLevels[variant] = level;
                        }
                    }
                }
            }
        }
    }
}