namespace CraftMagicItems.Patches
{
    /// <summary>Structure defining visual X/Y/Z-axis adjustments to weapons to patch the existing game</summary>
    public struct IkPatch
    {
        /// <summary>Constructor</summary>
        /// <param name="uuid">Unique ID of the blueprint to patch</param>
        /// <param name="x">X-axis adjustment</param>
        /// <param name="y">Y-axis adjustment</param>
        /// <param name="z">Z-axis adjustment</param>
        public IkPatch(string uuid, float x, float y, float z)
        {
            BlueprintId = uuid;
            X = x;
            Y = y;
            Z = z;
        }

        public string BlueprintId;
        
        public float X, Y, Z;
    }
}
