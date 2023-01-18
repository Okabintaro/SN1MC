using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace SN1MC.Controls
{
    class VRMainMenu : MonoBehaviour
    {
        public static void ModifyMainMenuForVr() {
            // Set Main Camera to render everything, including UI
            Camera mainCamera = GameObject.FindGameObjectsWithTag("MainCamera").First(c => c.name.Equals("Main Camera")).GetComponent<Camera>();
            mainCamera.cullingMask = -1;
            // VRCameraRig.instance.SwapCamera(mainCamera);
            VRCameraRig.instance.StealCamera(mainCamera);

            // Turn off UI Camera
            Camera uiCamera = FindObjectsOfType<Camera>().First(c => c.name.Equals("UI Camera"));
            uiCamera.enabled = false;

            // Move Main Menu Canvas in front of player
            Canvas menuCanvas = FindObjectsOfType<Canvas>().First(c => c.name.Equals("Menu canvas"));
            menuCanvas.transform.position = new Vector3(0, 1.5f, 1.00f);

            //var raycaster = menuCanvas.gameObject.GetComponent<uGUI_GraphicRaycaster>();
            //DestroyImmediate(raycaster);
            // TODO: Patch it?
            //raycaster.enabled = false;
            //menuCanvas.gameObject.AddComponent<GraphicRaycaster>();

            // TODO: Increase UI fidelity?
        }

        public static void SetupMainMenu()
        {
            Camera uiCamera = FindObjectsOfType<Camera>().First(c => c.name.Equals("UI Camera"));
            VRCameraRig.instance.UseUICamera(uiCamera);
            Camera mainCamera = GameObject.FindGameObjectsWithTag("MainCamera").First(c => c.name.Equals("Main Camera")).GetComponent<Camera>();
            VRCameraRig.instance.StealCamera(mainCamera);
        }
    }

    [HarmonyPatch(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.Awake))]
    public static class MainMenu_SetupVR
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            //VRMainMenu.ModifyMainMenuForVr();
            VRMainMenu.SetupMainMenu();
            VRInputManager.SwitchToUIBinding();
        }
    }



}
