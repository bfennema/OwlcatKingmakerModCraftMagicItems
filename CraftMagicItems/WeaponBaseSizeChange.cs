using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Object = UnityEngine.Object;

namespace CraftMagicItems {
    [ComponentName("Weapon Base Size Change")]
    [AllowMultipleComponents]
    /**
     * Weapon size changes in RuleCalculateWeaponStats cannot stack, so do base size changes in a postfix patch
     */
    public class WeaponBaseSizeChange : GameLogicComponent {
        public int SizeCategoryChange;

        [HarmonyLib.HarmonyPatch(typeof(BlueprintItemWeapon), "BaseDamage", HarmonyLib.MethodType.Getter)]
        // ReSharper disable once UnusedMember.Local
        private static class BlueprintItemWeaponBaseDamage {
            private static void Postfix(BlueprintItemWeapon __instance, ref DiceFormula __result) {
                foreach (var enchantment in __instance.Enchantments) {
                    var component = enchantment.GetComponent<WeaponBaseSizeChange>();
                    if (component != null) {
                        __result = WeaponDamageScaleTable.Scale(__result, __instance.Size + component.SizeCategoryChange, __instance.Size, __instance);
                        break;
                    }
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(BlueprintItemWeapon), "AttackBonusStat", HarmonyLib.MethodType.Getter)]
        // ReSharper disable once UnusedMember.Local
        private static class BlueprintItemWeaponAttackBonusStat {
            private static void Postfix(BlueprintItemWeapon __instance, ref StatType __result) {
                foreach (var enchantment in __instance.Enchantments) {
                    var component = enchantment.GetComponent<WeaponBaseSizeChange>();
                    if (component != null) {
                        if (component.SizeCategoryChange != 0) {
                            __result = StatType.Strength;
                        }
                        break;
                    }
                }
            }
        }


        [HarmonyLib.HarmonyPatch(typeof(BlueprintItemWeapon), "IsTwoHanded", HarmonyLib.MethodType.Getter)]
        // ReSharper disable once UnusedMember.Local
        private static class BlueprintItemWeaponIsTwoHanded {
            private static void Postfix(BlueprintItemWeapon __instance, ref bool __result) {
                foreach (var enchantment in __instance.Enchantments) {
                    var component = enchantment.GetComponent<WeaponBaseSizeChange>();
                    if (component != null) {
                        if ((component.SizeCategoryChange == 1 && !__instance.Type.IsLight) || (component.SizeCategoryChange == 2)) {
                            __result = true;
                        } else if (component.SizeCategoryChange == -1 || component.SizeCategoryChange == -2) {
                            __result = false;
                        }
                        break;
                    }
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(BlueprintItemWeapon), "IsLight", HarmonyLib.MethodType.Getter)]
        // ReSharper disable once UnusedMember.Local
        private static class BlueprintItemWeaponIsLight {
            private static void Postfix(BlueprintItemWeapon __instance, ref bool __result) {
                foreach (var enchantment in __instance.Enchantments) {
                    var component = enchantment.GetComponent<WeaponBaseSizeChange>();
                    if (component != null) {
                        if (component.SizeCategoryChange == 1) {
                            __result = false;
                        } else if ((component.SizeCategoryChange == -1 && !__instance.Type.IsTwoHanded) || (component.SizeCategoryChange == -2)) {
                            __result = true;
                        }
                        break;
                    }
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(BlueprintItemWeapon), "SubtypeName", HarmonyLib.MethodType.Getter)]
        // ReSharper disable once UnusedMember.Local
        private static class BlueprintItemWeaponSubtypeName {
            private static void Postfix(BlueprintItemWeapon __instance, ref string __result) {
                foreach (var enchantment in __instance.Enchantments) {
                    var component = enchantment.GetComponent<WeaponBaseSizeChange>();
                    if (component != null) {
                        if (__instance.Type.IsLight && component.SizeCategoryChange == -1) {
                            __result = $"{new L10NString("1b8e1ff2-d137-402b-83d0-60c82c79fe67")} { __result}"; // tiny
                        } else if (!__instance.IsTwoHanded && component.SizeCategoryChange == -1) {
                            __result = $"{new L10NString("7266d912-6bab-4a1d-afab-e218f231429a")} { __result}"; // small
                        } else if ((__instance.Type.IsLight && component.SizeCategoryChange == 1) ||
                            (__instance.Type.IsTwoHanded && component.SizeCategoryChange == -1)) {
                            __result = $"{new L10NString("485ccfc5-1107-480a-9614-63692e2d9b28")} {__result}"; // medium
                        } else if (!__instance.Type.IsTwoHanded) {
                            __result = $"{new L10NString("8da7c769-c04f-4b16-92bb-f65f05e7f1a3")} {__result}"; // large
                        } else {
                            __result = $"{new L10NString("c6e8323e-68cf-4ffb-a095-fcaf2f257c48")} {__result}"; // huge
                        }
                        break;
                    }
                }
            }
        }
    }
}