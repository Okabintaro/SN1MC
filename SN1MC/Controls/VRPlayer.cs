using HarmonyLib;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR;
using System.Collections;
using UWE;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using RootMotion.FinalIK;

namespace SN1MC.Controls
{
    class VRPlayer : MonoBehaviour {
        public FullBodyBipedIK ik = null;

        public Transform leftTarget;
        public Transform rightTarget;

        public Transform leftHand;
        public Transform rightHand;
        public Transform leftElbow;
        public Transform rightElbow;

        private Vector3 leftElbowOffset;
        private Vector3 rightElbowOffset;

        public static VRPlayer instance;

        public void SetIK(FullBodyBipedIK ik) {
            this.ik = ik;
            leftHand = ik.solver.leftHandEffector.bone;
            rightHand = ik.solver.rightHandEffector.bone;
            leftElbow = leftHand.parent;
            rightElbow = rightHand.parent;
            leftHand.parent = leftElbow.parent;
            rightHand.parent = rightElbow.parent;

            var camRig = VRCameraRig.instance;
            leftTarget = camRig.leftHandTarget.transform;
            rightTarget = camRig.rightHandTarget.transform;

            leftElbowOffset = leftElbow.transform.position - leftHand.transform.position;
            rightElbowOffset = rightElbow.transform.position - rightHand.transform.position;
            ResetHandTargets();
            // Extend globes BoundingBox to fight culling
            FindObjectsOfType<SkinnedMeshRenderer>().Where(m => m.name.Contains("gloves_geo") || m.name.Contains("hands_geo")).ForEach(
                mr => { mr.localBounds = new Bounds(Vector3.zero, new Vector3(2.0f, 2.0f, 2.0f));
                }
            );

            StartCoroutine(DisableBodyRendering());
            instance = this;
        }

        public void ResetHandTargets() {
            leftTarget.localPosition = new Vector3(-0.05f, 0.1f, -0.1f);
            leftTarget.localEulerAngles = new Vector3(0.0f, 0.0f, 90.0f);
            rightTarget.localPosition = new Vector3(0.05f, 0.1f, -0.1f);
            rightTarget.localEulerAngles = new Vector3(0.0f, 180.0f, 270.0f);
        }
        
        // TODO: Hook those
        public void OnOpenPDA() {
            leftTarget.localPosition = new Vector3(-0.05f, 0.0418f, -0.14f);
            leftTarget.localEulerAngles = new Vector3(305.1264f, 354.6509f, 99.6091f);
            ResetHandTargets();
        }
        public void OnClosePDA() {
            ResetHandTargets();
        }

        void Update() {
            if (ik.enabled) {
                ik.solver.leftHandEffector.target = leftTarget;
                ik.solver.rightHandEffector.target = rightTarget;
            }
        }

        void LateUpdate() {
            // Hand/controller tracking without IK
            if (this.ik.enabled) {
                return;
            }
            var cElbow = VRCameraRig.instance;

            leftHand.transform.SetPositionAndRotation(leftTarget.position, leftTarget.rotation);
            rightHand.transform.SetPositionAndRotation(rightTarget.position, rightTarget.rotation);

            leftElbow.transform.SetPositionAndRotation(leftHand.position, leftHand.rotation);
            rightElbow.transform.SetPositionAndRotation(rightHand.position, rightHand.rotation);

            leftElbow.localScale = Vector3.zero;
            rightElbow.localScale = Vector3.zero;

        }


        // TODO: Proper patch/fix
        IEnumerator DisableBodyRendering() {
            while(true) {
                // Disable body rendering
                var bodyRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>().Where(renderer => renderer.name.Contains("body"));
                bodyRenderers.ForEach(r => r.enabled = false);
                yield return new WaitForSeconds(2.0f);
            }
            yield break;
        }

    }

    #region Patches

    // TODO: Move/cleanup this
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Start))]
    public class VRPlayerCreate : MonoBehaviour
    {

        [HarmonyPostfix]
        public static void Postfix(ArmsController __instance)
        {
            // Disable IK
            __instance.ik.enabled = false;
            __instance.leftAim.aimer.enabled = false;
            __instance.rightAim.aimer.enabled = false;

            // Attach
            __instance.gameObject.AddComponent<VRPlayer>().SetIK(__instance.ik);
            // __instance.pda.ui.canvasScaler.vrMode = uGUI_CanvasScaler.Mode.Inversed;
        }
    }

    // TODO: Write tooling to easily determine offsets foreach tool
    //[HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Reconfigure))]
    //public static class ReconfigureHandIK
    //{
    //    [HarmonyPostfix]
    //    public static bool Prefix(ArmsController __instance, PlayerTool tool)
    //    {
    //        return false;
    //    }
    //}

    // This removes the animation that inspects the object/tool when equipped for the first time
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.StartInspectObjectAsync))]
    public static class DontInspectObjectAnimation
    {
        [HarmonyPostfix]
        public static bool Prefix(ArmsController __instance)
        {
            return false;
        }
    }

    #endregion
}
