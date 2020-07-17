using System.Collections.Generic;
using CraftMagicItems.UI;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;

namespace CraftMagicItems.Config
{
    /// <summary>Class containing the selections from the UI</summary>
    public class Selections
    {
        /// <summary>Currently selected index for a given setting</summary>
        /// <remarks>
        ///     TODO: This should probably be broken up for ease of maintenance
        /// </remarks>
        public readonly Dictionary<string, int> SelectedIndex = new Dictionary<string, int>();

        /// <summary>
        ///     The currently-selected caster level with which to create an item (wand, scroll, potion, item effect, etc.)
        ///     i.e.: "cast {spell} as an Nth level caster"
        /// </summary>
        public int SelectedCasterLevel;

        /// <summary>Flag indicating whether to ONLY display currently prepared/available spells</summary>
        public bool SelectedShowPreparedSpells;

        /// <summary>Is a double weapon's second side selected?</summary>
        public bool SelectedDoubleWeaponSecondEnd;

        /// <summary>Is a shield's weapon (bash, spikes, etc.) seleted?</summary>
        public bool SelectedShieldWeapon;

        /// <summary>Selected times that an item can cast a given spell per day</summary>
        public int SelectedCastsPerDay;

        /// <summary>Currently selected base item blueprint</summary>
        public BlueprintItemEquipment SelectedBaseBlueprint;

        /// <summary>User-entered custom name for the item</summary>
        public string SelectedCustomName;

        /// <summary>Has the user selected to bond with a new item</summary>
        public bool SelectedBondWithNewObject;

        /// <summary>Currently selected crafter/caster</summary>
        public UnitEntityData CurrentCaster;

        /// <summary>Currently-selected crafting section to render</summary>
        public OpenSection CurrentSection = OpenSection.CraftMagicItemsSection;

        /// <summary>Blueprint that is currently being upgraded</summary>
        public BlueprintItem UpgradingBlueprint;


        /// <summary>Retrieves the select index in <see cref="SelectedIndex" /> matching the key of <paramref name="label" /></summary>
        /// <param name="label">Label used as a key to search on</param>
        /// <returns>The selected index value, or 0 if <paramref name="label" /> cannot be found</returns>
        public int GetSelectionIndex(string label)
        {
            return SelectedIndex.ContainsKey(label) ? SelectedIndex[label] : 0;
        }

        /// <summary>Sets the <see cref="SelectedIndex" /> matching the key of <paramref name="label" /></summary>
        /// <param name="label">Label used as a key to search on</param>
        /// <param name="value">Value to assign</param>
        public void SetSelectionIndex(string label, int value)
        {
            SelectedIndex[label] = value;
        }
    }
}