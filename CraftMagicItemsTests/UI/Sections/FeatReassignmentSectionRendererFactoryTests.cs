using System;
using CraftMagicItems.UI.Sections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CraftMagicItemsTests.UI.Sections
{
    /// <summary>Test class for <see cref="FeatReassignmentSectionRendererFactory" /></summary>
    [TestClass]
    public class FeatReassignmentSectionRendererFactoryTests
    {
        [TestMethod]
        public void FeatReassignmentSectionRendererFactory_Defaults_FeatReassignmentSectionRenderer()
        {
            //control
            FeatReassignmentSectionRendererFactory.Reset();

            //invocation
            var instance = FeatReassignmentSectionRendererFactory.GetFeatReassignmentSectionRenderer();

            //validation
            Assert.AreEqual(typeof(FeatReassignmentSectionRenderer), instance.GetType(), $"Expected an instance of {nameof(FeatReassignmentSectionRenderer)} to be returned.");
        }

        [TestMethod]
        public void SetConstructor_Works()
        {
            //control
            Func<IFeatReassignmentSectionRenderer> mockConstructor = () => { return new Mock<IFeatReassignmentSectionRenderer>().Object; };
            FeatReassignmentSectionRendererFactory.SetConstructor(mockConstructor);

            //invocation
            var instance = FeatReassignmentSectionRendererFactory.GetFeatReassignmentSectionRenderer();

            //validation
            Assert.AreNotEqual(typeof(FeatReassignmentSectionRenderer), instance.GetType(), $"Expected an instance of {nameof(FeatReassignmentSectionRenderer)} to be returned.");
        }

        [TestMethod]
        public void GetFeatReassignmentSectionRenderer_Works()
        {
            //invocation
            var instance = FeatReassignmentSectionRendererFactory.GetFeatReassignmentSectionRenderer();

            //validation
            Assert.IsNotNull(instance, $"Expected an instance of {nameof(IFeatReassignmentSectionRenderer)} to be returned.");
        }

        [TestMethod]
        public void Reset_Works()
        {
            //control
            Func<IFeatReassignmentSectionRenderer> mockConstructor = () => { return new Mock<IFeatReassignmentSectionRenderer>().Object; };
            FeatReassignmentSectionRendererFactory.SetConstructor(mockConstructor);
            FeatReassignmentSectionRendererFactory.Reset();

            //invocation
            var instance = FeatReassignmentSectionRendererFactory.GetFeatReassignmentSectionRenderer();

            //validation
            Assert.AreEqual(typeof(FeatReassignmentSectionRenderer), instance.GetType(), $"Expected an instance of {nameof(FeatReassignmentSectionRenderer)} to be returned.");
        }
    }
}