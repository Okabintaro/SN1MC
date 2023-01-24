using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Linq;
using RootMotion.FinalIK;

namespace SN1MC.Controls
{
    class VRHands : MonoBehaviour {
        public FullBodyBipedIK ik = null;

        public Transform leftTarget;
        public Transform rightTarget;

        public Transform leftHand;
        public Transform rightHand;
        public Transform leftElbow;
        public Transform rightElbow;

        private Vector3 leftElbowOffset;
        private Vector3 rightElbowOffset;

        public static VRHands instance;

        public void Setup(FullBodyBipedIK ik) {
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
            StartCoroutine(DisableBodyRendering());
            instance = this;
        }

        public void ResetHandTargets() {
            leftTarget.localPosition = new Vector3(-0.05f, 0.1f, -0.1f);
            leftTarget.localEulerAngles = new Vector3(0.0f, 0.0f, 90.0f);
            rightTarget.localPosition = new Vector3(0.05f, 0.1f, -0.1f);
            rightTarget.localEulerAngles = new Vector3(0.0f, 180.0f, 270.0f);
        }
        
        public void OnOpenPDA() {
            leftTarget.localPosition = new Vector3(-0.05f, 0.0418f, -0.14f);
            leftTarget.localEulerAngles = new Vector3(305.1264f, 354.6509f, 99.6091f);
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

        // TODO: Proper patch/fix, this doesnt need to run each 2 seconds
        IEnumerator DisableBodyRendering() {
            while(true) {
            // Extend globes BoundingBox to fight culling
                transform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true).Where(m => m.name.Contains("glove") || m.name.Contains("hands")).ForEach(
                mr => { 
                    var newBounds = new Bounds(Vector3.zero, new Vector3(3.0f, 3.0f, 3.0f));
                    mr.localBounds = newBounds;
                    mr.allowOcclusionWhenDynamic = false;
                });
                // Disable body rendering
                var bodyRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.name.Contains("body") || r.name.Contains("vest"));
                bodyRenderers.ForEach(r => r.enabled = false);
                yield return new WaitForSeconds(2.0f);
            }
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
            __instance.gameObject.AddComponent<VRHands>().Setup(__instance.ik);
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

    [HarmonyPatch(typeof(PDA), nameof(PDA.Open))]
    public static class SetPDAHandOffsets
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            VRHands.instance.OnOpenPDA();
        }
    }
    [HarmonyPatch(typeof(PDA), nameof(PDA.Close))]
    public static class UnsetPDAHandOffsets
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            VRHands.instance.OnClosePDA();
        }
    }

    #endregion
}
