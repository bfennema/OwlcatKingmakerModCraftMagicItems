using System.Collections.Generic;
using CraftMagicItems.Patches;

namespace CraftMagicItems.Constants
{
    /// <summary>Constants class containing <see cref="IkPatch" /> definitions</summary>
    public static class VisualAdjustmentPatches
    {
        /// <summary>Collection of <see cref="" /> patches for visual adjustments to displayed items</summary>
        public static IEnumerable<IkPatch> LeftHandedWeaponPatchList
        {
            get
            {
                return new[]
                {
                    DuelingSword,
                    Tongi,
                    Falcata,
                    Estoc,
                    Rapier,
                    HeavyPick,
                    Trident,
                    HeavyMace,
                    HeavyFlail,
                };
            }
        }

        /// <summary>Dueling sword patch</summary>
        private static readonly IkPatch DuelingSword = new IkPatch("a6f7e3dc443ff114ba68b4648fd33e9f", 0.00f, -0.10f, 0.01f);

        /// <summary>Tongi patch</summary>
        private static readonly IkPatch Tongi = new IkPatch("13fa38737d46c9e4abc7f4d74aaa59c3", 0.00f, -0.36f, 0.00f);

        /// <summary>Falcata patch</summary>
        private static readonly IkPatch Falcata = new IkPatch("1af5621e2ae551e42bd1dd6744d98639", 0.00f, -0.07f, 0.00f);

        /// <summary>Estoc patch</summary>
        private static readonly IkPatch Estoc = new IkPatch("d516765b3c2904e4a939749526a52a9a", 0.00f, -0.15f, 0.00f);

        /// <summary>Rapier patch</summary>
        private static readonly IkPatch Rapier = new IkPatch("2ece38f30500f454b8569136221e55b0", 0.00f, -0.08f, 0.00f);

        /// <summary>Heavy pick patch</summary>
        private static readonly IkPatch HeavyPick = new IkPatch("a492410f3d65f744c892faf09daad84a", 0.00f, -0.20f, 0.00f);

        /// <summary>Trident patch</summary>
        private static readonly IkPatch Trident = new IkPatch("6ff66364e0a2c89469c2e52ebb46365e", 0.00f, -0.10f, 0.00f);

        /// <summary>Heavy mace patch</summary>
        private static readonly IkPatch HeavyMace = new IkPatch("d5a167f0f0208dd439ec7481e8989e21", 0.00f, -0.08f, 0.00f);

        /// <summary>Heavy flail patch</summary>
        private static readonly IkPatch HeavyFlail = new IkPatch("8fefb7e0da38b06408f185e29372c703", -0.14f, 0.00f, 0.00f);
    }
}