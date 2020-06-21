using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;

namespace CraftMagicItems {
    [ComponentName("Weapon Size Change")]
    public class WeaponSizeChange : GameLogicComponent {
        public int SizeCategoryChange;

        [HarmonyLib.HarmonyPatch(typeof(BlueprintItemWeapon), "BaseDamage", HarmonyLib.MethodType.Getter)]
        // ReSharper disable once UnusedMember.Local
        private static class BlueprintItemWeaponBaseDamage {
            private static void Postfix(BlueprintItemWeapon __instance, ref DiceFormula __result) {
                foreach (var enchantment in __instance.Enchantments) {
                    var component = enchantment.GetComponent<WeaponSizeChange>();
                    if (component != null) {
                        __result = WeaponDamageScaleTable.Scale(__result, __instance.Size + component.SizeCategoryChange, __instance.Size, __instance);
                        break;
                    }
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(RuleCalculateWeaponStats), "WeaponSize", HarmonyLib.MethodType.Getter)]
        // ReSharper disable once UnusedMember.Local
        private static class RuleCalculateWeaponStatsWeaponSizePatch {
            private static void Prefix(RuleCalculateWeaponStats __instance, ref int ___m_SizeShift, ref int __state, ref Size __result) {
                __state = ___m_SizeShift;
                foreach (var enchantment in __instance.Weapon.Enchantments) {
                    var component = enchantment.Blueprint.GetComponent<WeaponSizeChange>();
                    if (component != null) {
                        if ((component.SizeCategoryChange > 0 && ___m_SizeShift > 0) ||
                            (component.SizeCategoryChange < 0 && ___m_SizeShift < 0)) {
                            ___m_SizeShift = 0;
                        }
                        break;
                    }
                }
            }
            private static void Postfix(ref int ___m_SizeShift, int __state) {
                ___m_SizeShift = __state;
            }
        }
    }
}