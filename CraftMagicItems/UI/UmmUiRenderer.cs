using UnityEngine;

namespace CraftMagicItems.UI
{
    /// <summary>Class that handles the Unity Mod Manager UI rendering</summary>
    public class UmmUiRenderer
    {
        /// <summary>Renders a checkbox in Unity Mod Manager</summary>
        /// <param name="label">Label for the checkbox</param>
        /// <param name="value">Currently selected value</param>
        /// <returns>The inverse of <paramref name="value" /> when the button is clicked, otherwise <paramref name="value" /></returns>
        public static bool RenderCheckbox(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            var text = value ? "✔" : "✖";
            var color = value ? "green" : "red";
            if (GUILayout.Button($"<color={color}><b>{text}</b></color> {label}", GUILayout.ExpandWidth(false)))
            {
                value = !value;
            }

            GUILayout.EndHorizontal();

            return value;
        }

        /// <summary>Renders a text box on screen in Unity Mod Manager for the item's customized name</summary>
        /// <param name="defaultValue">Default value for the item's name</param>
        /// <param name="selectedCustomName">Currently-selected custom name</param>
        /// <returns>The updated text as entered by user, otherwise <paramref name="selectedCustomName" /></returns>
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

        /// <summary>Renders an integer selection slider</summary>
        /// <param name="label">Label for the slider</param>
        /// <param name="value">Initial value</param>
        /// <param name="min">Minimum possible value</param>
        /// <param name="max">Maximum possible value</param>
        /// <returns>Returns the value selected by the user, clamped and rounded after rendering controls to the screen</returns>
        public static int RenderIntSlider(string label, int value, int min, int max)
        {
            value = Mathf.Clamp(value, min, max);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(300)));
            GUILayout.Label($"{value}", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            return value;
        }

        /// <summary>Renders a toggle-able section selection in Unity Mod Manager for the user to show/hide</summary>
        /// <param name="label">Label for the toggle</param>
        /// <param name="value">Flag indicating whether the toggle is active</param>
        /// <returns>Whether the toggle is currently active in Unity Mod Manager</returns>
        public static bool RenderToggleSection(string label, bool value)
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            bool toggledOn = GUILayout.Toggle(value, " <size=16><b>" + label + "</b></size>");

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return toggledOn;
        }

        /// <summary>Renders a Label control as its own line in Unity Mod Manager</summary>
        /// <param name="label">Text to be displayed</param>
        public static void RenderLabel(string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            GUILayout.EndHorizontal();
        }
    }
}