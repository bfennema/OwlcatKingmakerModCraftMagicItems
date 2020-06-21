using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kingmaker.Blueprints;
#if !PATCH21_BETA
using Object = UnityEngine.Object;
#endif

namespace CraftMagicItems {
    public static class CustomBlueprintBuilder {
        public const int VanillaAssetIdLength = 32;

        private static bool enabled;
        private static Regex blueprintRegex;
        private static Func<BlueprintScriptableObject, Match, string> patchBlueprint;

        public struct Substitution
        {
            public string oldGuid;
            public string newGuid;
        }

        private static Substitution[] substitutions;

        public static List<string> CustomBlueprintIDs { get; } = new List<string>();

        public static bool DidDowngrade { get; private set; }

        public static void InitialiseBlueprintRegex(Regex initBlueprintRegex) {
            // This needs to happen as early as possible to allow graceful downgrading when the mod startup fails.
            blueprintRegex = initBlueprintRegex;
        }

        public static void Initialise(Func<BlueprintScriptableObject, Match, string> initPatchBlueprint, bool initEnabled, params Substitution[] initSubstitutions) {
            patchBlueprint = initPatchBlueprint;
            enabled = initEnabled;
            substitutions = initSubstitutions;
        }

        public static bool Enabled {
            set {
                enabled = value;
                if (enabled) {
                    DidDowngrade = false;
                } else {
                    // If we disable custom blueprints, remove any we've created from the ResourcesLibrary.
                    foreach (var assetId in CustomBlueprintIDs) {
                        var customBlueprint = ResourcesLibrary.LibraryObject.BlueprintsByAssetId?[assetId];
                        if (customBlueprint != null) {
                            ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Remove(assetId);
                            ResourcesLibrary.LibraryObject.GetAllBlueprints()?.Remove(customBlueprint);
                        }
                    }

                    CustomBlueprintIDs.Clear();
                }
            }
        }

        public static void Reset() {
            DidDowngrade = false;
        }

        private static BlueprintScriptableObject PatchBlueprint(string assetId, BlueprintScriptableObject blueprint) {
            if (blueprintRegex == null) {
                // Catastrophic failure - assume we're downgrading.
                DidDowngrade = true;
                return blueprint;
            }

            var match = blueprintRegex.Match(assetId);
            if (!match.Success) {
                return blueprint;
            }

            if (!enabled) {
                DidDowngrade = true;
                return blueprint;
            }

            if (blueprint.AssetGuid.Length == VanillaAssetIdLength) {
                // We have the original blueprint - clone it so we can make modifications which won't affect the original.
#if PATCH21_BETA
                var assetGuid = blueprint.AssetGuid;
                blueprint = (BlueprintScriptableObject)SerializedScriptableObject.Instantiate(blueprint);
                blueprint.AssetGuid = assetGuid;
                blueprint.name = blueprint.name + "(Clone)";
#else
                blueprint = Object.Instantiate(blueprint);
#endif
            }

            // Patch the blueprint
            var newAssetId = patchBlueprint(blueprint, match);
            if (newAssetId != null) {
#if PATCH21_BETA
                blueprint.OnEnable();
                foreach (var component in blueprint.ComponentsArray) {
                    component.OnEnable();
                }
#endif
                // Insert patched blueprint into ResourcesLibrary under the new GUID.
                Main.Accessors.SetBlueprintScriptableObjectAssetGuid(blueprint) = newAssetId;
                if (ResourcesLibrary.LibraryObject.BlueprintsByAssetId != null) {
                    ResourcesLibrary.LibraryObject.BlueprintsByAssetId[newAssetId] = blueprint;
                }
                ResourcesLibrary.LibraryObject.GetAllBlueprints()?.Add(blueprint);
                // Also record the custom GUID so we can clean it up if the mod is later disabled.
                CustomBlueprintIDs.Add(newAssetId);
            }

            return blueprint;
        }

        public static string AssetGuidWithoutMatch(string assetGuid, Match match = null) {
            if (match == null) {
                if (blueprintRegex == null) {
                    return assetGuid;
                }

                match = blueprintRegex.Match(assetGuid);
            }

            return !match.Success ? assetGuid : assetGuid.Substring(0, match.Index) + assetGuid.Substring(match.Index + match.Length);
        }

        // This patch is generic, and makes custom blueprints fall back to their initial version.
        public static class ResourcesLibraryTryGetBlueprintFallbackPatch {
            private static void Postfix(string assetId, ref BlueprintScriptableObject __result) {
                if (__result == null && assetId.Length > VanillaAssetIdLength) {
                    // Failed to load custom blueprint - return the original.
                    var originalGuid = assetId.Substring(0, VanillaAssetIdLength);
                    __result = ResourcesLibrary.TryGetBlueprint(originalGuid);
                }
            }
        }

        public static class ResourcesLibraryTryGetBlueprintModPatch {
            private static void Prefix(ref string assetId) {
                // Perform any backward compatibility substitutions
                for (var index = 0; index < substitutions.Length; index ++) {
                    assetId = assetId.Replace(substitutions[index].oldGuid, substitutions[index].newGuid);
                }
            }
            private static void Postfix(string assetId, ref BlueprintScriptableObject __result) {
                if (__result != null && assetId != __result.AssetGuid) {
                    __result = PatchBlueprint(assetId, __result);
                }
            }
        }
    }
}