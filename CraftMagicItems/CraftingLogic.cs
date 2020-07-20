using System;
using System.Collections.Generic;
using System.Linq;
using CraftMagicItems.Constants;
using CraftMagicItems.Localization;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.Log;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Utility;

namespace CraftMagicItems
{
    /// <summary>Core logic class for crafting items</summary>
    public static class CraftingLogic
    {
        public static void WorkOnProjects(UnitDescriptor caster, bool returningToCapital)
        {
            if (!caster.IsPlayerFaction || caster.State.IsDead || caster.State.IsFinallyDead)
            {
                return;
            }

            Main.Selections.CurrentCaster = caster.Unit;
            var withPlayer = Game.Instance.Player.PartyCharacters.Contains(caster.Unit);
            var playerInCapital = Main.IsPlayerInCapital();
            // Only update characters in the capital when the player is also there.
            if (!withPlayer && !playerInCapital)
            {
                // Character is back in the capital - skipping them for now.
                return;
            }

            var isAdventuring = withPlayer && !Main.IsPlayerSomewhereSafe();
            var timer = Main.GetCraftingTimerComponentForCaster(caster);
            if (timer == null || timer.CraftingProjects.Count == 0)
            {
                // Character is not doing any crafting
                return;
            }

            // Round up the number of days, so caster makes some progress on a new project the first time they rest.
            var interval = Game.Instance.Player.GameTime.Subtract(timer.LastUpdated);
            var daysAvailableToCraft = (int)Math.Ceiling(interval.TotalDays);
            if (daysAvailableToCraft <= 0)
            {
                if (isAdventuring)
                {
                    Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-not-full-day"));
                }

                return;
            }

            // Time passes for this character even if they end up making no progress on their projects.  LastUpdated can go up to
            // a day into the future, due to the rounding up of daysAvailableToCraft.
            timer.LastUpdated += TimeSpan.FromDays(daysAvailableToCraft);
            // Work on projects sequentially, skipping any that can't progress due to missing items, missing prerequisites or having too high a DC.
            foreach (var project in timer.CraftingProjects.ToList())
            {
                if (project.UpgradeItem != null)
                {
                    // Check if the item has been dropped and picked up again, which apparently creates a new object with the same blueprint.
                    if (project.UpgradeItem.Collection != Game.Instance.Player.Inventory && project.UpgradeItem.Collection != Game.Instance.Player.SharedStash)
                    {
                        var itemInStash = Game.Instance.Player.SharedStash.FirstOrDefault(item => item.Blueprint == project.UpgradeItem.Blueprint);
                        if (itemInStash != null)
                        {
                            Main.ItemUpgradeProjects.Remove(project.UpgradeItem);
                            Main.ItemUpgradeProjects[itemInStash] = project;
                            project.UpgradeItem = itemInStash;
                        }
                        else
                        {
                            var itemInInventory = Game.Instance.Player.Inventory.FirstOrDefault(item => item.Blueprint == project.UpgradeItem.Blueprint);
                            if (itemInInventory != null)
                            {
                                Main.ItemUpgradeProjects.Remove(project.UpgradeItem);
                                Main.ItemUpgradeProjects[itemInInventory] = project;
                                project.UpgradeItem = itemInInventory;
                            }
                        }
                    }

                    // Check that the caster can get at the item they're upgrading... it must be in the party inventory, and either un-wielded, or the crafter
                    // must be with the wielder (together in the capital or out in the party together).
                    var wieldedInParty = (project.UpgradeItem.Wielder == null || Game.Instance.Player.PartyCharacters.Contains(project.UpgradeItem.Wielder.Unit));
                    if ((!playerInCapital || returningToCapital)
                        && (project.UpgradeItem.Collection != Game.Instance.Player.SharedStash || withPlayer)
                        && (project.UpgradeItem.Collection != Game.Instance.Player.Inventory || ((!withPlayer || !wieldedInParty) && (withPlayer || wieldedInParty))))
                    {
                        project.AddMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-upgrade-item", project.UpgradeItem.Blueprint.Name));
                        Main.AddBattleLogMessage(project.LastMessage);
                        continue;
                    }
                }

                var craftingData = Main.LoadedData.ItemCraftingData.FirstOrDefault(data => data.Name == project.ItemType);
                StatType craftingSkill;
                int dc;
                int progressRate;

                if (project.ItemType == Main.BondedItemRitual)
                {
                    craftingSkill = StatType.SkillKnowledgeArcana;
                    dc = 10 + project.Crafter.Stats.GetStat(craftingSkill).ModifiedValue;
                    progressRate = Main.ModSettings.MagicCraftingRate;
                }
                else if (Main.IsMundaneCraftingData(craftingData))
                {
                    craftingSkill = StatType.SkillKnowledgeWorld;
                    dc = Main.CalculateMundaneCraftingDC((RecipeBasedItemCraftingData)craftingData, project.ResultItem.Blueprint, caster);
                    progressRate = Main.ModSettings.MundaneCraftingRate;
                }
                else
                {
                    craftingSkill = StatType.SkillKnowledgeArcana;
                    dc = 5 + project.CasterLevel;
                    progressRate = Main.ModSettings.MagicCraftingRate;
                }

                var missing = CheckSpellPrerequisites(project, caster, isAdventuring, out var missingSpells, out var spellsToCast);
                if (missing > 0)
                {
                    var missingSpellNames = missingSpells
                        .Select(ability => ability.Name)
                        .BuildCommaList(project.AnyPrerequisite);

                    if (craftingData.PrerequisitesMandatory || project.PrerequisitesMandatory)
                    {
                        project.AddMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-prerequisite",
                            project.ResultItem.Name, missingSpellNames));
                        Main.AddBattleLogMessage(project.LastMessage);
                        // If the item type has mandatory prerequisites and some are missing, move on to the next project.
                        continue;
                    }

                    Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-spell", missingSpellNames,
                                            DifficultyClass.MissingPrerequisiteDCModifier * missing));
                }
                var missing2 = CheckFeatPrerequisites(project, caster, out var missingFeats);
                if (missing2 > 0)
                {
                    var missingFeatNames = missingFeats.Select(ability => ability.Name).BuildCommaList(project.AnyPrerequisite);
                    Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-feat", missingFeatNames,
                        DifficultyClass.MissingPrerequisiteDCModifier * missing2));
                }
                missing += missing2;
                missing += CheckCrafterPrerequisites(project, caster);
                dc += DifficultyClass.MissingPrerequisiteDCModifier * missing;
                var casterLevel = Main.CharacterCasterLevel(caster);
                if (casterLevel < project.CasterLevel)
                {
                    // Rob's ruling... if you're below the prerequisite caster level, you're considered to be missing a prerequisite for each
                    // level you fall short, unless ModSettings.CasterLevelIsSinglePrerequisite is true.
                    var casterLevelPenalty = Main.ModSettings.CasterLevelIsSinglePrerequisite
                        ? DifficultyClass.MissingPrerequisiteDCModifier
                        : DifficultyClass.MissingPrerequisiteDCModifier * (project.CasterLevel - casterLevel);
                    dc += casterLevelPenalty;
                    Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-low-caster-level", project.CasterLevel, casterLevelPenalty));
                }
                var oppositionSchool = Main.CheckForOppositionSchool(caster, project.SpellPrerequisites);
                if (oppositionSchool != SpellSchool.None)
                {
                    dc += DifficultyClass.OppositionSchoolDCModifier;
                    Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-opposition-school",
                        LocalizedTexts.Instance.SpellSchoolNames.GetText(oppositionSchool), DifficultyClass.OppositionSchoolDCModifier));
                }

                var skillCheck = 10 + caster.Stats.GetStat(craftingSkill).ModifiedValue;
                if (skillCheck < dc)
                {
                    // Can't succeed by taking 10... move on to the next project.
                    project.AddMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-dc-too-high", project.ResultItem.Name,
                        LocalizedTexts.Instance.Stats.GetText(craftingSkill), skillCheck, dc));
                    Main.AddBattleLogMessage(project.LastMessage);
                    continue;
                }

                // Cleared the last hurdle, so caster is going to make progress on this project.
                // You only work at 1/4 speed if you're crafting while adventuring.
                var adventuringPenalty = !isAdventuring || Main.ModSettings.CraftAtFullSpeedWhileAdventuring ? 1 : DifficultyClass.AdventuringProgressPenalty;
                // Each 1 by which the skill check exceeds the DC increases the crafting rate by 20% of the base progressRate
                var progressPerDay = (int)(progressRate * (1 + (float)(skillCheck - dc) / 5) / adventuringPenalty);
                var daysUntilProjectFinished = (int)Math.Ceiling(1.0 * (project.TargetCost - project.Progress) / progressPerDay);
                var daysCrafting = Math.Min(daysUntilProjectFinished, daysAvailableToCraft);
                var progressGold = daysCrafting * progressPerDay;
                foreach (var spell in spellsToCast)
                {
                    if (spell.SourceItem != null)
                    {
                        // Use items whether we're adventuring or not, one charge per day of daysCrafting.  We might run out of charges...
                        if (spell.SourceItem.IsSpendCharges && !((BlueprintItemEquipment)spell.SourceItem.Blueprint).RestoreChargesOnRest)
                        {
                            var itemSpell = spell;
                            for (var day = 0; day < daysCrafting; ++day)
                            {
                                if (itemSpell.SourceItem.Charges <= 0)
                                {
                                    // This item is exhausted and we haven't finished crafting - find another item.
                                    itemSpell = Main.FindCasterSpell(caster, spell.Blueprint, isAdventuring, spellsToCast);
                                }

                                if (itemSpell == null)
                                {
                                    // We've run out of items that can cast the spell...crafting progress is going to slow, if not stop.
                                    progressGold -= progressPerDay * (daysCrafting - day);
                                    skillCheck -= DifficultyClass.MissingPrerequisiteDCModifier;
                                    if (craftingData.PrerequisitesMandatory || project.PrerequisitesMandatory)
                                    {
                                        Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-prerequisite", project.ResultItem.Name, spell.Name));
                                        daysCrafting = day;
                                        break;
                                    }

                                    Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-spell", spell.Name, DifficultyClass.MissingPrerequisiteDCModifier));
                                    if (skillCheck < dc)
                                    {
                                        // Can no longer make progress
                                        Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-dc-too-high", project.ResultItem.Name,
                                            LocalizedTexts.Instance.Stats.GetText(craftingSkill), skillCheck, dc));
                                        daysCrafting = day;
                                    }
                                    else
                                    {
                                        // Progress will be slower, but they don't need to cast this spell any longer.
                                        progressPerDay = (int)(progressRate * (1 + (float)(skillCheck - dc) / 5) / adventuringPenalty);
                                        daysUntilProjectFinished =
                                            day + (int)Math.Ceiling(1.0 * (project.TargetCost - project.Progress - progressGold) / progressPerDay);
                                        daysCrafting = Math.Min(daysUntilProjectFinished, daysAvailableToCraft);
                                        progressGold += (daysCrafting - day) * progressPerDay;
                                    }

                                    break;
                                }

                                GameLogContext.SourceUnit = caster.Unit;
                                GameLogContext.Text = itemSpell.SourceItem.Name;
                                Main.AddBattleLogMessage(LocalizedStringBlueprints.CharacterUsedItemLocalized);
                                GameLogContext.Clear();
                                itemSpell.SourceItem.SpendCharges(caster);
                            }
                        }
                    }
                    else if (isAdventuring)
                    {
                        // Actually cast the spells if we're adventuring.
                        Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-expend-spell", spell.Name));
                        spell.SpendFromSpellbook();
                    }
                }

                var progressKey = project.ItemType == Main.BondedItemRitual
                    ? "craftMagicItems-logMessage-made-progress-bondedItem"
                    : "craftMagicItems-logMessage-made-progress";
                var progress = LocalizationHelper.FormatLocalizedString(progressKey, progressGold, project.TargetCost - project.Progress, project.ResultItem.Name);
                var checkResult = LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-made-progress-check", LocalizedTexts.Instance.Stats.GetText(craftingSkill),
                    skillCheck, dc);
                Main.AddBattleLogMessage(progress, checkResult);
                daysAvailableToCraft -= daysCrafting;
                project.Progress += progressGold;
                if (project.Progress >= project.TargetCost)
                {
                    // Completed the project!
                    if (project.ItemType == Main.BondedItemRitual)
                    {
                        Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-bonding-ritual-complete", project.ResultItem.Name), project.ResultItem);
                        Main.BondWithObject(project.Crafter, project.ResultItem);
                    }
                    else
                    {
                        Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-crafting-complete", project.ResultItem.Name), project.ResultItem);
                        Main.CraftItem(project.ResultItem, project.UpgradeItem);
                    }
                    timer.CraftingProjects.Remove(project);
                    if (project.UpgradeItem == null)
                    {
                        Main.ItemCreationProjects.Remove(project);
                    }
                    else
                    {
                        Main.ItemUpgradeProjects.Remove(project.UpgradeItem);
                    }
                }
                else
                {
                    var completeKey = project.ItemType == Main.BondedItemRitual
                        ? "craftMagicItems-logMessage-made-progress-bonding-ritual-amount-complete"
                        : "craftMagicItems-logMessage-made-progress-amount-complete";
                    var amountComplete = LocalizationHelper.FormatLocalizedString(completeKey, project.ResultItem.Name, 100 * project.Progress / project.TargetCost);
                    Main.AddBattleLogMessage(amountComplete, project.ResultItem);
                    project.AddMessage($"{progress} {checkResult}");
                }

                if (daysAvailableToCraft <= 0)
                {
                    return;
                }
            }

            if (daysAvailableToCraft > 0)
            {
                // They didn't use up all available days - reset the time they can start crafting to now.
                timer.LastUpdated = Game.Instance.Player.GameTime;
            }
        }

        private static int CheckCrafterPrerequisites(CraftingProjectData project, UnitDescriptor caster)
        {
            var missing = Main.GetMissingCrafterPrerequisites(project.CrafterPrerequisites, caster);
            foreach (var prerequisite in missing)
            {
                Main.AddBattleLogMessage(LocalizationHelper.FormatLocalizedString("craftMagicItems-logMessage-missing-crafter-prerequisite",
                    new L10NString($"craftMagicItems-crafter-prerequisite-{prerequisite}"), DifficultyClass.MissingPrerequisiteDCModifier));
            }

            return missing.Count;
        }

        private static int CheckFeatPrerequisites(CraftingProjectData project, UnitDescriptor caster, out List<BlueprintFeature> missingFeats)
        {
            return CheckFeatPrerequisites(project.FeatPrerequisites, project.AnyPrerequisite, caster, out missingFeats);
        }

        public static int CheckFeatPrerequisites(BlueprintFeature[] prerequisites, bool anyPrerequisite, UnitDescriptor caster,
            out List<BlueprintFeature> missingFeats)
        {
            missingFeats = new List<BlueprintFeature>();
            if (prerequisites != null)
            {
                foreach (var featBlueprint in prerequisites)
                {
                    var feat = caster.GetFeature(featBlueprint);
                    if (feat != null)
                    {
                        if (anyPrerequisite)
                        {
                            missingFeats.Clear();
                            return 0;
                        }
                    }
                    else
                    {
                        missingFeats.Add(featBlueprint);
                    }
                }
            }

            return anyPrerequisite ? Math.Min(1, missingFeats.Count) : missingFeats.Count;
        }

        private static int CheckSpellPrerequisites(CraftingProjectData project, UnitDescriptor caster, bool mustPrepare,
            out List<BlueprintAbility> missingSpells, out List<AbilityData> spellsToCast)
        {
            return CheckSpellPrerequisites(project.SpellPrerequisites, project.AnyPrerequisite, caster, mustPrepare, out missingSpells, out spellsToCast);
        }

        public static int CheckSpellPrerequisites(BlueprintAbility[] prerequisites, bool anyPrerequisite, UnitDescriptor caster, bool mustPrepare,
            out List<BlueprintAbility> missingSpells, out List<AbilityData> spellsToCast)
        {
            spellsToCast = new List<AbilityData>();
            missingSpells = new List<BlueprintAbility>();
            if (prerequisites != null)
            {
                foreach (var spellBlueprint in prerequisites)
                {
                    var spell = Main.FindCasterSpell(caster, spellBlueprint, mustPrepare, spellsToCast);
                    if (spell != null)
                    {
                        spellsToCast.Add(spell);
                        if (anyPrerequisite)
                        {
                            missingSpells.Clear();
                            return 0;
                        }
                    }
                    else
                    {
                        missingSpells.Add(spellBlueprint);
                    }
                }
            }

            return anyPrerequisite ? Math.Min(1, missingSpells.Count) : missingSpells.Count;
        }
    }
}