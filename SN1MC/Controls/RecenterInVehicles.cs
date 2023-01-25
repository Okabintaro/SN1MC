using HarmonyLib;
using UnityEngine;

namespace SN1MC.Controls
{
	[HarmonyPatch(typeof(SeaMoth), nameof(SeaMoth.OnPlayerEntered))]
	static class RecenterInSeamoth {
		public static void Postfix() {
            VRUtil.Recenter();
		}
	}

	[HarmonyPatch(typeof(Exosuit), nameof(Exosuit.OnPlayerEntered))]
	static class RecenterInExosuit {
		public static void Postfix() {
            VRUtil.Recenter();
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.EnterPilotingMode))]
	static class RecenterWhenPilotingCyclops {
		public static void Postfix() {
            VRUtil.Recenter();
		}
	}
}