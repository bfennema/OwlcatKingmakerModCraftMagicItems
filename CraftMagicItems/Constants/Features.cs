using Kingmaker.Blueprints.Classes;

namespace CraftMagicItems.Constants
{
    /// <summary>Class containing constants for abilities and Feats</summary>
    public static class Features
    {
        /// <summary>Collection of <see cref="FeatureGroup" /> that crafting feats can be added to</summary>
        public static readonly FeatureGroup[] CraftingFeatGroups = { FeatureGroup.Feat, FeatureGroup.WizardFeat };

        /// <summary>Blueprint unique identifier for the items that are "martial" weapons</summary>
        /// <remarks>Used to determine which mundane items are martial under special conditions</remarks>
        public const string MartialWeaponProficiencies = "203992ef5b35c864390b4e4a1e200629";

        /// <summary>Blueprint unique identifier for the Channel Energy ability applied to a character</summary>
        public const string ChannelEnergyFeatureGuid = "a79013ff4bcd4864cb669622a29ddafb";

        /// <summary>Blueprint unique identifier for the Shield Master feat</summary>
        public const string ShieldMasterGuid = "dbec636d84482944f87435bd31522fcc";

        /// <summary>Blueprint unique identifier for the Prodigious Two-Weapon Fighting feat</summary>
        public const string ProdigiousTwoWeaponFightingGuid = "ddba046d03074037be18ad33ea462028";

        /// <summary>Blueprint unique identifiers for applied class features that have bonded item features added by the mod</summary>
        public static readonly string[] BondedItemFeatures = {
            "2fb5e65bd57caa943b45ee32d825e9b9",
            "aa34ca4f3cd5e5d49b2475fcfdf56b24"
        };
    }
}