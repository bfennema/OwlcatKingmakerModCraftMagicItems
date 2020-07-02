namespace CraftMagicItems.Constants
{
    /// <summary>Class containing constants for identifying blueprint unique identifiers for item enchantments</summary>
    public static class EnchantmentBlueprints
    {
        /// <summary>Unique identifier for the Longshank-bane weapon enchantment</summary>
        public const string LongshankBaneGuid = "92a1f5db1a03c5b468828c25dd375806";

        /// <summary>Collection of unique identifiers for basic(?) enchantments</summary>
        /// <remarks>
        ///     Array is broken up into sets of 2, the first of each set is the "source" and the second a "destination"
        ///     
        ///     It looks like the first one is the "weapon" enhancement, and the second is the "unarmed" equivalent. Must be for the Anulet of Mighty Fists?
        ///     
        ///     TODO: flip this over into a Struct of Weapon/Amulet so that what is happening is clearer.
        /// </remarks>
        public static readonly string[] ItemEnchantmentGuids =
        {
            "d42fc23b92c640846ac137dc26e000d4", "da7d830b3f75749458c2e51524805560", // Enchantment +1
            "eb2faccc4c9487d43b3575d7e77ff3f5", "49f9befa0e77cd5428ca3b28fd66a54e", // Enchantment +2
            "80bb8a737579e35498177e1e3c75899b", "bae627dfb77c2b048900f154719ca07b", // Enchantment +3
            "783d7d496da6ac44f9511011fc5f1979", "a4016a5d78384a94581497d0d135d98b", // Enchantment +4
            "bdba267e951851449af552aa9f9e3992", "c3ad7f708c573b24082dde91b081ca5f", // Enchantment +5
            "a36ad92c51789b44fa8a1c5c116a1328", "90316f5801dbe4748a66816a7c00380c", // Agile
        };
    }
}