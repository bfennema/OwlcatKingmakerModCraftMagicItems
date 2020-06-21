using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Kingmaker;
#if PATCH21
using Kingmaker.Assets.UI.Context;
#endif
using Kingmaker.Blueprints;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
#if !PATCH21_BETA
using Object = UnityEngine.Object;
#endif

namespace CraftMagicItems {
    public class CreateQuiverAbility : ScriptableObject {
        private static bool initialised;

        [HarmonyLib.HarmonyPatch(typeof(MainMenu), "Start")]
        // ReSharper disable once UnusedMember.Local
        public static class MainMenuStartPatch {
            private static void AddQuiver(BlueprintActivatableAbility ability, BlueprintBuff buff, string guid, PhysicalDamageMaterial material) {
#if PATCH21_BETA
                var component = SerializedScriptableObject.CreateInstance<AddOutgoingPhysicalDamageProperty>();
#else
                var component = ScriptableObject.CreateInstance<AddOutgoingPhysicalDamageProperty>();

#endif
                component.AddMaterial = true;
                component.Material = material;

#if PATCH21_BETA
                var quiverBuff = (BlueprintBuff)SerializedScriptableObject.Instantiate(buff);
#else
                var quiverBuff = Object.Instantiate(buff);

#endif
                quiverBuff.ComponentsArray = new BlueprintComponent[] { component };
                Main.Accessors.SetBlueprintUnitFactDisplayName(quiverBuff) = new L10NString($"craftMagicItems-mundane-{material.ToString().ToLower()}-quiver-name");
                Main.Accessors.SetBlueprintUnitFactDescription(quiverBuff) = new L10NString($"craftMagicItems-mundane-{material.ToString().ToLower()}-quiver-description");
#if PATCH21_BETA
                quiverBuff.OnEnable();
                foreach (var c in quiverBuff.ComponentsArray) {
                    c.OnEnable();
                }
#endif

                var buffGuid = $"{guid}#CraftMagicItems({material.ToString()}QuiverBuff)";

                Main.Accessors.SetBlueprintScriptableObjectAssetGuid(quiverBuff) = buffGuid;
                ResourcesLibrary.LibraryObject.BlueprintsByAssetId?.Add(buffGuid, quiverBuff);
                ResourcesLibrary.LibraryObject.GetAllBlueprints()?.Add(quiverBuff);

#if PATCH21_BETA
                var quiverAbility = (BlueprintActivatableAbility)SerializedScriptableObject.Instantiate(ability);
#else
                var quiverAbility = Object.Instantiate(ability);

#endif
                quiverAbility.Buff = quiverBuff;
                Main.Accessors.SetBlueprintUnitFactDisplayName(quiverAbility) = new L10NString($"craftMagicItems-mundane-{material.ToString().ToLower()}-quiver-name");
                Main.Accessors.SetBlueprintUnitFactDescription(quiverAbility) = new L10NString($"craftMagicItems-mundane-{material.ToString().ToLower()}-quiver-description");
#if PATCH21_BETA
                quiverBuff.OnEnable();
                foreach (var c in quiverAbility.ComponentsArray) {
                    c.OnEnable();
                }
#endif

                var abilityGuid = $"{guid}#CraftMagicItems({material.ToString()}QuiverAbility)";

                Main.Accessors.SetBlueprintScriptableObjectAssetGuid(quiverAbility) = abilityGuid;
                ResourcesLibrary.LibraryObject.BlueprintsByAssetId?.Add(abilityGuid, quiverAbility);
                ResourcesLibrary.LibraryObject.GetAllBlueprints()?.Add(quiverAbility);
            }

            public static void Postfix() {
                if (!initialised) {
                    initialised = true;

                    var guid = "5a769135b9b00684ea010d36ae49848e";
                    var ability = ResourcesLibrary.TryGetBlueprint<BlueprintActivatableAbility>(guid);
                    var buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("19cb147ab37f9234eb2fed40ca15b774");

                    AddQuiver(ability, buff, guid, PhysicalDamageMaterial.ColdIron);
                    AddQuiver(ability, buff, guid, PhysicalDamageMaterial.Silver);
                    AddQuiver(ability, buff, guid, PhysicalDamageMaterial.Adamantite);
                }
            }
        }

#if PATCH21
        [HarmonyLib.HarmonyPatch(typeof(MainMenuUiContext), "Initialize")]
        private static class MainMenuUiContextInitializePatch {
            private static void Postfix() {
                MainMenuStartPatch.Postfix();
            }
        }
#endif

        [HarmonyLib.HarmonyPatch(typeof(ItemSlot), "InsertItem")]
        // ReSharper disable once UnusedMember.Local
        private static class ItemSlotInsertItemPatch {
            static readonly MethodInfo methodToReplace = HarmonyLib.AccessTools.Property(typeof(ItemEntity), "IsStackable").GetGetMethod();
            static readonly string quiverGuid = "25f9b5ef564cbef49a1e54c48e67dfc1#CraftMagicItems";

            // ReSharper disable once UnusedMember.Local
            private static IEnumerable<HarmonyLib.CodeInstruction> Transpiler(IEnumerable<HarmonyLib.CodeInstruction> instructions) {
                foreach (var inst in instructions) {
                    if (inst.opcode == OpCodes.Callvirt && inst.operand as MethodInfo == methodToReplace) {
                        yield return new HarmonyLib.CodeInstruction(OpCodes.Call, new Func<ItemEntity, bool>(IsStackable).Method);
                    } else {
                        yield return inst;
                    }
                }
            }

            static private bool IsStackable(ItemEntity item) {
                return item.IsStackable && !item.Blueprint.AssetGuid.StartsWith(quiverGuid);
            }
        }
    }
}