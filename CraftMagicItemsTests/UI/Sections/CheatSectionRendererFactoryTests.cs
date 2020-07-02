using System;
using CraftMagicItems.UI.Sections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CraftMagicItemsTests.UI.Sections
{
    /// <summary>Test class for <see cref="CheatSectionRendererFactory" /></summary>
    [TestClass]
    public class CheatSectionRendererFactoryTests
    {
        [TestMethod]
        public void CheatSectionRendererFactory_Defaults_CheatSectionRenderer()
        {
            //control
            CheatSectionRendererFactory.Reset();

            //invocation
            var instance = CheatSectionRendererFactory.GetCheatSectionRenderer();

            //validation
            Assert.AreEqual(typeof(CheatSectionRenderer), instance.GetType(), $"Expected an instance of {nameof(CheatSectionRenderer)} to be returned.");
        }

        [TestMethod]
        public void SetConstructor_Works()
        {
            //control
            Func<ICheatSectionRenderer> mockConstructor = () => { return new Mock<ICheatSectionRenderer>().Object; };
            CheatSectionRendererFactory.SetConstructor(mockConstructor);

            //invocation
            var instance = CheatSectionRendererFactory.GetCheatSectionRenderer();

            //validation
            Assert.AreNotEqual(typeof(CheatSectionRenderer), instance.GetType(), $"Expected an instance of {nameof(CheatSectionRenderer)} to be returned.");
        }

        [TestMethod]
        public void GetCheatSectionRenderer_Works()
        {
            //invocation
            var instance = CheatSectionRendererFactory.GetCheatSectionRenderer();

            //validation
            Assert.IsNotNull(instance, $"Expected an instance of {nameof(ICheatSectionRenderer)} to be returned.");
        }

        [TestMethod]
        public void Reset_Works()
        {
            //control
            Func<ICheatSectionRenderer> mockConstructor = () => { return new Mock<ICheatSectionRenderer>().Object; };
            CheatSectionRendererFactory.SetConstructor(mockConstructor);
            CheatSectionRendererFactory.Reset();

            //invocation
            var instance = CheatSectionRendererFactory.GetCheatSectionRenderer();

            //validation
            Assert.AreEqual(typeof(CheatSectionRenderer), instance.GetType(), $"Expected an instance of {nameof(CheatSectionRenderer)} to be returned.");
        }
    }
}