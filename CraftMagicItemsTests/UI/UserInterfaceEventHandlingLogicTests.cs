using System;
using CraftMagicItems;
using CraftMagicItems.UI;
using CraftMagicItems.UI.Sections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CraftMagicItemsTests.UI
{
    /// <summary>Test class for <see cref="UserInterfaceEventHandlingLogic" /></summary>
    public class UserInterfaceEventHandlingLogicTests
    {
        /// <summary>Test class for <see cref="UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings" /></summary>
        [TestClass]
        public class RenderCheatsSectionAndUpdateSettingsTests
        {
            [TestMethod]
            public void Reads_CraftingCostsNoGold()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { CraftingCostsNoGold = false };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, settings.CraftingCostsNoGold);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { CraftingCostsNoGold = true };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, settings.CraftingCostsNoGold);
            }

            [TestMethod]
            public void DoesNotRead_CraftingPriceScale_When_CraftingCostsNoGold()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };
                int initial = -10000000;

                //control
                Settings settings = new Settings
                {
                    CraftingPriceScale = initial,
                };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(initial, settings.CraftingPriceScale);
            }

            [TestMethod]
            public void DoesNotInvoke_WarningAboutCustomItemVanillaItemCostDisparity_When_CraftingCostsNoGold()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                //control
                bool invokedWarning = false;
                Action setInvoked = () => { invokedWarning = true; };

                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(true);
                renderer.Setup(r => r.RenderOnly_WarningAboutCustomItemVanillaItemCostDisparity()).Callback(setInvoked);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, new Settings(), priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, invokedWarning);
            }

            [TestMethod]
            public void Reads_CraftingCostSelection()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                //control
                bool invokedWarning = false;
                Action setInvoked = () => { invokedWarning = true; };

                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CraftingCostSelection(It.IsAny<string>(), It.IsAny<string[]>())).Callback(setInvoked);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, new Settings(), priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, invokedWarning);
            }

            [TestMethod]
            public void Reads_CustomCraftingCostSlider()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                //control
                Settings settings = new Settings { CraftingPriceScale = -4 };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CraftingCostSelection(It.IsAny<string>(), It.IsAny<string[]>())).Returns(2);
                renderer.Setup(r => r.Evaluate_CustomCraftingCostSlider(It.IsAny<float>())).Returns(4000);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(4000, settings.CraftingPriceScale);
            }

            [TestMethod]
            public void Reads_CraftingPriceScale_From_CraftingCostSelection()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { CraftingPriceScale = -4 };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CraftingCostSelection(It.IsAny<string>(), It.IsAny<string[]>())).Returns(1);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(2, settings.CraftingPriceScale);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { CraftingPriceScale = -4 };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CraftingCostSelection(It.IsAny<string>(), It.IsAny<string[]>())).Returns(0);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(1, settings.CraftingPriceScale);
            }

            [TestMethod]
            public void Invokes_WarningAboutCustomItemVanillaItemCostDisparity_When_CraftingCostsGold()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                //control
                bool invokedWarning = false;
                Action setInvoked = () => { invokedWarning = true; };

                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingCostsNoGold(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CraftingCostSelection(It.IsAny<string>(), It.IsAny<string[]>())).Returns(-1);
                renderer.Setup(r => r.RenderOnly_WarningAboutCustomItemVanillaItemCostDisparity()).Callback(setInvoked);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, new Settings(), priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, invokedWarning);
            }

            [TestMethod]
            public void Reads_IgnoreCraftingFeats()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { IgnoreCraftingFeats = false };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_IgnoreCraftingFeats(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, settings.IgnoreCraftingFeats);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { IgnoreCraftingFeats = true };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_IgnoreCraftingFeats(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, settings.IgnoreCraftingFeats);
            }

            [TestMethod]
            public void Reads_CraftingTakesNoTime()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { CraftingTakesNoTime = false };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, settings.CraftingTakesNoTime);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { CraftingTakesNoTime = true };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, settings.CraftingTakesNoTime);
            }

            [TestMethod]
            public void DoesNotRead_CustomCraftRate_When_CraftingTakesNoTime()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };
                int initialMagicCraftRate = -10000000;
                int initialMundaneCraftRate = 9348458;
                bool initialCustomCraftRate = true;

                //control
                Settings settings = new Settings
                {
                    MagicCraftingRate = initialMagicCraftRate,
                    MundaneCraftingRate = initialMundaneCraftRate,
                    CustomCraftRate = initialCustomCraftRate,
                };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(initialMagicCraftRate, settings.MagicCraftingRate);
                Assert.AreEqual(initialMundaneCraftRate, settings.MundaneCraftingRate);
                Assert.AreEqual(initialCustomCraftRate, settings.CustomCraftRate);
            }

            [TestMethod]
            public void Reads_CustomCraftRate()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { CustomCraftRate = false };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CustomCraftRate(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, settings.CustomCraftRate);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { CustomCraftRate = true };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CustomCraftRate(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, settings.CustomCraftRate);
            }

            [TestMethod]
            public void Reads_MagicCraftingRate()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                //control
                Settings settings = new Settings { MagicCraftingRate = -8 };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CustomCraftRate(It.IsAny<bool>())).Returns(true);
                renderer.Setup(r => r.Evaluate_MagicCraftingRateSlider(It.IsAny<int>())).Returns(7);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(7, settings.MagicCraftingRate);
            }

            [TestMethod]
            public void Reads_MundaneCraftingRate()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                //control
                Settings settings = new Settings { MundaneCraftingRate = -23412345 };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CustomCraftRate(It.IsAny<bool>())).Returns(true);
                renderer.Setup(r => r.Evaluate_MundaneCraftingRateSlider(It.IsAny<int>())).Returns(12);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(12, settings.MundaneCraftingRate);
            }

            [TestMethod]
            public void Defaults_MagicCraftingRate()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                //control
                Settings settings = new Settings { MagicCraftingRate = -8 };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CustomCraftRate(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(Settings.MagicCraftingProgressPerDay, settings.MagicCraftingRate);
            }

            [TestMethod]
            public void Defaults_MundaneCraftingRate()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                //control
                Settings settings = new Settings { MundaneCraftingRate = -23412345 };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftingTakesNoTime(It.IsAny<bool>())).Returns(false);
                renderer.Setup(r => r.Evaluate_CustomCraftRate(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(Settings.MundaneCraftingProgressPerDay, settings.MundaneCraftingRate);
            }

            [TestMethod]
            public void Reads_CasterLevelIsSinglePrerequisite()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { CasterLevelIsSinglePrerequisite = false };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CasterLevelIsSinglePrerequisite(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, settings.CasterLevelIsSinglePrerequisite);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { CasterLevelIsSinglePrerequisite = true };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CasterLevelIsSinglePrerequisite(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, settings.CasterLevelIsSinglePrerequisite);
            }

            [TestMethod]
            public void Reads_CraftAtFullSpeedWhileAdventuring()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { CraftAtFullSpeedWhileAdventuring = false };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftAtFullSpeedWhileAdventuring(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, settings.CraftAtFullSpeedWhileAdventuring);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { CraftAtFullSpeedWhileAdventuring = true };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_CraftAtFullSpeedWhileAdventuring(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, settings.CraftAtFullSpeedWhileAdventuring);
            }

            [TestMethod]
            public void Reads_IgnorePlusTenItemMaximum()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { IgnorePlusTenItemMaximum = false };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_IgnorePlusTenItemMaximum(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, settings.IgnorePlusTenItemMaximum);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { IgnorePlusTenItemMaximum = true };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_IgnorePlusTenItemMaximum(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, settings.IgnorePlusTenItemMaximum);
            }

            [TestMethod]
            public void Reads_IgnoreFeatCasterLevelRestriction()
            {
                string priceLabel = "irrelevant";
                string[] priceOptions = new[] { String.Empty, String.Empty, String.Empty };

                /************
                *   Test 1  *
                ************/
                //control
                Settings settings = new Settings { IgnoreFeatCasterLevelRestriction = false };
                Mock<ICheatSectionRenderer> renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_IgnoreFeatCasterLevelRestriction(It.IsAny<bool>())).Returns(true);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(true, settings.IgnoreFeatCasterLevelRestriction);

                /************
                *   Test 2  *
                ************/
                //control
                settings = new Settings { IgnoreFeatCasterLevelRestriction = true };
                renderer = new Mock<ICheatSectionRenderer>();
                renderer.Setup(r => r.Evaluate_IgnoreFeatCasterLevelRestriction(It.IsAny<bool>())).Returns(false);

                //invocation
                UserInterfaceEventHandlingLogic.RenderCheatsSectionAndUpdateSettings(renderer.Object, settings, priceLabel, priceOptions);

                //validation
                Assert.AreEqual(false, settings.IgnoreFeatCasterLevelRestriction);
            }
        }
    }
}