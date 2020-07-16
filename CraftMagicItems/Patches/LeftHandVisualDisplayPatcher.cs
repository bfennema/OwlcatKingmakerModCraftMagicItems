using CraftMagicItems.Constants;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.View.Equipment;
using UnityEngine;

namespace CraftMagicItems.Patches
{
    /// <summary>Class that patches the visual display for left-handed Ik Targets</summary>
    public static class LeftHandVisualDisplayPatcher
    {
        /// <summary>Patches the <see cref="EquipmentOffsets.IkTargetLeftHand" /> for left-handed characters</summary>
        public static void PatchLeftHandedWeaponModels()
        {
            foreach (var patch in VisualAdjustmentPatches.LeftHandedWeaponPatchList)
            {
                var weapon = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponType>(patch.BlueprintId);
                if (weapon != null)
                {
                    var model = weapon.VisualParameters.Model;
                    var equipmentOffsets = model.GetComponent<EquipmentOffsets>();

                    var locator = new GameObject();
                    locator.transform.SetParent(model.transform);
                    locator.transform.localPosition = new Vector3(patch.X, patch.Y, patch.Z);
                    locator.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
                    locator.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                    equipmentOffsets.IkTargetLeftHand = locator.transform;
                }
            }
        }
    }
}