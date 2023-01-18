using UnityEngine.EventSystems;
using UnityEngine;
using HarmonyLib;
using UnityEngine.XR;

namespace SN1MC.Controls
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    class FpsInputPatches : PointerInputModule
    {
        public override void Process()
        {
        }
		

    }

    // Note: Not sure if this is actually needed or not
    // TODO: Check if we can remove this
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.GetCursorScreenPosition))]
    class RaycastPointerPosition {
        public static void Postfix(ref Vector2 __result) {
            if (!XRSettings.enabled || !VRCustomOptionsMenu.EnableMotionControls)
            {
                return;
            }
            if (VRCameraRig.instance == null || VRCameraRig.instance.EventCamera == null) {
                return;
            }
            var eventCamera = VRCameraRig.instance.EventCamera;
            __result = new Vector2(eventCamera.pixelWidth / 2, eventCamera.pixelHeight / 2);
        }
    }

}

