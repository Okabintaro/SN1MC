using HarmonyLib;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;
using UWE;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine.EventSystems;


/*
The VRCamera Rig handles the controllers together with their laser pointers to control the UI.
*/
namespace SN1MC.Controls
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;

    static class TargetAngles {
        public static readonly Vector3 Default = new Vector3(45, 0, 0);
        public static readonly Vector3 Forward = new Vector3(90, 0, 0);
        public static readonly Vector3 Up = new Vector3(0, 0, 0);
    }

    static class MyUtils
    {
        public static GameObject WithParent(this GameObject obj, Transform target)
        {
            obj.transform.parent = target;
            return obj;
        }
        public static GameObject WithParent(this GameObject obj, GameObject target)
        {
            obj.transform.parent = target.transform;
            return obj;
        }
        public static GameObject ResetTransform(this GameObject obj)
        {
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            return obj;
        }

        public static ParentConstraint ParentTo(this ParentConstraint parentConstraint, Transform target, Vector3 translationOffset)
        {
            // Remove old sources
            for (int i = 0; i < parentConstraint.sourceCount; i++)
            {
                parentConstraint.RemoveSource(0);
            }

            var cs = new ConstraintSource();
            cs.sourceTransform = target;
            cs.weight = 1.0f;
            parentConstraint.AddSource(cs);
            parentConstraint.SetTranslationOffset(0, translationOffset);
            parentConstraint.SetRotationOffset(0, Vector3.zero);
            parentConstraint.locked = true;
            parentConstraint.constraintActive = true;
            parentConstraint.weight = 1.0f;

            return parentConstraint;
        }
    }

    class VRCameraRig : MonoBehaviour
    {
        // Setup and created in Start()
        public Camera vrCamera;
        public GameObject leftController;
        public GameObject rightController;
        // Those are used for the IK/Hands
        public GameObject leftHandTarget;
        public GameObject rightHandTarget;

        // TODO: Those should not be full laserpointers probably
        // The only thing I need is cameras at different positions for the UI or Worldspace Raycasts
        public LaserPointerNew laserPointer;
        public LaserPointerNew laserPointerLeft;

        public GameObject uiRig;
        public GameObject leftControllerUI;
        public GameObject rightControllerUI;
        public LaserPointerNew laserPointerUI;

        public GameObject modelL;
        public GameObject modelR;

        public static VRCameraRig instance;
        private GameObject modelRUI;

        public Camera uiCamera = null;
        public bool uiTrackHead = false;
        public GameObject worldTarget;
        public float worldTargetDistance;
        private Transform rigParentTarget;

        public Camera UIControllerCamera
        {
            get
            {
                if (laserPointerUI == null)
                {
                    return null;
                }
                return laserPointerUI.eventCamera;
            }
        }
        public Camera WorldControllerCamera
        {
            get
            {
                if (laserPointer == null)
                {
                    return null;
                }
                return laserPointer.eventCamera;
            }
        }

        private Vector3 _targetAngle = TargetAngles.Default;
        private FPSInputModule fpsInput = null;

        public Vector3 TargetAngle
        {
            get {
                return _targetAngle;
            }
            set {
                _targetAngle = value;
                laserPointerUI.transform.localEulerAngles = value;
                laserPointerLeft.transform.localEulerAngles = value;
                laserPointer.transform.localEulerAngles = value;
            }
        }

        public static Transform GetTargetTansform()
        {
            // TODO: Switch depending on tool
            return VRCameraRig.instance.laserPointer.transform;
        }

        public void ParentTo(Transform target)
        {
            // GetComponent<ParentConstraint>().ParentTo(target, Vector3.zero);
            this.rigParentTarget = target;
        }


        public void Start()
        {
            // TODO: Naming is inconsistent, clean this mess up, only need 1/2 pointers?
            leftController = new GameObject(nameof(leftController)).WithParent(transform);
            rightController = new GameObject(nameof(rightController)).WithParent(transform);

            leftHandTarget = new GameObject(nameof(leftHandTarget)).WithParent(leftController);
            rightHandTarget = new GameObject(nameof(rightHandTarget)).WithParent(rightController);
            leftHandTarget.transform.localEulerAngles = new Vector3(270, 90, 0);
            Vector3 handOffset = new Vector3(90, 270, 0);
            rightHandTarget.transform.localEulerAngles = handOffset;

            // Laser Pointer Setup
            laserPointer = new GameObject(nameof(laserPointer)).WithParent(rightController.transform).AddComponent<LaserPointerNew>();
            laserPointerLeft = new GameObject(nameof(laserPointerLeft)).WithParent(leftController.transform).AddComponent<LaserPointerNew>();
            laserPointerLeft.gameObject.SetActive(false);
            // laserPointer.gameObject.SetActive(false);
            laserPointer.disableAfterCreation = true;

            // NOTE: These laserpointer and controllers is NOT parented to the Rig, since they act in UI space, not world space
            uiRig = new GameObject(nameof(uiRig));
            Object.DontDestroyOnLoad(uiRig);
            leftControllerUI = new GameObject(nameof(leftControllerUI)).WithParent(uiRig.transform);
            rightControllerUI = new GameObject(nameof(rightControllerUI)).WithParent(uiRig.transform);
            laserPointerUI = new GameObject(nameof(laserPointerUI)).WithParent(rightControllerUI.transform).AddComponent<LaserPointerNew>();
            // TODO: Constructors possible?
            laserPointerUI.doWorldRaycasts = true;
            laserPointerUI.useUILayer = true;
            this.TargetAngle = TargetAngles.Default;

            // TODO: Probably can get rid of this
            vrCamera = new GameObject(nameof(vrCamera)).WithParent(transform).AddComponent<Camera>();
            vrCamera.gameObject.tag = "MainCamera";

            SetupControllerModels();

            // Connect Input module and layer pointer together
            // TODO: This should be easier using singleton setup
            fpsInput = FindObjectOfType<FPSInputModule>();
            laserPointer.inputModule = fpsInput;
            laserPointerLeft.inputModule = fpsInput;
            laserPointerUI.inputModule = fpsInput;
        }

        private void SetupControllerModels()
        {
            // Create two cubes to show controller positions
            // TODO: Replace with actual models from steamvr
            modelR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelR.transform.parent = rightControllerUI.transform;
            modelR.transform.localPosition.Set(0, 0, 0);
            modelR.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            Object.Destroy(modelR.GetComponent<BoxCollider>());

            modelL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelL.transform.parent = leftControllerUI.transform;
            modelL.transform.localPosition.Set(0, 0, 0);
            modelL.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            Object.Destroy(modelL.GetComponent<BoxCollider>());

            modelL.layer = LayerID.UI;
            modelR.layer = LayerID.UI;
            modelR.GetComponent<MeshRenderer>().enabled = VRCustomOptionsMenu.DebugEnabled;
            modelL.GetComponent<MeshRenderer>().enabled = VRCustomOptionsMenu.DebugEnabled;
        }

        // This is used to get the camera from the main menu
        // Main issue with making a new camera was the water surface but that should also be fixable
        // TODO: Maybe remove this, so we only have one common camera
        public void StealCamera(Camera camera)
        {
            // Destroy/Delete old camera
            // NOTE: Subnautica renderes the water using specific camera component which also renders when the camera is disabled
            if (camera != vrCamera && vrCamera != null)
            {
                vrCamera.enabled = false;
                Destroy(vrCamera.gameObject);
            }

            vrCamera = camera;
            Vector3 oldPos = camera.transform.position;
            transform.position = oldPos;
            vrCamera.transform.parent = this.transform;
        }

        public void StealUICamera(Camera camera, bool fromGame = false)
        {
            uiRig.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            if (uiCamera != null)
            {
                Destroy(uiCamera.gameObject);
            }

            if (fromGame)
            {
                // This fixes a weird issue I had, where the UI Camera from the game would behave like it wasnt moving
                // even though the transform was changing properly.
                // Maybe it is because the tracking was once disabled in the main game, but I am not sure, since I tried enabling it too.
                // Copying the properties from the main camera and setting up the original important properties fixed it.
                uiRig.transform.position = Vector3.zero;
                var oldMask = camera.cullingMask;
                var oldClear = camera.clearFlags;
                var oldDepth = camera.depth;

                camera.CopyFrom(SNCameraRoot.main.mainCamera);
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localRotation = Quaternion.identity;
                camera.renderingPath = RenderingPath.Forward;
                camera.cullingMask = oldMask;
                camera.clearFlags = CameraClearFlags.Depth;
                camera.depth = oldDepth;

                camera.transform.parent = uiRig.transform;
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localRotation = Quaternion.identity;

                // Set all canvas scalers to static, which makes UI better usable
                FindObjectsOfType<uGUI_CanvasScaler>().Where(obj => !obj.name.Contains("PDA")).ForEach(cs => cs.vrMode = uGUI_CanvasScaler.Mode.Static);
            }
            else
            {
                camera.transform.parent = uiRig.transform;
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localRotation = Quaternion.identity;
            }
            uiCamera = camera;
        }

        public IEnumerator SetupGameCameras()
        {
            var rig = VRCameraRig.instance;
            rig.StealCamera(SNCameraRoot.main.mainCamera);
            yield return new WaitForSeconds(1.0f);
            rig.StealUICamera(SNCameraRoot.main.guiCamera, true);
            yield return new WaitForSeconds(1.0f);

            var screenCanvas = uGUI.main.screenCanvas.gameObject;
            screenCanvas.WithParent(uiCamera.transform).ResetTransform();
            var clipCamera = FindObjectsOfType<Camera>().First(cam => cam.name == "Clip Camera");
            clipCamera.gameObject.WithParent(vrCamera.transform).ResetTransform();

            // Steal Reticle and attach to the right hand
            var handReticle = HandReticle.main.gameObject.WithParent(rightControllerUI.transform);
            handReticle.AddComponent<Canvas>().worldCamera = uiCamera;
            handReticle.transform.localEulerAngles = new Vector3(90, 0, 0);
            handReticle.transform.localPosition = new Vector3(0, 0, 0.05f);
            handReticle.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            FindObjectsOfType<uGUI_CanvasScaler>().ForEach(cs => cs.SetDirty());
        }

        private void UpdateSteamVRControllers()
        {
            rightController.transform.localPosition = SteamVRInputManager.RightControllerPosition;
            leftController.transform.localPosition = SteamVRInputManager.LeftControllerPosition;
            rightController.transform.localRotation = SteamVRInputManager.RightControllerRotation;
            leftController.transform.localRotation = SteamVRInputManager.LeftControllerRotation;

            rightControllerUI.transform.localPosition = SteamVRInputManager.RightControllerPosition;
            leftControllerUI.transform.localPosition = SteamVRInputManager.LeftControllerPosition;
            rightControllerUI.transform.localRotation = SteamVRInputManager.RightControllerRotation;
            leftControllerUI.transform.localRotation = SteamVRInputManager.LeftControllerRotation;

            // TODO: Enable when needed
            if (false)
            {
                rightController.transform.localPosition = Vector3.Lerp(rightController.transform.localPosition, SteamVRInputManager.RightControllerPosition, VRCustomOptionsMenu.ikSmoothing);
                rightController.transform.localRotation = Quaternion.Lerp(rightController.transform.localRotation, SteamVRInputManager.RightControllerRotation, VRCustomOptionsMenu.ikSmoothing);
                leftController.transform.localPosition = Vector3.Lerp(leftController.transform.localPosition, SteamVRInputManager.LeftControllerPosition, VRCustomOptionsMenu.ikSmoothing);
                leftController.transform.localRotation = Quaternion.Lerp(leftController.transform.localRotation, SteamVRInputManager.LeftControllerRotation, VRCustomOptionsMenu.ikSmoothing);
            }
        }

        private void UpdateXRControllers()
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

        public void UpdateControllerPositions()
        {
            if (SN1MC.UsingSteamVR)
            {
                UpdateSteamVRControllers();
            }
            else
            {
                UpdateXRControllers();
            }
        }

        public void Update()
        {
            // Move the camera rig to the player each frame and rotate the uiRig accordingly
            if (rigParentTarget != null)
            {
                this.transform.SetPositionAndRotation(rigParentTarget.position, rigParentTarget.rotation);
                uiRig.transform.rotation = transform.rotation;
            }
            UpdateControllerPositions();

            if (VRCustomOptionsMenu.DebugEnabled) {
                RaycastResult? uiTarget = fpsInput?.lastRaycastResult;
                DebugPanel.Show($"World Target: {worldTarget?.name}({worldTargetDistance})\nUI Target:{uiTarget?.gameObject?.name}({uiTarget?.distance})\nFocused: {EventSystem.current.isFocused}");
            }
        }

        // Gets set by GUIHand Patch, which already does world raycasting so we dont have to do it ourselfs
        public void SetWorldTarget(GameObject activeTarget, float activeHitDistance)
        {
            this.worldTarget = activeTarget;
            this.worldTargetDistance = activeHitDistance;
            this.laserPointerUI.SetWorldTarget(worldTarget, worldTargetDistance);
        }
    }

    #region Patches

    // Create the Rig together with the uGUI Prefab
    [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
    public static class uGUI_AwakeSetupRig
    {
        [HarmonyPostfix]
        public static void Postfix(uGUI_MainMenu __instance)
        {
            // TODO: Should use proper singleton pattern?
            var rig = new GameObject(nameof(VRCameraRig)).AddComponent<VRCameraRig>();
            VRCameraRig.instance = rig;
            Object.DontDestroyOnLoad(rig);
        }
    }

    // Make the uGUI_GraphicRaycaster take the LaserPointers EventCamera when possible
    // Have to switch between guiCameraSpace and Worldspace for e.g. Scanner Room and Cyclops UI
    [HarmonyPatch(typeof(uGUI_GraphicRaycaster))]
    [HarmonyPatch(nameof(uGUI_GraphicRaycaster.eventCamera), MethodType.Getter)]
    class uGUI_GraphicRaycaster_VREventCamera_Patch
    {
        public static bool Prefix(uGUI_GraphicRaycaster __instance, ref Camera __result)
        {
            if (VRCameraRig.instance == null)
            {
                return true;
            }
            if (!(SNCameraRoot.main != null))
            {
                __result = VRCameraRig.instance.UIControllerCamera;
            }
            else
            {
                if (__instance.guiCameraSpace)
                {
                    __result = VRCameraRig.instance.UIControllerCamera;
                }
                else
                {
                    __result = VRCameraRig.instance.WorldControllerCamera;
                }
            }
            return false;
        }
    }

    // Same Patch as above but for the UnityEngine GraphicRaycaster
    // Turns out some canvases like the left panel on the cyclops don't use the uGUI_GraphicRaycaster
    // TODO: They seem to be in world space only though, have to double check.
    [HarmonyPatch(typeof(GraphicRaycaster))]
    [HarmonyPatch(nameof(GraphicRaycaster.eventCamera), MethodType.Getter)]
    class Unity_GraphicRaycaster_VREventCamera_Patch
    {
        public static bool Prefix(GraphicRaycaster __instance, ref Camera __result)
        {
            // TODO: Clean this up
            var canvas = __instance.GetComponent<Canvas>();
            if (canvas == null) {
                return true;
            }
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null))
            {
                return true;
            }
            if (VRCameraRig.instance == null)
            {
                return true;
            }
            __result = VRCameraRig.instance.WorldControllerCamera;
            return false;
        }
    }

    [HarmonyPatch(typeof(uGUI_CanvasScaler), nameof(uGUI_CanvasScaler.UpdateTransform))]
    static class uGUI_CanvasScalerPDA_Attach
    {
        public static void Postfix(uGUI_CanvasScaler __instance)
        {
            // TODO: There gotta be a better way to attach this only to the PDA, maybe custom behaviour, disabling the Scalar?
            if (__instance.gameObject.GetComponent<uGUI_PDA>() == null)
            {
                return;
            }
            if (VRCameraRig.instance == null)
                return;
            var rigWorldPos = SNCameraRoot.main.transform;

            var worldPos = __instance._anchor.transform.position;
            var worldRot = __instance._anchor.transform.rotation;
            var uiSpacePos = worldPos - rigWorldPos.position;
            var uiSpaceRotation = worldRot;
            // DebugPanel.Show($"PDA Pos/Rot: {worldPos}/{worldRot.eulerAngles}\n -> {uiSpacePos}/{uiSpaceRotation.eulerAngles}\nrigPos/rot: {rigWorldPos.position}, {rigWorldPos.eulerAngles}");
            __instance.rectTransform.position = uiSpacePos;
            __instance.rectTransform.rotation = uiSpaceRotation;
        }
    }

    // Makes the ingame menu spawn infront of you in vr
    [HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Awake))]
    class MakeIngameMenuStatic
    {
        public static void Postfix(IngameMenu __instance)
        {
            var scalar = __instance.GetComponent<uGUI_CanvasScaler>();
            scalar.vrMode = uGUI_CanvasScaler.Mode.Static;
        }
    }

    // Makes the builder menu spawn infront of you in vr
    // TODO: Could make those more general patches?
    [HarmonyPatch(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.Awake))]
    class MakeBuilderMenuStatic
    {
        public static void Postfix(uGUI_BuilderMenu __instance)
        {
            var scalar = __instance.GetComponent<uGUI_CanvasScaler>();
            scalar.vrMode = uGUI_CanvasScaler.Mode.Static;
        }
    }
    // Makes the builder menu spawn infront of you in vr
    [HarmonyPatch(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.Open))]
    class MakeBuilderMenuStatic2
    {
        public static void Postfix(uGUI_BuilderMenu __instance)
        {
            var scalar = __instance.GetComponent<uGUI_CanvasScaler>();
            scalar.SetDirty();
            // TODO: Look into the dirty a bit more. Why does it work for Fabricators?
            scalar.UpdateTransform(SNCameraRoot.main.guiCamera);
        }
    }

    // Create the VRCameraRig when ArmsController is started
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Start))]
    public static class ArmsController_Start_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ArmsController __instance)
        {
            if (!XRSettings.enabled || !VRCustomOptionsMenu.EnableMotionControls)
            {
                return;
            }
            // TODO/DOING: Fixing this
            Camera mainCamera = SNCameraRoot.main.mainCam;
            VRCameraRig.instance.ParentTo(mainCamera.transform.parent);
            VRCameraRig.instance.StealCamera(SNCameraRoot.main.mainCamera);
            CoroutineHost.StartCoroutine(VRCameraRig.instance.SetupGameCameras());
        }
    }

    // Get the current raycast world target from GUIHand to use for the laserpointer on right hand
    [HarmonyPatch(typeof(GUIHand), nameof(GUIHand.UpdateActiveTarget))]
    public static class GUIHandPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GUIHand __instance)
        {
            var rig = VRCameraRig.instance;
            if (rig == null)
            {
                return;
            }
            rig.SetWorldTarget(__instance.activeTarget, __instance.activeHitDistance);
        }
    }


    // Don't disable the the automatic camera tracking of the UI Camera in the Main Game
    [HarmonyPatch(typeof(ManagedCanvasUpdate), nameof(ManagedCanvasUpdate.GetUICamera))]
    public static class PatchCameraTrackingDisabled
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false, new CodeMatch[] {
                new CodeMatch(ci => ci.Calls(typeof(XRDevice).GetMethod(nameof(XRDevice.DisableAutoXRCameraTracking))))
            }).ThrowIfNotMatch("Could not find XRDevice Deactivation").Advance(-2).RemoveInstructions(3).InstructionEnumeration();
        }
    }

    #endregion
}