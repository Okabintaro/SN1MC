using UnityEngine.EventSystems;
using UnityEngine;
using HarmonyLib;
using UnityEngine.XR;

namespace SN1MC.Controls
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;

    // Raycast from the middle of the event "camera" on the controller for accurate laserpointing
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.GetCursorScreenPosition))]
    class RaycastPointerPosition {
        public static void Postfix(ref Vector2 __result, FPSInputModule __instance) {
            if (VRCameraRig.instance == null || VRCameraRig.instance.UIControllerCamera == null) {
                return;
            }

            // TODO: Check if this could be better patched in the raycaster canvas
            var eventCamera = VRCameraRig.instance.UIControllerCamera;
            __result = new Vector2(eventCamera.pixelWidth / 2, eventCamera.pixelHeight / 2);
        }
    }

   [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.ShouldStartDrag))]
   // Since we do the dragging/raycasting in worldspace now the drag threshold has to be way lower.
   // Can't change the EventSystem.pixelThreshold because that is only integer.
   class SetDragThresholdHacky {
       public static bool Prefix(ref bool __result, Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold) {
           threshold = 0.07f;
           __result = !useDragThreshold || (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
           return false;
      }
   }

    // Instead of saving the screen space position of the pointer, we save
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.UpdateMouseState))]
    class UseWorldSpacePointerPosition {
        public static void Prefix(FPSInputModule __instance, PointerEventData leftData) {
            leftData.position = __instance.lastRaycastResult.worldPosition;
       }
    }

#if false
    // TODO: Remove/Toggle
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.UpdateMouseState))]
    class DebugPointerEventState : PointerInputModule {
        public static void Postfix(FPSInputModule __instance) {
            var data = __instance.m_MouseState.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData;
            // Mod.logger.LogInfo($"Mouse State: {data.delta}, {data.dragging}, {data.position}, {data.pointerPress}");
            // DebugPanel.Show($"Mouse State: {data.delta}, {data.dragging}, {data.position}, {data.pointerPress}");
            bool shouldStartDrag = FPSInputModule.ShouldStartDrag(data.pressPosition, data.position, (float)EventSystem.current.pixelDragThreshold, data.useDragThreshold);
            // DebugPanel.Show($"IsDragging: {shouldStartDrag}\n IsMoving: {data.IsPointerMoving()} || {data.pointerDrag} == null \n shouldStart: {shouldStartDrag}\n useThreshold: {data.useDragThreshold}, {EventSystem.current.pixelDragThreshold}\n diff: {(data.pressPosition - data.position).sqrMagnitude}");
            DebugPanel.Show($"Position: {data.pressPosition}, {data.position}");
       }

        public override void Process()
        {
        }
    }
#endif

}

