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
        public static bool RenderCheckbox(bool value, string label)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"{(value ? "<color=green><b>✔</b></color>" : "<color=red><b>✖</b></color>")} {label}", GUILayout.ExpandWidth(false)))
            {
                value = !value;
            }

            GUILayout.EndHorizontal();

            return value;
        }

        /// <summary>Renders a text box on screen in Unity Mod Manager for the item's customized name</summary>
        /// <param name="defaultValue">Default value for the item's name</param>
        /// <param name="selectedCustomName">Currently-selected custom name</param>
        public static string RenderCustomNameField(string defaultValue, string selectedCustomName)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name: ", GUILayout.ExpandWidth(false));
            if (string.IsNullOrEmpty(selectedCustomName))
            {
                selectedCustomName = defaultValue;
            }

            selectedCustomName = GUILayout.TextField(selectedCustomName, GUILayout.Width(300));
            if (selectedCustomName.Trim().Length == 0)
            {
                selectedCustomName = null;
            }

            GUILayout.EndHorizontal();

            return selectedCustomName;
        }
    }
}