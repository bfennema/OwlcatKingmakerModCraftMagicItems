using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using CraftMagicItems.Config;
using CraftMagicItems.Constants;
using CraftMagicItems.Localization;
using CraftMagicItems.Patches;
using CraftMagicItems.UI;
using CraftMagicItems.UI.Sections;
using CraftMagicItems.UI.UnityModManager;
using Kingmaker;
#if PATCH21
using Kingmaker.Assets.UI.Context;
#endif
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.WeaponEnchants;
using Kingmaker.Designers.TempMapCode.Capital;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.GameModes;
using Kingmaker.Items;
#if !PATCH21
using Kingmaker.Items.Slots;
#endif
using Kingmaker.Kingdom;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.ActionBar;
using Kingmaker.UI.Common;
using Kingmaker.UI.Log;
using Kingmaker.UI.Tooltip;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
#if !PATCH21
using Kingmaker.UnitLogic.ActivatableAbilities;
#endif
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View.Equipment;
using Newtonsoft.Json;
using UnityEngine;
using UnityModManagerNet;
using Random = System.Random;

namespace CraftMagicItems.Patches.Harmony
{
    // Add "pending" log items when the battle log becomes available again, so crafting messages sent when e.g. camping
    // in the overland map are still shown eventually.
    [HarmonyLib.HarmonyPatch(typeof(BattleLogManager), "Initialize")]
    // ReSharper disable once UnusedMember.Local
    public static class BattleLogManagerInitializePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix()
        {
            if (Enumerable.Any(Main.PendingLogItems))
            {
                foreach (var item in Main.PendingLogItems)
                {
                    item.UpdateSize();
                    Game.Instance.UI.BattleLogManager.LogView.AddLogEntry(item);
                }

                Main.PendingLogItems.Clear();
            }
        }
    }
}