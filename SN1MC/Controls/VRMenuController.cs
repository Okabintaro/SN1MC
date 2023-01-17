using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using System.Linq;
using System.Collections;
using UWE;

// Setup controller tracking, laserpointer and UI in Main Menu
namespace SN1MC.Controls
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    class VRMenuController : MonoBehaviour
    {
        private static VRMenuController _main;
        public static VRMenuController main
        {
            get
            {
                if (_main == null)
                {
                    _main = new VRMenuController();
                }
                return _main;
            }
        }

        public static GameObject leftController;
        public static GameObject rightController;
        public static GameObject modelL;
        public static GameObject modelR;

        public static LaserPointerNew laserPointer;
        public static Camera mainCamera;

        private void ModifyMainMenuForVr() {
            // Set Main Camera to render everything, including UI
            mainCamera.cullingMask = -1;

            // Turn off UI Camera
            Camera uiCamera = FindObjectsOfType<Camera>().First(c => c.name.Equals("UI Camera"));
            uiCamera.enabled = false;

            // Move Main Menu Canvas in front of player
            Canvas menuCanvas = FindObjectsOfType<Canvas>().First(c => c.name.Equals("Menu canvas"));
            menuCanvas.transform.position = new Vector3(0, 1.75f, 0.75f);

            //var raycaster = menuCanvas.gameObject.GetComponent<uGUI_GraphicRaycaster>();
            //DestroyImmediate(raycaster);
            // TODO: Patch it?
            //raycaster.enabled = false;
            //menuCanvas.gameObject.AddComponent<GraphicRaycaster>();

            // TODO: Increase UI fidelity?
        }

        public void InitializeMenu()
        {
            // TODO: Swap FPSInput with VRInputModule
            VROptions.gazeBasedCursor = true;


            Debug.Log("Creating Controllers and laser pointer");
            // TODO: Reasearch what this does
            GameInput.SetupDefaultControllerBindings();

            // Create Controllers and parent them to world/environment camera
            rightController = new GameObject("rightController");
            leftController = new GameObject("leftController");
            mainCamera = GameObject.FindGameObjectsWithTag("MainCamera").First(c => c.name.Equals("Main Camera")).GetComponent<Camera>();
            var camParent = mainCamera.transform.parent;
            rightController.transform.parent = camParent;
            leftController.transform.parent = camParent;

            // Create and setup laser pointer
            laserPointer = rightController.AddComponent<LaserPointerNew>();
            laserPointer.transform.parent = rightController.transform;
            // TODO: Check if need to reset pos/localPos

            // Create two cubes to show controller positions
            modelR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelR.transform.parent = leftController.transform;
            modelR.transform.localPosition.Set(0, 0, 0);
            modelR.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            modelL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelL.transform.parent = rightController.transform;
            modelL.transform.localPosition.Set(0, 0, 0);
            modelL.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            // TODO: Doesn't seem to work
            // Trying to render controler models
            if (false)
            {
                modelR = new GameObject("ModelR");
                modelL = new GameObject("ModelL");
                modelR.transform.parent = rightController.transform;
                modelL.transform.parent = leftController.transform;
                var rm = modelL.AddComponent<SteamVRRef.Valve.VR.SteamVR_RenderModel>();
                rm.verbose = true;
                rm.updateDynamically = false;
                rm.createComponents = false;
                rm = modelR.AddComponent<SteamVRRef.Valve.VR.SteamVR_RenderModel>();
                rm.verbose = true;
                rm.updateDynamically = false;
                rm.createComponents = false;
            }

            Debug.Log("Setting up canvas camera...");
            CoroutineHost.StartCoroutine(SetupEventCamera());
            ModifyMainMenuForVr();
        }

        private static IEnumerator SetupEventCamera()
        {
            yield return new WaitForSeconds(1.0f);

            // Make the Menu canvas use the world camera
            // TODO: Should not be needed in the future
            Canvas menuCanvas = FindObjectsOfType<Canvas>().First(c => c.name.Equals("Menu canvas"));
            menuCanvas.worldCamera = laserPointer.eventCamera;
            Debug.Log($"Setting up eventcamera for {menuCanvas}, using {laserPointer.eventCamera.name}");

            // Connect Input module and layer pointer together
            FPSInputModule fpsInput = FindObjectOfType<FPSInputModule>();
            VRInputModule vrim = fpsInput.gameObject.AddComponent<VRInputModule>();
            Debug.Log($"Input Module before: {EventSystem.current.currentInputModule.name}");
            vrim.ActivateModule();
            vrim.eventCamera = laserPointer.eventCamera;
            fpsInput.enabled = false;
            Debug.Log($"Input Module after: {EventSystem.current.currentInputModule.name}");
            yield break;
        }

        // TODO: Remove this if VRController works
        public static Vector3 MainMenuRaycast()
        {
            var rayDestination = rightController.transform.position + rightController.transform.forward * FPSInputModule.current.maxInteractionDistance;

            Ray raycast = new Ray(rightController.transform.position, rightController.transform.forward);
            var layerNames = new string[] { "SubRigidbodyExclude", "Interior", "TerrainCollider", "Trigger", "UI", "Useable", "Default" };
            //var layerNames = new string[] { "UI" };
            var layerMask = LayerMask.GetMask(layerNames);
            RaycastHit hit;
            bool wasHit = Physics.Raycast(raycast, out hit, FPSInputModule.current.maxInteractionDistance, layerMask);
            var end = rayDestination;
            if (wasHit)
            {
                end = hit.point;
            }
            else
            {

            }

            laserPointer.SetEnd(end);
            return mainCamera.WorldToScreenPoint(end);
        }

        public void UpdateControllerPositions()
        {
            if (SN1MC.UsingSteamVR)
            {
                rightController.transform.localPosition = Vector3.Lerp(rightController.transform.localPosition, VRInputManager.RightControllerPosition, VRCustomOptionsMenu.ikSmoothing);
                rightController.transform.localRotation = Quaternion.Lerp(rightController.transform.localRotation, VRInputManager.RightControllerRotation, VRCustomOptionsMenu.ikSmoothing);

                leftController.transform.localPosition = Vector3.Lerp(leftController.transform.localPosition, VRInputManager.LeftControllerPosition, VRCustomOptionsMenu.ikSmoothing);
                leftController.transform.localRotation = Quaternion.Lerp(leftController.transform.localRotation, VRInputManager.LeftControllerRotation, VRCustomOptionsMenu.ikSmoothing);

                //SteamVRActions.Valve.VR.SteamVR_Actions.SubnauticaVRUI.Activate();

                // TODO: Not sure if this is needed in the main menu, maybe in Pause menu?
                if (true)
                {
                    SteamVRActions.Valve.VR.SteamVR_Actions.SubnauticaVRUI.Activate();
                    SteamVRActions.Valve.VR.SteamVR_Actions.SubnauticaVRMain.Deactivate();
                }
                else
                {
                    SteamVRActions.Valve.VR.SteamVR_Actions.SubnauticaVRUI.Deactivate();
                    SteamVRActions.Valve.VR.SteamVR_Actions.SubnauticaVRMain.Activate();
                }
            }
            else
            {
                //ErrorMessage.AddDebug("Track: " + SN1MC.vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, allPoses));
                XRInputManager.GetXRInputManager().rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPos);
                XRInputManager.GetXRInputManager().rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightRot);

                rightController.transform.localPosition = Vector3.Lerp(rightController.transform.localPosition, rightPos, VRCustomOptionsMenu.ikSmoothing);
                rightController.transform.localRotation = Quaternion.Lerp(rightController.transform.localRotation, rightRot, VRCustomOptionsMenu.ikSmoothing);

                XRInputManager.GetXRInputManager().leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPos);
                XRInputManager.GetXRInputManager().leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftRot);

                leftController.transform.localPosition = Vector3.Lerp(leftController.transform.localPosition, leftPos, VRCustomOptionsMenu.ikSmoothing);
                leftController.transform.localRotation = Quaternion.Lerp(leftController.transform.localRotation, leftRot, VRCustomOptionsMenu.ikSmoothing);
            }
        }

        [HarmonyPatch(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.Awake))]
        public static class uGUI_MainMenu_Awake_Patch
        {
            [HarmonyPostfix]
            public static void Postffix(uGUI_MainMenu __instance)
            {
                if (!XRSettings.enabled || !VRCustomOptionsMenu.EnableMotionControls)
                {
                    return;
                }
                //main.InitializeMenu();
            }
        }

        [HarmonyPatch(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.Update))]
        public static class uGUI_Update_Patch
        {
            [HarmonyPostfix]
            public static void Postffix(uGUI_MainMenu __instance)
            {
                if (!XRSettings.enabled || !VRCustomOptionsMenu.EnableMotionControls)
                {
                    return;
                }
                //main.UpdateControllerPositions();
            }
        }
    }
}
