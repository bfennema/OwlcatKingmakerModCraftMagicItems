using UnityEngine;

namespace CraftMagicItems.UI.BattleLog
{
    /// <summary>Interface that defines logging of messages to the Battle Log in Kingmaker</summary>
    /// <remarks>Used for mocking the battle log for tests and such</remarks>
    public interface IBattleLog
    {
        /// <summary>Adds a message to the battle log</summary>
        /// <param name="message">Message to add</param>
        /// <param name="tooltip">Secondary object or <see cref="String" /> for the tooltip to display</param>
        /// <param name="color"><see cref="Color" /> to use to render <paramref name="message" /></param>
        void AddBattleLogMessage(System.String message, System.Object tooltip = null, Color? color = null);
    }
}