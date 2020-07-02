namespace CraftMagicItems.Constants
{
    /// <summary>Class containing constants for identifying blueprint unique identifiers for item enchantments</summary>
    public static class EnchantmentBlueprints
    {
        /// <summary>Unique identifier for the Longshank-bane weapon enchantment</summary>
        public const string LongshankBaneGuid = "92a1f5db1a03c5b468828c25dd375806";

        /// <summary>Collection of unique identifiers for enchantments applicable to both melee weapons and Amulet of Mighty Fists</summary>
        public static readonly UnarmedStrikeEnchantment[] ItemEnchantmentGuids =
        {
            new UnarmedStrikeEnchantment
            {
                WeaponEnchantmentGuid = "d42fc23b92c640846ac137dc26e000d4",
                UnarmedEnchantmentGuid = "da7d830b3f75749458c2e51524805560",
                Description = "Enchantment +1"
            },
            new UnarmedStrikeEnchantment
            {
                WeaponEnchantmentGuid = "eb2faccc4c9487d43b3575d7e77ff3f5",
                UnarmedEnchantmentGuid = "49f9befa0e77cd5428ca3b28fd66a54e",
                Description = "Enchantment +2"
            },
            new UnarmedStrikeEnchantment
            {
                WeaponEnchantmentGuid = "80bb8a737579e35498177e1e3c75899b",
                UnarmedEnchantmentGuid = "bae627dfb77c2b048900f154719ca07b",
                Description = "Enchantment +3"
            },
            new UnarmedStrikeEnchantment
            {
                WeaponEnchantmentGuid = "783d7d496da6ac44f9511011fc5f1979",
                UnarmedEnchantmentGuid = "a4016a5d78384a94581497d0d135d98b",
                Description = "Enchantment +4"
            },
            new UnarmedStrikeEnchantment
            {
                WeaponEnchantmentGuid = "bdba267e951851449af552aa9f9e3992",
                UnarmedEnchantmentGuid = "c3ad7f708c573b24082dde91b081ca5f",
                Description = "Enchantment +5"
            },
            new UnarmedStrikeEnchantment
            {
                WeaponEnchantmentGuid = "a36ad92c51789b44fa8a1c5c116a1328",
                UnarmedEnchantmentGuid = "90316f5801dbe4748a66816a7c00380c",
                Description = "Agile"
            },
        };
    }
}