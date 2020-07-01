using System;

namespace CraftMagicItems.UI.Sections
{
    /// <summary>Factory for <see cref="ICheatSectionRenderer" /></summary>
    public static class CheatSectionRendererFactory
    {
        private static Func<ICheatSectionRenderer> construction;

        /// <summary>Static constructor</summary>
        static CheatSectionRendererFactory()
        {
            Reset();
        }

        /// <summary>Resets the constructed instance to <see cref="CheatSectionRenderer" /></summary>
        public static void Reset()
        {
            SetConstructor(() => { return new CheatSectionRenderer(); });
        }

        /// <summary>Sets the constructed instance to <paramref name="constructor" /></summary>
        public static void SetConstructor(Func<ICheatSectionRenderer> constructor)
        {
            construction = constructor;
        }

        /// <summary>Constructs an instance of <see cref="ICheatSectionRenderer" /></summary>
        /// <returns>An instance of <see cref="ICheatSectionRenderer" /></returns>
        public static ICheatSectionRenderer GetCheatSectionRenderer()
        {
            return construction();
        }
    }
}