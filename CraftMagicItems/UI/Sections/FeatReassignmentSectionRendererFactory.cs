using System;

namespace CraftMagicItems.UI.Sections
{
    /// <summary>Factory for <see cref="IFeatReassignmentSectionRenderer" /></summary>
    public static class FeatReassignmentSectionRendererFactory
    {
        private static Func<IFeatReassignmentSectionRenderer> construction;

        /// <summary>Static constructor</summary>
        static FeatReassignmentSectionRendererFactory()
        {
            Reset();
        }

        /// <summary>Resets the constructed instance to <see cref="FeatReassignmentSectionRenderer" /></summary>
        public static void Reset()
        {
            SetConstructor(() => { return new FeatReassignmentSectionRenderer(); });
        }

        /// <summary>Sets the constructed instance to <paramref name="constructor" /></summary>
        public static void SetConstructor(Func<IFeatReassignmentSectionRenderer> constructor)
        {
            construction = constructor;
        }

        /// <summary>Constructs an instance of <see cref="IFeatReassignmentSectionRenderer" /></summary>
        /// <returns>An instance of <see cref="IFeatReassignmentSectionRenderer" /></returns>
        public static IFeatReassignmentSectionRenderer GetFeatReassignmentSectionRenderer()
        {
            return construction();
        }
    }
}