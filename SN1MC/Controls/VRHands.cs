using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Linq;
using RootMotion.FinalIK;

namespace SN1MC.Controls
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;
    using System;
    using System.Globalization;

    public struct TransformOffset
    {
        public TransformOffset(Vector3 pos, Vector3 angles)
        {
            Pos = pos;
            Angles = angles;
        }

        public TransformOffset(Transform transform) : this()
        {
            Pos = transform.localPosition;
            Angles = transform.localEulerAngles;
        }

        public Vector3 Pos { get; }
        public Vector3 Angles { get; }
        public override string ToString() => $"TransformOffset(Pos=({Pos.x:f3}, {Pos.y:f3}, {Pos.z:f3}), Angles=({Angles.x:f3}, {Angles.y:f3}, {Angles.z:f3}))";
        internal string SwitchString(string type) {
            FormattableString str = $"case {type} _: return new TransformOffset(new Vector3({Pos.x:f3}f, {Pos.y:f3}f, {Pos.z:f3}f), new Vector3({Angles.x:f3}f, {Angles.y:f3}f, {Angles.z:f3}f));";
            return str.ToString(CultureInfo.InvariantCulture);
        }

        public void Apply(Transform tf) {
            tf.localPosition = Pos;
            tf.localEulerAngles = Angles;
        }

    }

    // The following four methods are used to calibrate offsets of items/tools in hands
    class OffsetCalibrationTool {

        SteamVR_Action_Boolean holdToMoveAction;
        SteamVR_Action_Boolean saveTransformAction;
        Transform target;
        Transform parent;

        private OffsetCalibrationTool() {}

        public OffsetCalibrationTool(Transform target, SteamVR_Action_Boolean holdToMoveAction, SteamVR_Action_Boolean saveTransformAction)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.holdToMoveAction = holdToMoveAction ?? throw new ArgumentNullException(nameof(holdToMoveAction));
            this.saveTransformAction = saveTransformAction ?? throw new ArgumentNullException(nameof(saveTransformAction));
        }

        public void Enable() {
            holdToMoveAction.onStateDown += UnparentTarget;
            holdToMoveAction.onStateUp += ReparentTarget;
            saveTransformAction.onStateDown += SaveTransform;
        }
        public void Disable() {
            holdToMoveAction.onStateDown -= UnparentTarget;
            holdToMoveAction.onStateUp -= ReparentTarget;
            saveTransformAction.onStateDown -= SaveTransform;
        }

        // Unparent Target, so we can finetune the position
        public void UnparentTarget(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
            parent = target.parent;
            var tool = global::Player.main.armsController.lastTool;
            target.SetParent(null, true);
        }

        // Reparent the target to hand/controller again
        public void ReparentTarget(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
            target.SetParent(parent, true);
            var angles = target.localEulerAngles;
            var snapAngle = 15;
            target.localEulerAngles = new Vector3(angles.x.Snap(snapAngle), angles.y.Snap(snapAngle), angles.z.Snap(snapAngle));
        }

        // Save the transform by logging it to a logfile
        // TODO: Could use an event?
        public void SaveTransform(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
            // Print it out for putting it into the mod
            var tool = global::Player.main.armsController.lastTool;
            string offset = new TransformOffset(target).SwitchString(tool.GetType().Name);
            Mod.logger.LogInfo(offset);
            ErrorMessage.AddDebug($"Saved Offset for {tool.GetType().Name}!\n{offset}");
        }
    }

    // Define all the Offsets
    static class ToolOffset {
        // Default Offset when no tool is Equipped
        public static TransformOffset Hand = new TransformOffset(new Vector3(0.05f, 0.1f, -0.1f), new Vector3(0.0f, 180.0f, 270.0f));

        internal static TransformOffset GetHandOffset(this PlayerTool tool) {
            switch (tool) {
                case Seaglide _: return new TransformOffset(new Vector3(0.050f, 0.100f, -0.100f), new Vector3(0.000f, 180.000f, 270.000f));
                case Gravsphere _: return new TransformOffset(new Vector3(-0.010f, 0.114f, -0.125f), new Vector3(10.485f, 158.948f, 244.422f));
                case DeployableStorage _: return new TransformOffset(new Vector3(0.017f, 0.099f, -0.135f), new Vector3(27.633f, 159.160f, 251.929f));
                case Constructor _: return new TransformOffset(new Vector3(0.042f, 0.076f, -0.166f), new Vector3(53.635f, 151.667f, 249.508f));
                case LEDLight _: return new TransformOffset(new Vector3(0.051f, 0.113f, -0.122f), new Vector3(20.287f, 157.143f, 262.503f));
                case Knife _: return new TransformOffset(new Vector3(0.008f, 0.095f, -0.115f), new Vector3(17.193f, 162.033f, 250.308f));
                case FlashLight _: return new TransformOffset(new Vector3(0.011f, 0.125f, -0.123f), new Vector3(18.573f, 162.636f, 247.017f));
                case Beacon _: return new TransformOffset(new Vector3(0.006f, 0.137f, -0.165f), new Vector3(31.791f, 151.351f, 242.064f));
                case StasisRifle _: return new TransformOffset(new Vector3(0.013f, 0.091f, -0.155f), new Vector3(32.203f, 147.266f, 237.102f));
                case PropulsionCannonWeapon _: return new TransformOffset(new Vector3(0.001f, 0.078f, -0.169f), new Vector3(36.510f, 148.234f, 231.454f));
                case BuilderTool _: return new TransformOffset(new Vector3(0.042f, 0.090f, -0.129f), new Vector3(30.567f, 155.533f, 258.169f));
                case AirBladder _: return new TransformOffset(new Vector3(-0.032f, 0.090f, -0.133f), new Vector3(7.689f, 145.798f, 224.260f));
                case DiveReel _: return new TransformOffset(new Vector3(-0.019f, 0.096f, -0.119f), new Vector3(14.196f, 148.834f, 238.635f));
                case Welder _: return new TransformOffset(new Vector3(0.002f, 0.110f, -0.140f), new Vector3(23.999f, 153.807f, 241.427f));
                case ScannerTool _: return new TransformOffset(new Vector3(0.006f, 0.109f, -0.149f), new Vector3(25.775f, 145.925f, 236.092f));
                case LaserCutter _: return new TransformOffset(new Vector3(-0.017f, 0.123f, -0.133f), new Vector3(17.726f, 151.261f, 233.612f));
                case Flare _: return new TransformOffset(new Vector3(0.019f, 0.088f, -0.134f), new Vector3(31.170f, 153.374f, 244.228f));
                case RepulsionCannon _: return new TransformOffset(new Vector3(-0.002f, 0.088f, -0.166f), new Vector3(33.777f, 149.093f, 232.610f));
                default: return Hand;
            };
        }
    }


    class VRHands : MonoBehaviour
    {
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

        private OffsetCalibrationTool calibrationTool;

        public void Setup(FullBodyBipedIK ik)
        {
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

            calibrationTool = new OffsetCalibrationTool(rightTarget, SteamVR_Actions.subnauticaVRMain_MoveDown, SteamVR_Actions.subnauticaVRMain_LeftHand);
            if (VRCameraRig.instance.DebugEnabled)
                calibrationTool.Enable();

            instance = this;
        }

        public void ResetHandTargets()
        {
            leftTarget.localPosition = new Vector3(-0.05f, 0.1f, -0.1f);
            leftTarget.localEulerAngles = new Vector3(0.0f, 0.0f, 90.0f);
            rightTarget.localPosition = new Vector3(0.05f, 0.1f, -0.1f);
            rightTarget.localEulerAngles = new Vector3(0.0f, 180.0f, 270.0f);
        }
        public void OnOpenPDA()
        {
            leftTarget.localPosition = new Vector3(-0.05f, 0.0418f, -0.14f);
            leftTarget.localEulerAngles = new Vector3(305.1264f, 354.6509f, 99.6091f);
        }
        public void OnClosePDA()
        {
            ResetHandTargets();
        }

        void Update()
        {
            if (ik.enabled)
            {
                ik.solver.leftHandEffector.target = leftTarget;
                ik.solver.rightHandEffector.target = rightTarget;
            }
        }

        void LateUpdate()
        {
            // Hand/controller tracking without IK
            if (this.ik.enabled)
            {
                // TODO: Add back experimental IK behind an option
                return;
            }

            // Move the hands to the targets which are attached to the controllers
            leftHand.transform.SetPositionAndRotation(leftTarget.position, leftTarget.rotation);
            rightHand.transform.SetPositionAndRotation(rightTarget.position, rightTarget.rotation);

            // Reset Elbows
            leftElbow.transform.SetPositionAndRotation(leftHand.position, leftHand.rotation);
            rightElbow.transform.SetPositionAndRotation(rightHand.position, rightHand.rotation);
            leftElbow.localScale = Vector3.zero;
            rightElbow.localScale = Vector3.zero;
        }

        // TODO: Proper patch/fix, this doesnt need to run each 2 seconds
        IEnumerator DisableBodyRendering()
        {
            while (true)
            {
                // Extend globes BoundingBox to fight culling
                transform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true).Where(m => m.name.Contains("glove") || m.name.Contains("hands")).ForEach(
                mr =>
                {
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


        internal void ConfigureLaserPointer(PlayerTool tool)
        {
            var rig = VRCameraRig.instance;
            // TODO: See if we maybe can use switch Expressions from c# 9?
            switch (tool)
            {
                case ScannerTool _:
                case StasisRifle _:
                case RepulsionCannon _:
                case LaserCutter _:
                case Welder _:
                    rig.TargetAngle = TargetAngles.Forward;
                    break;
                default:
                    rig.TargetAngle = TargetAngles.Default;
                    break;
            }
        }

        internal void ConfigureToolPose(PlayerTool tool)
        {
            tool.GetHandOffset().Apply(rightTarget.transform);
        }

        internal void OnToolEquipped(PlayerTool tool)
        {
            ConfigureToolPose(tool);
            ConfigureLaserPointer(tool);
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

    // Reconfigure the aiming
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Reconfigure))]
    public static class ChangeAimAngleForTools
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerTool tool)
        {
            VRHands.instance?.OnToolEquipped(tool);
        }
    }

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
