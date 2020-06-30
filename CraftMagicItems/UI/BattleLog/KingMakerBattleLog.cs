using Kingmaker;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.UI.Log;
using UnityEngine;

namespace CraftMagicItems.UI.BattleLog
{
    /// <summary>Class that drives logging of messages to the Battle Log in Kingmaker</summary>
    public class KingmakerBattleLog : IBattleLog
    {
        /// <summary>Adds a message to the battle log</summary>
        /// <param name="message">Message to add</param>
        /// <param name="tooltip">Secondary object or <see cref="String" /> for the tooltip to display</param>
        /// <param name="color"><see cref="Color" /> to use to render <paramref name="message" /></param>
        public void AddBattleLogMessage(string message, object tooltip = null, Color? color = null)
        {
            var data = new LogDataManager.LogItemData(message, color ?? GameLogStrings.Instance.DefaultColor, tooltip, PrefixIcon.None);
            if (Game.Instance.UI.BattleLogManager)
            {
                Game.Instance.UI.BattleLogManager.LogView.AddLogEntry(data);
            }
            else
            {
                Main.PendingLogItems.Add(data);
            }
        }
    }
}