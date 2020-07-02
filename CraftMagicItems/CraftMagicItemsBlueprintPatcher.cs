using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CraftMagicItems.Constants;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.ResourceLinks;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.View.Animation;
using Kingmaker.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CraftMagicItems {
    public class CraftMagicItemsBlueprintPatcher {
        public const string TimerBlueprintGuid = "52e4be2ba79c8c94d907bdbaf23ec15f#CraftMagicItems(timer)";
        public const string BondedItemBuffBlueprintGuid = "1efa689e594ca82428b8fff1a739c9be#CraftMagicItems(bondedItem)";

        private const string MatchedParensComma = @"([^(),]+|(?<Level>\()|(?<-Level>\))|(?(Level),))+(?(Level)(?!))";

        public static readonly Regex BlueprintRegex =
            new Regex($"({OldBlueprintPrefix}|{BlueprintPrefix})"
                      + @"\(("
                      + @"CL=(?<casterLevel>\d+)(?<spellLevelMatch>,SL=(?<spellLevel>\d+))?(?<spellIdMatch>,spellId=\((?<spellId>" + MatchedParensComma +
                      @")\))?"
                      + @"|enchantments=\((?<enchantments>|" + MatchedParensComma + @")\)(,remove=(?<remove>[0-9a-f;]+))?(,name=(?<name>[^✔]+)✔)?"
                      + @"(,ability=(?<ability>null|[0-9a-f]+))?"
                      + $"(,activatableAbility=(?<activatableAbility>{MatchedParensComma}))?(,charges=(?<charges>[0-9]+))?(,weight=(?<weight>[0-9]+))?"
                      + @"(,material=(?<material>[a-zA-Z]+))?(,visual=(?<visual>null|[0-9a-f]+))?"
                      + @"(,animation=(?<animation>null|[a-zA-Z]+))?(,priceAdjust=(?<priceAdjust>[-0-9]+))?"
                      + @"(,CL=(?<casterLevel>[0-9]+))?(,SL=(?<spellLevel>[0-9]+))?(,perDay=(?<perDay>[0-9]+))?(,nameId=(?<nameId>[^,]+))?(,descriptionId=(?<descriptionId>[^,]+))?"
                      + $"(,secondEnd=(?<secondEnd>{MatchedParensComma}))?"
                      + @"|feat=(?<feat>[-a-z]+)"
                      + @"|(?<timer>timer)"
                      + @"|(?<bondedItem>bondedItem)"
                      + @"|(?<components>(Component\[(?<index>[0-9]+)\](?<field>[^=]*)?=(?<value>" + MatchedParensComma + @"),?)+(,nameId=(?<nameId>[^,)]+))?"
                      + @"(,descriptionId=(?<descriptionId>[^,)]+))?)"
                      + @")\)");

        private static readonly ItemsFilter.ItemType[] SlotsWhichShowEnchantments = {
            ItemsFilter.ItemType.Weapon,
            ItemsFilter.ItemType.Armor,
            ItemsFilter.ItemType.Shield
        };

        // TODO remove the ScribeScroll prefix eventually
        private const string OldBlueprintPrefix = "#ScribeScroll";
        public const string BlueprintPrefix = "#CraftMagicItems";

        public readonly Dictionary<PhysicalDamageMaterial, string> PhysicalDamageMaterialEnchantments = new Dictionary<PhysicalDamageMaterial, string>() {
            {PhysicalDamageMaterial.Adamantite, "ab39e7d59dd12f4429ffef5dca88dc7b"},
            {PhysicalDamageMaterial.ColdIron, "e5990dc76d2a613409916071c898eee8"},
            {PhysicalDamageMaterial.Silver, "0ae8fc9f2e255584faf4d14835224875"}
        };

        private readonly CraftMagicItemsAccessors accessors;

        public CraftMagicItemsBlueprintPatcher(CraftMagicItemsAccessors accessors, bool modEnabled) {
            this.accessors = accessors;
            CustomBlueprintBuilder.Initialise(ApplyBlueprintPatch, modEnabled,
                "d8e1ebc1062d8cc42abff78783856b0d#CraftMagicItems(Component[1]=CraftMagicItems.WeaponSizeChange#CraftMagicItems,Component[1].SizeCategoryChange=1)",
                "d8e1ebc1062d8cc42abff78783856b0d#CraftMagicItems(Component[1]=CraftMagicItems.WeaponBaseSizeChange#CraftMagicItems,Component[1].SizeCategoryChange=1)",
                "7f2b282626862e345935bbea5e66424b#CraftMagicItems(feat=arms-armour)",
                "7f2b282626862e345935bbea5e66424b#CraftMagicItems(feat=arms-armor)");
        }

        public string BuildCustomSpellItemGuid(string originalGuid, int casterLevel, int spellLevel = -1, string spellId = null) {
            // Check if GUID is already customised by this mod
            var match = BlueprintRegex.Match(originalGuid);
            if (match.Success && match.Groups["casterLevel"].Success) {
                // Remove the existing customisation
                originalGuid = CustomBlueprintBuilder.AssetGuidWithoutMatch(originalGuid, match);
                // Use any values which aren't explicitly overridden
                if (spellLevel == -1 && match.Groups["spellLevelMatch"].Success) {
                    spellLevel = int.Parse(match.Groups["spellLevel"].Value);
                }

                if (spellId == null && match.Groups["spellIdMatch"].Success) {
                    spellId = match.Groups["spellId"].Value;
                }
            }

            return $"{originalGuid}{BlueprintPrefix}(CL={casterLevel}" +
                   $"{(spellLevel == -1 ? "" : $",SL={spellLevel}")}" +
                   $"{(spellId == null ? "" : $",spellId=({spellId})")}" +
                   ")";
        }

        public string BuildCustomRecipeItemGuid(string originalGuid, IEnumerable<string> enchantments, string[] remove = null, string name = null,
            string ability = null, string activatableAbility = null, int charges = -1, int weight = -1, PhysicalDamageMaterial material = 0, string visual = null, string animation = null,
            int casterLevel = -1, int spellLevel = -1, int perDay = -1, string nameId = null, string descriptionId = null, string secondEndGuid = null,
            int priceAdjust = 0) {
            // Check if GUID is already customised by this mod
            var match = BlueprintRegex.Match(originalGuid);
            if (match.Success && match.Groups["enchantments"].Success) {
                var enchantmentsList = enchantments.Concat(match.Groups["enchantments"].Value.Split(';'))
                    .Where(guid => guid.Length > 0).Distinct().ToList();
                var removeList = match.Groups["remove"].Success
                    ? (remove ?? Enumerable.Empty<string>()).Concat(match.Groups["remove"].Value.Split(';')).Distinct().ToList()
                    : remove?.ToList();
                if (removeList != null) {
                    foreach (var guid in removeList.ToArray()) {
                        if (enchantmentsList.Contains(guid)) {
                            enchantmentsList.Remove(guid);
                            removeList.Remove(guid);
                        }
                    }
                }

                enchantments = enchantmentsList;
                remove = removeList?.Count > 0 ? removeList.ToArray() : null;
                if (name == null && match.Groups["name"].Success) {
                    name = match.Groups["name"].Value;
                    nameId = null;
                }

                if (ability == null && match.Groups["ability"].Success) {
                    ability = match.Groups["ability"].Value;
                }

                if (activatableAbility == null && match.Groups["activatableAbility"].Success) {
                    activatableAbility = match.Groups["activatableAbility"].Value;
                }

                if (charges == -1 && match.Groups["charges"].Success) {
                    perDay = int.Parse(match.Groups["charges"].Value);
                }

                if (weight == -1 && match.Groups["weight"].Success) {
                    weight = int.Parse(match.Groups["weight"].Value);
                }

                if (material == 0 && match.Groups["material"].Success) {
                    Enum.TryParse(match.Groups["material"].Value, out material);
                }

                if (visual == null && match.Groups["visual"].Success) {
                    visual = match.Groups["visual"].Value;
                }

                if (animation == null && match.Groups["animation"].Success) {
                    animation = match.Groups["animation"].Value;
                }

                if (priceAdjust == 0 && match.Groups["priceAdjust"].Success) {
                    priceAdjust = int.Parse(match.Groups["priceAdjust"].Value);
                }

                if (match.Groups["casterLevel"].Success) {
                    casterLevel = Math.Max(casterLevel, int.Parse(match.Groups["casterLevel"].Value));
                }

                if (match.Groups["spellLevel"].Success) {
                    spellLevel = Math.Max(spellLevel, int.Parse(match.Groups["spellLevel"].Value));
                }

                if (perDay == -1 && match.Groups["perDay"].Success) {
                    perDay = int.Parse(match.Groups["perDay"].Value);
                }

                if (name == null && nameId == null && match.Groups["nameId"].Success) {
                    nameId = match.Groups["nameId"].Value;
                }

                if (descriptionId == null && match.Groups["descriptionId"].Success) {
                    descriptionId = match.Groups["descriptionId"].Value;
                }

                if (secondEndGuid == null && match.Groups["secondEnd"].Success) {
                    secondEndGuid = match.Groups["secondEnd"].Value;
                }

                // Remove the original customisation.
                originalGuid = CustomBlueprintBuilder.AssetGuidWithoutMatch(originalGuid, match);
            }

            return $"{originalGuid}{BlueprintPrefix}(enchantments=({string.Join(";", enchantments)})" +
                   $"{(remove == null || remove.Length == 0 ? "" : ",remove=" + string.Join(";", remove))}" +
                   $"{(name == null ? "" : $",name={name.Replace('✔', '_')}✔")}" +
                   $"{(ability == null ? "" : $",ability={ability}")}" +
                   $"{(activatableAbility == null ? "" : $",activatableAbility={activatableAbility}")}" +
                   $"{(charges == -1 ? "" : $",charges={charges}")}" +
                   $"{(weight == -1 ? "" : $",weight={weight}")}" +
                   $"{(material == 0 ? "" : $",material={material}")}" +
                   $"{(visual == null ? "" : $",visual={visual}")}" +
                   $"{(animation == null ? "" : $",animation={animation}")}" +
                   $"{(priceAdjust == 0 ? "" : $",priceAdjust={priceAdjust}")}" +
                   $"{(casterLevel == -1 ? "" : $",CL={casterLevel}")}" +
                   $"{(spellLevel == -1 ? "" : $",SL={spellLevel}")}" +
                   $"{(perDay == -1 ? "" : $",perDay={perDay}")}" +
                   $"{(nameId == null ? "" : $",nameId={nameId}")}" +
                   $"{(descriptionId == null || descriptionId == "null" ? "" : $",descriptionId={descriptionId}")}" +
                   $"{(secondEndGuid == null ? "" : $",secondEnd={secondEndGuid}")}" +
                   ")";
        }

        private string BuildCustomComponentsItemGuid(string originalGuid, string[] values, string nameId, string descriptionId) {
            var components = "";
            for (var index = 0; index < values.Length; index += 3) {
                components += $"{(index > 0 ? "," : "")}Component[{values[index]}]{values[index + 1]}={values[index + 2]}";
            }

            return
                $"{originalGuid}{BlueprintPrefix}({components}{(nameId == null ? "" : $",nameId={nameId}")}{(descriptionId == null ? "" : $",descriptionId={descriptionId}")})";
        }

        private string BuildCustomFeatGuid(string originalGuid, string feat) {
            return $"{originalGuid}{BlueprintPrefix}(feat={feat})";
        }

        private void ApplyBuffBlueprintPatch(BlueprintBuff blueprint, BlueprintComponent component, string nameId) {
            blueprint.ComponentsArray = new[] {component};
            accessors.SetBlueprintBuffFlags(blueprint, 2 + 8); // BlueprintBluff.Flags enum is private.  Values are HiddenInUi = 2 + StayOnDeath = 8
            blueprint.FxOnStart = new PrefabLink();
            blueprint.FxOnRemove = new PrefabLink();
            // Set the display name - it's hidden in the UI, but someone might find it in Bag of Tricks.
            accessors.SetBlueprintUnitFactDisplayName(blueprint, new L10NString(nameId));
        }

        private string ApplyTimerBlueprintPatch(BlueprintBuff blueprint) {
            ApplyBuffBlueprintPatch(blueprint, ScriptableObject.CreateInstance<CraftingTimerComponent>(), "craftMagicItems-timer-buff-name");
            return TimerBlueprintGuid;
        }

        private string ApplyBondedItemBlueprintPatch(BlueprintBuff blueprint) {
            ApplyBuffBlueprintPatch(blueprint, ScriptableObject.CreateInstance<BondedItemComponent>(), "craftMagicItems-bondedItem-buff-name");
            return BondedItemBuffBlueprintGuid;
        }

        private string ApplyFeatBlueprintPatch(BlueprintFeature blueprint, Match match) {
            var feat = match.Groups["feat"].Value;
            accessors.SetBlueprintUnitFactDisplayName(blueprint, new L10NString($"craftMagicItems-feat-{feat}-displayName"));
            accessors.SetBlueprintUnitFactDescription(blueprint, new L10NString($"craftMagicItems-feat-{feat}-description"));
            var icon = Image2Sprite.Create($"{Main.ModEntry.Path}/Icons/craft-{feat}.png");
            accessors.SetBlueprintUnitFactIcon(blueprint, icon);
            var prerequisite = ScriptableObject.CreateInstance<PrerequisiteCasterLevel>();
            var featGuid = BuildCustomFeatGuid(blueprint.AssetGuid, feat);
            var itemData = Main.ItemCraftingData.First(data => data.FeatGuid == featGuid);
            prerequisite.SetPrerequisiteCasterLevel(itemData.MinimumCasterLevel);
            blueprint.ComponentsArray = new BlueprintComponent[] {prerequisite};
            return featGuid;
        }

        private string ApplySpellItemBlueprintPatch(BlueprintItemEquipmentUsable blueprint, Match match) {
            var casterLevel = int.Parse(match.Groups["casterLevel"].Value);
            blueprint.CasterLevel = casterLevel;
            var spellLevel = -1;
            if (match.Groups["spellLevelMatch"].Success) {
                spellLevel = int.Parse(match.Groups["spellLevel"].Value);
                blueprint.SpellLevel = spellLevel;
            }

            string spellId = null;
            if (match.Groups["spellIdMatch"].Success) {
                spellId = match.Groups["spellId"].Value;
                blueprint.Ability = (BlueprintAbility) ResourcesLibrary.TryGetBlueprint(spellId);
                blueprint.DC = 0;
            }

            if (blueprint.Ability != null && blueprint.Ability.LocalizedSavingThrow != null && blueprint.Ability.LocalizedSavingThrow.IsSet()) {
                blueprint.DC = 10 + blueprint.SpellLevel * 3 / 2;
            }

            accessors.SetBlueprintItemEquipmentUsableCost(blueprint, 0); // Allow the game to auto-calculate the cost
            // Also store the new item blueprint in our spell-to-item lookup dictionary.
            var itemBlueprintsForSpell = Main.FindItemBlueprintsForSpell(blueprint.Ability, blueprint.Type);
            if (itemBlueprintsForSpell == null || !itemBlueprintsForSpell.Contains(blueprint)) {
                Main.AddItemBlueprintForSpell(blueprint.Type, blueprint);
            }

            return BuildCustomSpellItemGuid(blueprint.AssetGuid, casterLevel, spellLevel, spellId);
        }

        public static bool DoesBlueprintShowEnchantments(BlueprintItem blueprint) {
            if (blueprint.ItemType == ItemsFilter.ItemType.Neck && Main.ItemPlusEquivalent(blueprint) > 0) {
                return true;
            }
            return SlotsWhichShowEnchantments.Contains(blueprint.ItemType);
        }

        private string ApplyRecipeItemBlueprintPatch(BlueprintItemEquipment blueprint, Match match) {
            var priceDelta = blueprint.Cost - Main.RulesRecipeItemCost(blueprint);
            string secondEndGuid = null;

            var removedIds = new List<string>();
            if (match.Groups["remove"].Success) {
                removedIds = match.Groups["remove"].Value.Split(';').ToList();
            }

            var enchantmentsValue = match.Groups["enchantments"].Value;
            var enchantmentIds = enchantmentsValue.Split(';').ToList();

            if (blueprint is BlueprintItemShield shield) {
                var armorComponentClone = Object.Instantiate(shield.ArmorComponent);
                ApplyRecipeItemBlueprintPatch(armorComponentClone, match);
                if (match.Groups["secondEnd"].Success) {
                    secondEndGuid = match.Groups["secondEnd"].Value;
                } else if (shield.WeaponComponent != null) {
                    var weaponEnchantmentIds = enchantmentIds;
                    if (weaponEnchantmentIds.Count > 0) {
                        enchantmentIds = new List<string>();
                        var armorEnchantments = armorComponentClone.Enchantments;
                        foreach (var enchantment in armorEnchantments) {
                            if (weaponEnchantmentIds.Contains(enchantment.AssetGuid)) {
                                weaponEnchantmentIds.Remove(enchantment.AssetGuid);
                                enchantmentIds.Add(enchantment.AssetGuid);
                            }
                        }
                    }

                    var weaponRemovedIds = removedIds;
                    if (weaponRemovedIds.Count > 0) {
                        removedIds = new List<string>();
                        var armorEnchantments = shield.ArmorComponent.Enchantments;
                        foreach (var enchantment in armorEnchantments) {
                            if (weaponRemovedIds.Contains(enchantment.AssetGuid)) {
                                weaponRemovedIds.Remove(enchantment.AssetGuid);
                                removedIds.Add(enchantment.AssetGuid);
                            }
                        }
                    }

                    if (weaponEnchantmentIds.Count > 0 || weaponRemovedIds.Count > 0) {
                        PhysicalDamageMaterial weaponMaterial = 0;
                        if (match.Groups["material"].Success) {
                            Enum.TryParse(match.Groups["material"].Value, out weaponMaterial);
                        }
                        secondEndGuid = BuildCustomRecipeItemGuid(shield.WeaponComponent.AssetGuid, weaponEnchantmentIds,
                            weaponRemovedIds.Count > 0 ? weaponRemovedIds.ToArray() : null, material: weaponMaterial);
                    }
                }
                if (secondEndGuid != null) {
                    var weaponComponent = ResourcesLibrary.TryGetBlueprint<BlueprintItemWeapon>(secondEndGuid);
                    if ((weaponComponent.DamageType.Physical.Form & PhysicalDamageForm.Piercing) != 0) {
                        accessors.SetBlueprintItemWeight(weaponComponent, 5.0f);
                    } else {
                        accessors.SetBlueprintItemWeight(weaponComponent, 0.0f);
                    }
                    accessors.SetBlueprintItemShieldWeaponComponent(shield, weaponComponent);
                    accessors.SetBlueprintItemWeight(armorComponentClone, armorComponentClone.Weight + weaponComponent.Weight);
                }
                accessors.SetBlueprintItemShieldArmorComponent(shield, armorComponentClone);
            }

            var initiallyMundane = blueprint.Enchantments.Count == 0 && blueprint.Ability == null && blueprint.ActivatableAbility == null;
            var replaceAbility = false;

            // Copy Enchantments so we leave base blueprint alone
            var enchantmentsCopy = blueprint.Enchantments.ToList();
            if (!(blueprint is BlueprintItemShield)) {
                accessors.SetBlueprintItemCachedEnchantments(blueprint, enchantmentsCopy);
            }
            // Remove enchantments first, to see if we end up with an item with no abilities.
            var removed = new List<BlueprintItemEnchantment>();
            if (match.Groups["remove"].Success) {
                foreach (var guid in removedIds) {
                    var enchantment = ResourcesLibrary.TryGetBlueprint<BlueprintItemEnchantment>(guid);
                    if (!enchantment) {
                        throw new Exception($"Failed to load enchantment {guid}");
                    }

                    removed.Add(enchantment);
                    enchantmentsCopy.Remove(enchantment);
                }
            }

            string ability = null;
            if (match.Groups["ability"].Success) {
                ability = match.Groups["ability"].Value;
                if (blueprint.Ability != null) {
                    replaceAbility = true;
                }
                blueprint.Ability = ability == "null" ? null : ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(ability);
                blueprint.SpendCharges = true;
                blueprint.RestoreChargesOnRest = true;
            }

            string activatableAbility = null;
            if (match.Groups["activatableAbility"].Success) {
                activatableAbility = match.Groups["activatableAbility"].Value;
                if (blueprint.ActivatableAbility != null) {
                    replaceAbility = true;
                }
                blueprint.ActivatableAbility = activatableAbility == "null"
                    ? null
                    : ResourcesLibrary.TryGetBlueprint<BlueprintActivatableAbility>(activatableAbility);
            }

            int charges = -1;
            if (match.Groups["charges"].Success) {
                charges = int.Parse(match.Groups["charges"].Value);
                blueprint.Charges = charges;
                blueprint.SpendCharges = true;
                blueprint.RestoreChargesOnRest = false;
                accessors.SetBlueprintItemIsStackable(blueprint, true);
            }

            int weight = -1;
            if (match.Groups["weight"].Success) {
                weight = int.Parse(match.Groups["weight"].Value);
                accessors.SetBlueprintItemWeight(blueprint, weight * .01f);
            }

            var priceAdjust = 0;
            if (match.Groups["priceAdjust"].Success) {
                priceAdjust = int.Parse(match.Groups["priceAdjust"].Value);
                priceDelta += priceAdjust;
            } else if (!initiallyMundane && enchantmentsCopy.Count == 0
                                  && (blueprint.Ability == null || ability != null) && (blueprint.ActivatableAbility == null || activatableAbility != null)) {
                // We're down to a base item with no abilities - reset priceDelta.
                priceDelta = 0;
            }

            var skipped = new List<BlueprintItemEnchantment>();
            var enchantmentsForDescription = new List<BlueprintItemEnchantment>();
            int sizeCategoryChange = 0;
            if (!string.IsNullOrEmpty(enchantmentsValue)) {
                foreach (var guid in enchantmentIds) {
                    var enchantment = ResourcesLibrary.TryGetBlueprint<BlueprintItemEnchantment>(guid);
                    if (!enchantment) {
                        throw new Exception($"Failed to load enchantment {guid}");
                    }

                    var component = enchantment.GetComponent<AddStatBonusEquipment>();
                    if (!string.IsNullOrEmpty(enchantment.Name) ||
                        (component && component.Descriptor != ModifierDescriptor.ArmorEnhancement && component.Descriptor != ModifierDescriptor.ShieldEnhancement)) {
                        skipped.Add(enchantment);
                    }
                    enchantmentsForDescription.Add(enchantment);
                    if (blueprint is BlueprintItemArmor && guid == ItemQualityBlueprints.MithralArmorEnchantmentGuid) {
                        // Mithral equipment has half weight
                        accessors.SetBlueprintItemWeight(blueprint, blueprint.Weight / 2);
                    }
                    if (blueprint is BlueprintItemEquipmentHand) {
                        var weaponBaseSizeChange = enchantment.GetComponent<WeaponBaseSizeChange>();
                        if (weaponBaseSizeChange != null) {
                            sizeCategoryChange = weaponBaseSizeChange.SizeCategoryChange;
                            if (sizeCategoryChange > 0) {
                                accessors.SetBlueprintItemWeight(blueprint, blueprint.Weight * 2);
                            } else if (sizeCategoryChange < 0) {
                                accessors.SetBlueprintItemWeight(blueprint, blueprint.Weight / 2);
                            }
                        }
                    }

                    if (!(blueprint is BlueprintItemShield) && (Main.GetItemType(blueprint) != ItemsFilter.ItemType.Shield
                                                                || Main.FindSourceRecipe(guid, blueprint) != null)) {
                        enchantmentsCopy.Add(enchantment);
                    }
                }
            }

            PhysicalDamageMaterial material = 0;
            if (match.Groups["material"].Success) {
                Enum.TryParse(match.Groups["material"].Value, out material);
                if (blueprint is BlueprintItemWeapon weapon) {
                    accessors.SetBlueprintItemWeaponDamageType(weapon, TraverseCloneAndSetField(weapon.DamageType, "Physical.Material", material.ToString()));
                    accessors.SetBlueprintItemWeaponOverrideDamageType(weapon, true);
                    var materialGuid = PhysicalDamageMaterialEnchantments[material];
                    var enchantment = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponEnchantment>(materialGuid);
                    enchantmentsCopy.Add(enchantment);
                    skipped.Add(enchantment);
                    enchantmentsForDescription.Add(enchantment);
                    if (material == PhysicalDamageMaterial.Silver) {
                        // PhysicalDamageMaterial.Silver is really Mithral, and Mithral equipment has half weight
                        accessors.SetBlueprintItemWeight(blueprint, blueprint.Weight / 2);
                    }
                }
            }

            var equipmentHand = blueprint as BlueprintItemEquipmentHand;
            string visual = null;
            if (match.Groups["visual"].Success) {
                visual = match.Groups["visual"].Value;
                // Copy icon from a different item
                var copyFromBlueprint = visual == "null" ? null : ResourcesLibrary.TryGetBlueprint<BlueprintItem>(visual);
                var iconSprite = copyFromBlueprint == null ? null : copyFromBlueprint.Icon;
                accessors.SetBlueprintItemIcon(blueprint, iconSprite);
                if (equipmentHand != null && copyFromBlueprint is BlueprintItemEquipmentHand srcEquipmentHand) {
                    accessors.SetBlueprintItemEquipmentHandVisualParameters(equipmentHand, srcEquipmentHand.VisualParameters);
                } else if (blueprint is BlueprintItemArmor armor && copyFromBlueprint is BlueprintItemArmor srcArmor) {
                    accessors.SetBlueprintItemArmorVisualParameters(armor, srcArmor.VisualParameters);
                }
            }

            string animation = null;
            if (match.Groups["animation"].Success) {
                animation = match.Groups["animation"].Value;
                WeaponAnimationStyle weaponAnimation;
                if (Enum.TryParse(animation, out weaponAnimation)) {
                    if (equipmentHand != null) {
                        accessors.SetBlueprintItemEquipmentWeaponAnimationStyle(equipmentHand.VisualParameters, weaponAnimation);
                    }
                }
            }

            var casterLevel = -1;
            if (match.Groups["casterLevel"].Success) {
                casterLevel = int.Parse(match.Groups["casterLevel"].Value);
                blueprint.CasterLevel = casterLevel;
            }

            var spellLevel = -1;
            if (match.Groups["spellLevel"].Success) {
                spellLevel = int.Parse(match.Groups["spellLevel"].Value);
                blueprint.SpellLevel = spellLevel;
            }

            var perDay = -1;
            if (match.Groups["perDay"].Success) {
                perDay = int.Parse(match.Groups["perDay"].Value);
                blueprint.Charges = perDay;
                blueprint.SpendCharges = true;
                blueprint.RestoreChargesOnRest = true;
                if (blueprint.Ability.LocalizedSavingThrow != null && blueprint.Ability.LocalizedSavingThrow.IsSet()) {
                    blueprint.DC = 10 + blueprint.SpellLevel * 3 / 2;
                } else {
                    blueprint.DC = 0;
                }
            }

            string name = null;
            if (match.Groups["name"].Success) {
                name = match.Groups["name"].Value;
                accessors.SetBlueprintItemDisplayNameText(blueprint, new FakeL10NString(name));
            }

            string nameId = null;
            if (name == null && match.Groups["nameId"].Success) {
                nameId = match.Groups["nameId"].Value;
                accessors.SetBlueprintItemDisplayNameText(blueprint, new L10NString(nameId));
            }

            string descriptionId = null;
            if (match.Groups["descriptionId"].Success) {
                descriptionId = match.Groups["descriptionId"].Value;
                if (descriptionId == "craftMagicItems-material-silver-weapon-description") {
                    // Backwards compatibility - remove custom silver weapon description
                    descriptionId = null;
                }
            }

            if (match.Groups["secondEnd"].Success && blueprint is BlueprintItemWeapon doubleWeapon) {
                secondEndGuid = match.Groups["secondEnd"].Value;
                doubleWeapon.SecondWeapon = ResourcesLibrary.TryGetBlueprint<BlueprintItemWeapon>(secondEndGuid);
            }

            if (!DoesBlueprintShowEnchantments(blueprint)) {
                skipped.Clear();
            }
            if (descriptionId != null) {
                accessors.SetBlueprintItemDescriptionText(blueprint, new L10NString(descriptionId));
                accessors.SetBlueprintItemFlavorText(blueprint, new L10NString(""));
            } else if ((blueprint is BlueprintItemShield || Main.GetItemType(blueprint) != ItemsFilter.ItemType.Shield)
                && (!DoesBlueprintShowEnchantments(blueprint) || enchantmentsForDescription.Count != skipped.Count || removed.Count > 0)) {
                accessors.SetBlueprintItemDescriptionText(blueprint,
                    Main.BuildCustomRecipeItemDescription(blueprint, enchantmentsForDescription, skipped, removed, replaceAbility, ability, casterLevel, perDay));
                accessors.SetBlueprintItemFlavorText(blueprint, new L10NString(""));
            }

            accessors.SetBlueprintItemCost(blueprint, Main.RulesRecipeItemCost(blueprint) + priceDelta);
            return BuildCustomRecipeItemGuid(blueprint.AssetGuid, enchantmentIds, removedIds.Count > 0 ? removedIds.ToArray() : null, name, ability,
                activatableAbility, charges, weight, material, visual, animation, casterLevel, spellLevel, perDay, nameId, descriptionId, secondEndGuid, priceAdjust);
        }

        private T CloneObject<T>(T originalObject) {
            var type = originalObject.GetType();
            if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                return (T) (object) Object.Instantiate(originalObject as Object);
            }

            var clone = (T) Activator.CreateInstance(type);
            for (; type != null && type != typeof(Object); type = type.BaseType) {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields) {
                    field.SetValue(clone, field.GetValue(originalObject));
                }
            }

            return clone;
        }

        private T TraverseCloneAndSetField<T>(T original, string field, string value) where T : class {
            if (string.IsNullOrEmpty(field)) {
                value = value.Replace("#", ", ");
                var componentType = Type.GetType(value);
                if (componentType == null) {
                    throw new Exception($"Failed to create object with type {value}");
                }

                var componentObject = typeof(ScriptableObject).IsAssignableFrom(componentType)
                    ? ScriptableObject.CreateInstance(componentType)
                    : Activator.CreateInstance(componentType);

                if (!(componentObject is T component)) {
                    throw new Exception($"Failed to create expected instance with type {value}, " +
                                        $"result is {componentType.FullName}");
                }

                return component;
            } else {
                // Strip leading . off field
                if (field.StartsWith(".")) {
                    field = field.Substring(1);
                }
            }

            var clone = CloneObject(original);
            var fieldNameEnd = field.IndexOf('.');
            if (fieldNameEnd < 0) {
                var fieldAccess = Harmony12.Traverse.Create(clone).Field(field);
                if (!fieldAccess.FieldExists()) {
                    throw new Exception(
                        $"Field {field} does not exist on original of type {clone.GetType().FullName}, available fields: {string.Join(", ", Harmony12.Traverse.Create(clone).Fields())}");
                }

                if (value == "null") {
                    fieldAccess.SetValue(null);
                } else if (typeof(BlueprintScriptableObject).IsAssignableFrom(fieldAccess.GetValueType())) {
                    fieldAccess.SetValue(ResourcesLibrary.TryGetBlueprint(value));
                } else if (fieldAccess.GetValueType() == typeof(LocalizedString)) {
                    fieldAccess.SetValue(new L10NString(value));
                } else if (fieldAccess.GetValueType() == typeof(bool)) {
                    fieldAccess.SetValue(value == "true");
                } else if (fieldAccess.GetValueType() == typeof(int)) {
                    fieldAccess.SetValue(int.Parse(value));
                } else if (fieldAccess.GetValueType().IsEnum) {
                    fieldAccess.SetValue(Enum.Parse(fieldAccess.GetValueType(), value));
                } else {
                    fieldAccess.SetValue(value);
                }
            } else {
                var thisField = field.Substring(0, fieldNameEnd);
                var remainingFields = field.Substring(fieldNameEnd + 1);
                var arrayPos = thisField.IndexOf('[');
                if (arrayPos < 0) {
                    var fieldAccess = Harmony12.Traverse.Create(clone).Field(thisField);
                    if (!fieldAccess.FieldExists()) {
                        throw new Exception(
                            $"Field {thisField} does not exist on original of type {clone.GetType().FullName}, available fields: {string.Join(", ", Harmony12.Traverse.Create(clone).Fields())}");
                    }

                    if (fieldAccess.GetValueType().IsArray) {
                        throw new Exception($"Field {thisField} is an array but overall access {field} did not index the array");
                    }

                    fieldAccess.SetValue(TraverseCloneAndSetField(fieldAccess.GetValue(), remainingFields, value));
                } else {
                    var index = int.Parse(new string(thisField.Skip(arrayPos + 1).TakeWhile(char.IsDigit).ToArray()));
                    thisField = field.Substring(0, arrayPos);
                    var fieldAccess = Harmony12.Traverse.Create(clone).Field(thisField);
                    if (!fieldAccess.FieldExists()) {
                        throw new Exception(
                            $"Field {thisField} does not exist on original of type {clone.GetType().FullName}, available fields: {string.Join(", ", Harmony12.Traverse.Create(clone).Fields())}");
                    }

                    if (!fieldAccess.GetValueType().IsArray) {
                        throw new Exception(
                            $"Field {thisField} is of type {fieldAccess.GetValueType().FullName} but overall access {field} used an array index");
                    }

                    // TODO if I use fieldAccess.GetValue<object[]>().ToArray() to make this universally applicable, the SetValue fails saying it can't
                    // convert object[] to e.g. BlueprintComponent[].  Hard-code to only support BlueprintComponent for array for now.
                    if (fieldAccess.GetValueType() == typeof(BlueprintComponent[])) {
                        var arrayClone = fieldAccess.GetValue<BlueprintComponent[]>().ToArray();
                        arrayClone[index] = TraverseCloneAndSetField(arrayClone[index], remainingFields, value);
                        fieldAccess.SetValue(arrayClone);
                    } else if (fieldAccess.GetValueType() == typeof(Condition[])) {
                        var arrayClone = fieldAccess.GetValue<Condition[]>().ToArray();
                        arrayClone[index] = TraverseCloneAndSetField(arrayClone[index], remainingFields, value);
                        fieldAccess.SetValue(arrayClone);
                    } else {
                        throw new Exception(
                            $"Field {thisField} is of unsupported array type {fieldAccess.GetValueType().FullName} ({field})");
                    }
                }
            }

            return clone;
        }

        public void EnsureComponentNameUnique(BlueprintComponent component, BlueprintComponent[] existing) {
            // According to Elmindra, components which are serialized need to have unique names in their array
            var name = component.name;
            var suffix = 0;
            while (existing.Any(blueprint => blueprint.name == name)) {
                suffix++;
                name = $"{component.name}_{suffix}";
            }
            component.name = name;
        }

        private string ApplyItemEnchantmentBlueprintPatch(BlueprintScriptableObject blueprint, Match match) {
            var values = new List<string>();
            // Ensure Components array is not shared with base blueprint
            var componentsCopy = blueprint.ComponentsArray.ToArray();
            var indexCaptures = match.Groups["index"].Captures;
            var fieldCaptures = match.Groups["field"].Captures;
            var valueCaptures = match.Groups["value"].Captures;
            for (var index = 0; index < indexCaptures.Count; ++index) {
                var componentIndex = int.Parse(indexCaptures[index].Value);
                var field = fieldCaptures[index].Value;
                var value = valueCaptures[index].Value;
                values.Add(indexCaptures[index].Value);
                values.Add(field);
                values.Add(value);
                if (componentIndex >= componentsCopy.Length) {
                    var component = TraverseCloneAndSetField<BlueprintComponent>(null, field, value);
                    EnsureComponentNameUnique(component, componentsCopy);
                    componentsCopy = componentsCopy.Concat(new[] {component}).ToArray();
                } else {
                    componentsCopy[componentIndex] = TraverseCloneAndSetField(componentsCopy[componentIndex], field, value);
                }
            }

            blueprint.ComponentsArray = componentsCopy;
            var enchantment = blueprint as BlueprintItemEnchantment;
            var feature = blueprint as BlueprintFeature;
            string nameId = null;
            if (match.Groups["nameId"].Success) {
                nameId = match.Groups["nameId"].Value;
                if (enchantment != null) {
                    accessors.SetBlueprintItemEnchantmentEnchantName(enchantment, new L10NString(nameId));
                } else if (feature != null) {
                    accessors.SetBlueprintUnitFactDisplayName(feature, new L10NString(nameId));
                }
            }

            string descriptionId = null;
            if (match.Groups["descriptionId"].Success) {
                descriptionId = match.Groups["descriptionId"].Value;
                if (enchantment != null) {
                    accessors.SetBlueprintItemEnchantmentDescription(enchantment, new L10NString(descriptionId));
                } else if (feature != null) {
                    accessors.SetBlueprintUnitFactDescription(feature, new L10NString(descriptionId));
                }
            }

            return BuildCustomComponentsItemGuid(blueprint.AssetGuid, values.ToArray(), nameId, descriptionId);
        }

        // Make our mod-specific updates to the blueprint based on the data stored in assetId.  Return a string which
        // is the AssetGuid of the supplied blueprint plus our customization again, or null if we couldn't change the
        // blueprint.
        private string ApplyBlueprintPatch(BlueprintScriptableObject blueprint, Match match) {
            switch (blueprint) {
                case BlueprintBuff buff when match.Groups["timer"].Success:
                    return ApplyTimerBlueprintPatch(buff);
                case BlueprintBuff buff when match.Groups["bondedItem"].Success:
                    return ApplyBondedItemBlueprintPatch(buff);
                case BlueprintFeature feature when match.Groups["feat"].Success:
                    return ApplyFeatBlueprintPatch(feature, match);
                case BlueprintItemEquipment equipment when match.Groups["enchantments"].Success:
                    return ApplyRecipeItemBlueprintPatch(equipment, match);
                case BlueprintItemEquipmentUsable usable when match.Groups["casterLevel"].Success:
                    return ApplySpellItemBlueprintPatch(usable, match);
                case BlueprintItemEnchantment enchantment when match.Groups["components"].Success:
                    return ApplyItemEnchantmentBlueprintPatch(enchantment, match);
                case BlueprintFeature feature when match.Groups["components"].Success:
                    return ApplyItemEnchantmentBlueprintPatch(feature, match);
                default: {
                    throw new Exception($"Match of assetId {match.Value} didn't match blueprint type {blueprint.GetType()}");
                }
            }
        }
    }
}