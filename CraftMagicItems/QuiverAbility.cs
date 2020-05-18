using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CraftMagicItems {
    public class CreateQuiverAbility : ScriptableObject {
        private static bool initialised;

        [Harmony12.HarmonyPatch(typeof(MainMenu), "Start")]
        // ReSharper disable once UnusedMember.Local
        private static class MainMenuStartPatch {
            private static void AddQuiver(BlueprintActivatableAbility ability, BlueprintBuff buff, string guid, PhysicalDamageMaterial material) {
                var component = ScriptableObject.CreateInstance<AddOutgoingPhysicalDamageProperty>();
                component.AddMaterial = true;
                component.Material = material;

                var quiverBuff = Object.Instantiate(buff);
                quiverBuff.ComponentsArray = new BlueprintComponent[] { component };
                Main.Accessors.SetBlueprintUnitFactDisplayName(quiverBuff, new L10NString($"craftMagicItems-mundane-{material.ToString().ToLower()}-quiver-name"));
                Main.Accessors.SetBlueprintUnitFactDescription(quiverBuff, new L10NString($"craftMagicItems-mundane-{material.ToString().ToLower()}-quiver-description"));

                var quiverAbility = Object.Instantiate(ability);
                quiverAbility.Buff = quiverBuff;
                Main.Accessors.SetBlueprintUnitFactDisplayName(quiverAbility, new L10NString($"craftMagicItems-mundane-{material.ToString().ToLower()}-quiver-name"));
                Main.Accessors.SetBlueprintUnitFactDescription(quiverAbility, new L10NString($"craftMagicItems-mundane-{material.ToString().ToLower()}-quiver-description"));

                var newGuid = $"{guid}#CraftMagicItems({material.ToString()}QuiverAbility)";

                Main.Accessors.SetBlueprintScriptableObjectAssetGuid(quiverAbility, newGuid);
                ResourcesLibrary.LibraryObject.BlueprintsByAssetId?.Add(newGuid, quiverAbility);
                ResourcesLibrary.LibraryObject.GetAllBlueprints().Add(quiverAbility);
            }

            // ReSharper disable once UnusedMember.Local
            private static void Postfix() {
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

        [Harmony12.HarmonyPatch(typeof(ItemSlot), "InsertItem")]
        // ReSharper disable once UnusedMember.Local
        private static class ItemSlotInsertItemPatch {
            static readonly MethodInfo methodToReplace = AccessTools.Property(typeof(ItemEntity), "IsStackable").GetGetMethod();
            static readonly string quiverGuid = "25f9b5ef564cbef49a1e54c48e67dfc1#CraftMagicItems";

            // ReSharper disable once UnusedMember.Local
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                foreach (var inst in instructions) {
                    if (inst.opcode == OpCodes.Callvirt && inst.operand as MethodInfo == methodToReplace) {
                        yield return new Harmony12.CodeInstruction(OpCodes.Call, new Func<ItemEntity, bool>(IsStackable).Method);
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