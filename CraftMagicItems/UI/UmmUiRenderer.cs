using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CraftMagicItems.UI
{
    /// <summary>Class that handles the Unity Mod Manager UI rendering</summary>
    public class UmmUiRenderer
    {
        /// <summary>Renders a checkbox in Unity Mod Manager</summary>
        /// <param name="value">Currently selected value</param>
        /// <param name="label">Label for the checkbox</param>
        public static void RenderCheckbox(ref bool value, string label)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"{(value ? "<color=green><b>✔</b></color>" : "<color=red><b>✖</b></color>")} {label}", GUILayout.ExpandWidth(false)))
            {
                value = !value;
            }

            GUILayout.EndHorizontal();
        }
    }
}