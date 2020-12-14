using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker;
#if PATCH21
using Kingmaker.Assets.UI.Context;
#endif
using Kingmaker.Localization;
using Newtonsoft.Json;

namespace CraftMagicItems.Localization
{
    public class L10NData
    {
        [JsonProperty] public string Key;
        [JsonProperty] public string Value;
    }

    /// <remarks>Localization is sometimes written in English as l10n, where 10 is the number of letters in the English word between 'l' and 'n'</remarks>
    class L10N
    {
        private static readonly Dictionary<string, string> ModifiedL10NStrings = new Dictionary<string, string>();

        private static bool initialLoad;
        private static bool enabled = true;

        private static void LoadL10NStrings()
        {
            if (LocalizationManager.CurrentPack == null)
            {
                return;
            }
            initialLoad = true;
            var currentLocale = LocalizationManager.CurrentLocale.ToString();
            var fileName = $"{Main.ModEntry.Path}/L10n/Strings_{currentLocale}.json";
            if (!File.Exists(fileName))
            {
                Main.ModEntry.Logger.Warning($"Localised text for current local \"{currentLocale}\" not found, falling back on enGB.");
                currentLocale = "enGB";
                fileName = $"{Main.ModEntry.Path}/L10n/Strings_{currentLocale}.json";
            }

            try
            {
                var allStrings = Main.ReadJsonFile<L10NData[]>(fileName);
                foreach (var data in allStrings)
                {
                    var value = data.Value;
                    if (LocalizationManager.CurrentPack.Strings.ContainsKey(data.Key))
                    {
                        var original = LocalizationManager.CurrentPack.Strings[data.Key];
                        ModifiedL10NStrings.Add(data.Key, original);
                        if (value[0] == '+')
                        {
                            value = original + value.Substring(1);
                        }
                    }
                    LocalizationManager.CurrentPack.Strings[data.Key] = value;
                }
            }
            catch (Exception e)
            {
                Main.ModEntry.Logger.Warning($"Exception loading L10n data for locale {currentLocale}: {e}");
                throw;
            }
        }

        public static void SetEnabled(bool newEnabled)
        {
            if (LocalizationManager.CurrentPack != null)
            {
                if (!initialLoad)
                {
                    LoadL10NStrings();
                }

                if (enabled != newEnabled)
                {
                    enabled = newEnabled;
                    foreach (var key in ModifiedL10NStrings.Keys.ToArray()) {
                        var swap = ModifiedL10NStrings[key];
                        ModifiedL10NStrings[key] = LocalizationManager.CurrentPack.Strings[key];
                        LocalizationManager.CurrentPack.Strings[key] = swap;
                    }
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(LocalizationManager), "CurrentLocale", HarmonyLib.MethodType.Setter)]
        private static class LocalizationManagerCurrentLocaleSetterPatch
        {
            // ReSharper disable once UnusedMember.Local
            private static void Postfix()
            {
                LoadL10NStrings();
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(MainMenu), "Start")]
        private static class MainMenuStartPatch
        {
            private static void Prefix()
            {
                // Kingmaker Mod Loader doesn't appear to patch the game before LocalizationManager.CurrentLocale has been set.
                if (!initialLoad)
                {
                    LoadL10NStrings();
                }
            }
        }

#if PATCH21
        [HarmonyLib.HarmonyPatch(typeof(MainMenuUiContext), "Initialize")]
        private static class MainMenuUiContextInitializePatch
        {
            private static void Prefix()
            {
                // Kingmaker Mod Loader doesn't appear to patch the game before LocalizationManager.CurrentLocale has been set.
                if (!initialLoad)
                {
                    LoadL10NStrings();
                }
            }
        }
#endif
    }
}