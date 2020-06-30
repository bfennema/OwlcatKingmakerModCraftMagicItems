using System;
using CraftMagicItems.UI.BattleLog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CraftMagicItemsTests.UI.BattleLog
{
    /// <summary>Test class for <see cref="BattleLogFactory" /></summary>
    [TestClass]
    public class BattleLogFactoryTests
    {
        [TestMethod]
        public void BattleLogFactory_Defaults_KingmakerBattleLog()
        {
            //control
            BattleLogFactory.Reset();

            //invocation
            var instance = BattleLogFactory.GetBattleLog();

            //validation
            Assert.AreEqual(typeof(KingmakerBattleLog), instance.GetType(), $"Expected an instance of {nameof(KingmakerBattleLog)} to be returned.");
        }

        [TestMethod]
        public void SetConstructor_Works()
        {
            //control
            Func<IBattleLog> mockConstructor = () => { return new Mock<IBattleLog>().Object; };
            BattleLogFactory.SetConstructor(mockConstructor);

            //invocation
            var instance = BattleLogFactory.GetBattleLog();

            //validation
            Assert.AreNotEqual(typeof(KingmakerBattleLog), instance.GetType(), $"Expected an instance of {nameof(KingmakerBattleLog)} to be returned.");
        }

        [TestMethod]
        public void GetBattleLog_Works()
        {
            //invocation
            var instance = BattleLogFactory.GetBattleLog();

            //validation
            Assert.IsNotNull(instance, $"Expected an instance of {nameof(IBattleLog)} to be returned.");
        }

        [TestMethod]
        public void Reset_Works()
        {
            //control
            Func<IBattleLog> mockConstructor = () => { return new Mock<IBattleLog>().Object; };
            BattleLogFactory.SetConstructor(mockConstructor);
            BattleLogFactory.Reset();

            //invocation
            var instance = BattleLogFactory.GetBattleLog();

            //validation
            Assert.AreEqual(typeof(KingmakerBattleLog), instance.GetType(), $"Expected an instance of {nameof(KingmakerBattleLog)} to be returned.");
        }
    }
}