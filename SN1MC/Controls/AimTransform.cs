﻿using HarmonyLib;
using UnityEngine;
using Valve.VR;

//Sets the aim to the controller and gets rid of the old steamvr 
// TODO: Not sure if this is actually needed anymore
namespace SN1MC.Controls.UI
{
	public class AimTransform
	{
		[HarmonyPatch(typeof(Builder), nameof(Builder.GetAimTransform))]
		public static class Builder_GetAimTransform__Patch
		{
			[HarmonyPrefix]
			static bool Prefix(ref Transform __result)
			{
				__result = VRCameraRig.instance.laserPointer.transform;
				return false;
			}
		}


		[HarmonyPatch(typeof(VRInitialization), nameof(VRInitialization.InitializeSteamVR))]
		public static class VRInitialization_InitializeSteamVR__Patch
		{
			[HarmonyPrefix]
			static bool Prefix()
			{
				//OpenVR.Compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseSeated);
				return false;
			}
		}

		[HarmonyPatch(typeof(SNCameraRoot), nameof(SNCameraRoot.GetAimingTransform))]
		public static class SNCameraRoot_GetAimingTransform__Patch
		{
			[HarmonyPrefix]
			static bool Prefix(ref Transform __result)
			{
				__result = VRCameraRig.instance.laserPointer.transform;
				return false;
			}
		}
	}
}