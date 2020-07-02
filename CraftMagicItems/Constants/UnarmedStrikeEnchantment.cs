namespace CraftMagicItems.Constants
{
    /// <summary>Structure defining enchantments that exist for both melee and unarmed strikes</summary>
    public struct UnarmedStrikeEnchantment
    {
        /// <summary>Used for descriptive purposes rather than a comment in code that someone might delete</summary>
        public string Description;

        /// <summary>The unique identifier of the weapon enchantment blueprint to copy data from</summary>
        public string WeaponEnchantmentGuid;

        /// <summary>The unique identifier of the unarmed strike blueprint to copy data into</summary>
        public string UnarmedEnchantmentGuid;
    }
}