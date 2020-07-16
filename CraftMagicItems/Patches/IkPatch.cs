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
            m_uuid = uuid;
            m_x = x;
            m_y = y;
            m_z = z;
        }

        public string m_uuid;
        
        public float m_x, m_y, m_z;
    }
}
