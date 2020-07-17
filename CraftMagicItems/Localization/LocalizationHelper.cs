using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Log;

namespace CraftMagicItems.Localization
{
    /// <summary>Class containing methods used for formatting localized strings</summary>
    public static class LocalizationHelper
    {
        /// <summary>Formats the localized string specified in <paramref name="key" /></summary>
        /// <param name="sourceUnit"><see cref="UnitEntityData" /> specifying what is taking action (such as a crafter)</param>
        /// <param name="key">Key used to look up a localized string</param>
        /// <param name="args">Collection of arguments used in <see cref="string.Format" /></param>
        /// <returns>A <see cref="string" /> representation of the localized string</returns>
        public static string FormatLocalizedString(UnitEntityData sourceUnit, string key, params object[] args)
        {
            // Set GameLogContext so the caster will be used when generating localized strings.
            GameLogContext.SourceUnit = sourceUnit;
            var template = new L10NString(key);
            var result = string.Format(template.ToString(), args);
            GameLogContext.Clear();
            return result;
        }

        /// <summary>Adds the currently selected caster to the localized string specified in <paramref name="key" /></summary>
        /// <param name="key">Key used to look up a localized string</param>
        /// <param name="args">Arguments used in <see cref="string.Format" /></param>
        /// <returns>A <see cref="string" /> representation of the localized string</returns>
        public static string FormatLocalizedString(string key, params object[] args)
        {
            return FormatLocalizedString(Main.Selections.CurrentCaster ?? Main.GetSelectedCrafter(false), key, args);
        }
    }
}