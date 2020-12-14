using System.Collections.Generic;
using System.Linq;
using CraftMagicItems.Constants;
using CraftMagicItems.Patches;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CraftMagicItemsTests.Constants
{
    [TestClass]
    public class VisualAdjustmentPatchesTests
    {
        [TestMethod]
        public void IkPatchList_Exposes_DuelingSword()
        {
            AssertBlueprintInIkPatchCollection("a6f7e3dc443ff114ba68b4648fd33e9f", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        [TestMethod]
        public void IkPatchList_Exposes_Tongi()
        {
            AssertBlueprintInIkPatchCollection("13fa38737d46c9e4abc7f4d74aaa59c3", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        [TestMethod]
        public void IkPatchList_Exposes_Falcata()
        {
            AssertBlueprintInIkPatchCollection("1af5621e2ae551e42bd1dd6744d98639", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        [TestMethod]
        public void IkPatchList_Exposes_Estoc()
        {
            AssertBlueprintInIkPatchCollection("d516765b3c2904e4a939749526a52a9a", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        [TestMethod]
        public void IkPatchList_Exposes_Rapier()
        {
            AssertBlueprintInIkPatchCollection("2ece38f30500f454b8569136221e55b0", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        [TestMethod]
        public void IkPatchList_Exposes_HeavyPick()
        {
            AssertBlueprintInIkPatchCollection("a492410f3d65f744c892faf09daad84a", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        [TestMethod]
        public void IkPatchList_Exposes_Trident()
        {
            AssertBlueprintInIkPatchCollection("6ff66364e0a2c89469c2e52ebb46365e", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        [TestMethod]
        public void IkPatchList_Exposes_HeavyMace()
        {
            AssertBlueprintInIkPatchCollection("d5a167f0f0208dd439ec7481e8989e21", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        [TestMethod]
        public void IkPatchList_Exposes_HeavyFlail()
        {
            AssertBlueprintInIkPatchCollection("8fefb7e0da38b06408f185e29372c703", VisualAdjustmentPatches.LeftHandedWeaponPatchList);
        }

        private void AssertBlueprintInIkPatchCollection(string uuid, IEnumerable<IkPatch> collection)
        {
            IkPatch instance = collection.Single(patch => patch.BlueprintId == uuid);
            Assert.AreNotEqual(default, instance);
        }
    }
}