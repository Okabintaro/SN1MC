using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace SN1MC.Controls
{
    class VRMainMenu : MonoBehaviour
    {
        public static void SetupMainMenu()
        {
            Mod.logger.LogInfo("Patching Main Menu...");
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
            VRMainMenu.SetupMainMenu();
            SteamVRInputManager.SwitchToUIBinding();
        }
    }



}
