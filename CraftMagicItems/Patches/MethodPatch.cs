using HarmonyLib;
using System.Reflection;

namespace CraftMagicItems.Patches
{
    public struct MethodPatch
    {
        public MethodPatch(MethodBase original, HarmonyLib.HarmonyMethod prefix = null, HarmonyLib.HarmonyMethod postfix = null)
        {
            m_original = original;
            m_prefix = prefix;
            m_postfix = postfix;
        }

        public MethodInfo Patch(HarmonyLib.Harmony instance)
        {
            return instance.Patch(m_original, m_prefix, m_postfix);
        }

        public bool MatchOriginal(MethodBase method)
        {
            return m_original != null & m_original == method;
        }

        public bool MatchPrefix(MethodBase method)
        {
            return m_prefix != null && m_prefix.method == method;
        }

        public bool MatchPostfix(MethodBase method)
        {
            return m_postfix != null && m_postfix.method == method;
        }

        MethodBase m_original;
        HarmonyMethod m_prefix;
        HarmonyMethod m_postfix;
    }
}