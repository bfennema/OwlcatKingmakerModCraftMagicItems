using System;

namespace CraftMagicItems.UI.BattleLog
{
    /// <summary>Factory for <see cref="IBattleLog" /></summary>
    public static class BattleLogFactory
    {
        private static Func<IBattleLog> construction;

        /// <summary>Static constructor</summary>
        static BattleLogFactory()
        {
            Reset();
        }

        /// <summary>Resets the constructed instance to <see cref="KingmakerBattleLog" /></summary>
        public static void Reset()
        {
            SetConstructor(() => { return new KingmakerBattleLog(); });
        }

        /// <summary>Sets the constructed instance to <paramref name="constructor" /></summary>
        public static void SetConstructor(Func<IBattleLog> constructor)
        {
            construction = constructor;
        }

        /// <summary>Constructs an instance of <see cref="IBattleLog" /></summary>
        /// <returns>An instance of <see cref="IBattleLog" /></returns>
        public static IBattleLog GetBattleLog()
        {
            return construction();
        }
    }
}