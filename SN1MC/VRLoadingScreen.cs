﻿using UnityEngine;
using HarmonyLib;

namespace SN1MC
{
    //Credits to moose(#8572) from Flat2VR for this ported from the below zero vr mod makes it so the load screen is not in your face and you can see the game load, will prolly remove
    //cause there is mods that change this in Subnautica, just left it here for the time.
    class VRLoadingScreen
    {
        [HarmonyPatch(typeof(uGUI), nameof(uGUI.Update))]
        class uGUI_LoadingScreenReposition_Patch
        {

            private static GameObject loadingCanvasObject;
            private static Canvas loadingCanvas;

            static void Postfix(uGUI __instance)
            {
                maybeCreateLoadingCanvas();
                if (isLoading() && hasLoadingScreen(__instance))
                {
                    GameObject loadingScreen = __instance.loading.gameObject;
                    loadingScreen.transform.SetParent(loadingCanvas.GetComponent<RectTransform>());
                    loadingScreen.GetComponent<RectTransform>().position =

                    MainCamera.camera.gameObject.transform.parent.position + MainCamera.camera.gameObject.transform.parent.forward * 3f;
                  
                    loadingScreen.GetComponent<RectTransform>().LookAt(MainCamera.camera.gameObject.transform.parent);
                    loadingScreen.GetComponent<RectTransform>().Rotate(0f, 180f, 0f);
                }
            }

            private static bool isLoading()
            {
                return uGUI.main.loading.isLoading || !uGUI.main;
            }

            private static bool hasLoadingScreen(uGUI instance)
            {
                return instance.loading != null && instance.loading.gameObject != null;
            }

            private static void maybeCreateLoadingCanvas()
            {
                if (loadingCanvasObject != null && loadingCanvasObject)
                {
                    return;
                }
                loadingCanvasObject = new GameObject("VRLoadingCanvas");
                UnityEngine.Object.DontDestroyOnLoad(loadingCanvasObject);
                loadingCanvas = loadingCanvasObject.AddComponent<Canvas>();
                loadingCanvas.renderMode = RenderMode.WorldSpace;
                loadingCanvas.GetComponent<RectTransform>().SetParent(loadingCanvasObject.transform, false);
            }
        }

        /*[HarmonyPatch(typeof(uGUI_SceneLoading), nameof(uGUI_SceneLoading.SetFastLoadMode))]
        class uGUI_SceneLoading_SetFastLoadMode_Patch
        {
            static bool Prefix(uGUI_SceneLoading __instance, bool useFastLoadMode)
            {
                PlatformServices services = PlatformUtils.main.GetServices();
                if (services != null)
                {
                    services.SetUseFastLoadMode(useFastLoadMode);
                }
                return false;
            }
        }*/

    }
}