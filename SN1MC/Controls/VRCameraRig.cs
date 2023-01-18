using HarmonyLib;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR;
using System.Collections;
using UWE;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;


/*
The VRCamera Rig handles the controllers together with their laser pointers to control the UI.
*/
namespace SN1MC.Controls
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;

    static class MyUtils {
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
    }

    class VRCameraRig : MonoBehaviour {
        // Setup and created in Awake()
        public Camera vrCamera;
        public GameObject leftController;
        public GameObject rightController;
        public LaserPointerNew laserPointer;
        public LaserPointerNew laserPointerLeft;

        public GameObject uiRig;
        public GameObject leftControllerUI;
        public GameObject rightControllerUI;
        public LaserPointerNew laserPointerUI;

        public GameObject modelL;
        public GameObject modelR;
        public ParentConstraint parentConstraint;

        public static VRCameraRig instance;
        private GameObject modelRUI;

        public Camera uiCamera = null;
        public bool trackMainCamera;

        public Camera EventCamera {
            get {
                if (laserPointer == null) {
                    return null;
                }
                return laserPointerUI.eventCamera;
            }
        }


        public void Start() {
            // TODO: Naming is inconsistent
            leftController = new GameObject(nameof(leftController)).WithParent(transform);
            rightController = new GameObject(nameof(rightController)).WithParent(transform);

            laserPointer = new GameObject(nameof(laserPointer)).WithParent(rightController.transform).AddComponent<LaserPointerNew>();
            laserPointerLeft = new GameObject(nameof(laserPointerLeft)).WithParent(leftController.transform).AddComponent<LaserPointerNew>();
            laserPointerLeft.gameObject.SetActive(false);
            laserPointer.gameObject.SetActive(false);

            // NOTE: These laserpointer and controllers is NOT parented to the Rig, since they act in UI space, not world space
            uiRig = new GameObject(nameof(uiRig));
            Object.DontDestroyOnLoad(uiRig);
            leftControllerUI = new GameObject(nameof(leftControllerUI)).WithParent(uiRig.transform);
            rightControllerUI = new GameObject(nameof(rightControllerUI)).WithParent(uiRig.transform);
            laserPointerUI = new GameObject(nameof(laserPointerUI)).WithParent(rightControllerUI.transform).AddComponent<LaserPointerNew>();
            // TODO: Constructors possible?
            laserPointerUI.doWorldRaycasts = false;
            laserPointerUI.useUILayer = true;

            var laserPointerAngle = new Vector3(45, 0, 0);
            laserPointerUI.transform.localEulerAngles = laserPointerAngle;
            laserPointerLeft.transform.localEulerAngles = laserPointerAngle;
            laserPointer.transform.localEulerAngles = laserPointerAngle;

            // TODO: Probably can get rid of this
            vrCamera = new GameObject(nameof(vrCamera)).WithParent(transform).AddComponent<Camera>();
            vrCamera.gameObject.tag = "MainCamera";

            SetupControllerModels();

            // Connect Input module and layer pointer together
            // TODO: This should be easier using singleton setup
            FPSInputModule fpsInput = FindObjectOfType<FPSInputModule>();
            laserPointer.inputModule = fpsInput;
            laserPointerLeft.inputModule = fpsInput;
            laserPointerUI.inputModule = fpsInput;

            parentConstraint = gameObject.AddComponent<ParentConstraint>();
            parentConstraint.constraintActive = false;
        }

        private void SetupControllerModels()
        {

            // Create two cubes to show controller positions
            // TODO: Replace with actual models from steamvr
            modelR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelR.transform.parent = rightController.transform;
            modelR.transform.localPosition.Set(0, 0, 0);
            modelR.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            Object.DestroyImmediate(modelR.GetComponent<BoxCollider>());

            modelL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelL.transform.parent = leftController.transform;
            modelL.transform.localPosition.Set(0, 0, 0);
            modelL.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            Object.DestroyImmediate(modelL.GetComponent<BoxCollider>());

            // Create two cubes to show controller positions
            // TODO: Replace with actual models from steamvr
            modelRUI = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelRUI.transform.parent = rightControllerUI.transform;
            modelRUI.transform.localPosition.Set(0, 0, 0);
            modelRUI.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            modelRUI.layer = LayerMask.NameToLayer("UI");
            Object.DestroyImmediate(modelRUI.GetComponent<BoxCollider>());
        }

        public void ParentTo(Transform target)
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
            parentConstraint.SetTranslationOffset(0, Vector3.zero);
            parentConstraint.SetRotationOffset(0, Vector3.zero);
            parentConstraint.locked = true;
            parentConstraint.constraintActive = true;
            parentConstraint.weight = 1.0f;
        }


        // This is used to get the camera from the main menu
        // Main issue with making a new camera was the water surface but that should also be fixable
        // TODO: Maybe remove this, so we only have one common camera
        public void StealCamera(Camera camera) {
            // Destroy/Delete old camera
            // NOTE: Subnautica renderes the water using specific camera component which also renders when the camera is disabled
            if (camera != vrCamera && vrCamera != null) {
                vrCamera.enabled = false;
                Destroy(vrCamera.gameObject);
            }

            vrCamera = camera;
            Vector3 oldPos = camera.transform.position;
            transform.position = oldPos;
            vrCamera.transform.parent = this.transform;
        }
        public void UseUICamera(Camera camera, bool trackMain=false) {
            uiRig.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            
            // TODO: I think this is wrong. Right should be 0, 0, while camera is above rig
            if (uiCamera != null)
            {
                Destroy(uiCamera.gameObject);
            }
            uiCamera = camera;
            camera.transform.parent = uiRig.transform;
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;

            // TODO: Move this hack somewhere else and explain why we move away a bit
            trackMainCamera = trackMain;
            if (trackMain) {
                var newPos = new Vector3(10, 0, 0);
                uiRig.transform.position = newPos;
                FindObjectOfType<uGUI>().gameObject.transform.position = newPos;
                var scalers = FindObjectsOfType<uGUI_CanvasScaler>();
                foreach (var scaler in scalers)
                {
                    scaler.vrMode = uGUI_CanvasScaler.Mode.Static;
                }
                foreach (var m in FindObjectsOfType<IngameMenu>())
                {
                    m.transform.position = newPos;
                }

            }


            CoroutineHost.StartCoroutine(EnableTracking());
        }

        public static IEnumerator EnableTracking() {
            yield return new WaitForSeconds(1.0f);
            XRDevice.DisableAutoXRCameraTracking(VRCameraRig.instance.uiCamera, false);
            yield break;
        }

        private void UpdateSteamVRControllers() {
            rightController.transform.localPosition = VRInputManager.RightControllerPosition;
            leftController.transform.localPosition = VRInputManager.LeftControllerPosition;
            rightController.transform.localRotation = VRInputManager.RightControllerRotation;
            leftController.transform.localRotation = VRInputManager.LeftControllerRotation;

            rightControllerUI.transform.localPosition = VRInputManager.RightControllerPosition;
            leftControllerUI.transform.localPosition = VRInputManager.LeftControllerPosition;
            rightControllerUI.transform.localRotation = VRInputManager.RightControllerRotation;
            leftControllerUI.transform.localRotation = VRInputManager.LeftControllerRotation;

            // TODO: Enable when needed
            if (false)
            {
                rightController.transform.localPosition = Vector3.Lerp(rightController.transform.localPosition, VRInputManager.RightControllerPosition, VRCustomOptionsMenu.ikSmoothing);
                rightController.transform.localRotation = Quaternion.Lerp(rightController.transform.localRotation, VRInputManager.RightControllerRotation, VRCustomOptionsMenu.ikSmoothing);
                leftController.transform.localPosition = Vector3.Lerp(leftController.transform.localPosition, VRInputManager.LeftControllerPosition, VRCustomOptionsMenu.ikSmoothing);
                leftController.transform.localRotation = Quaternion.Lerp(leftController.transform.localRotation, VRInputManager.LeftControllerRotation, VRCustomOptionsMenu.ikSmoothing);
            }
        }
        
        private void UpdateXRControllers() {
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
            if (trackMainCamera) {
                uiCamera.transform.localPosition = vrCamera.transform.localPosition;
                uiCamera.transform.localRotation = vrCamera.transform.localRotation;
            }
        }

        public void Update() {
            UpdateControllerPositions();
        }

        // TODO: This is temporary. Need to patch FPSInput properly.
        public IEnumerator SwapInputModule()
        {
            yield return new WaitForSeconds(1.0f);

            // VRInputModule vrim = fpsInput.gameObject.AddComponent<VRInputModule>();
            //laserPointer.inputModule = vrim;
            // Debug.Log($"Input Module before: {EventSystem.current.currentInputModule.name}");
            // vrim.ActivateModule();
            // vrim.eventCamera = laserPointer.eventCamera;
            // fpsInput.enabled = false;
            // Debug.Log($"Input Module after: {EventSystem.current.currentInputModule.name}");
            yield break;
        }

    }

    #region Patches

    // There might be a better hook for this
    [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
    public static class uGUI_AwakeSetupRig
    {
        [HarmonyPostfix]
        public static void Postfix(uGUI_MainMenu __instance)
        {
            if (!XRSettings.enabled || !VRCustomOptionsMenu.EnableMotionControls)
            {
                return;
            }

            // TODO: Should use proper singleton pattern?
            var rig = new GameObject(nameof(VRCameraRig)).AddComponent<VRCameraRig>();
            VRCameraRig.instance = rig;
            Object.DontDestroyOnLoad(rig);
        }
    }

    // Make the uGUI_GraphicRaycaster take the LaserPointers EventCamera when possible
    // TODO: More descriptive
    [HarmonyPatch(typeof(uGUI_GraphicRaycaster))]
    [HarmonyPatch(nameof(uGUI_GraphicRaycaster.eventCamera), MethodType.Getter)]
    class uGUI_GraphicRaycaster_VREventCamera_Patch
    {
        public static void Postfix(ref Camera __result)
        {
            if (!XRSettings.enabled || !VRCustomOptionsMenu.EnableMotionControls)
            {
                return;
            }
            if (VRCameraRig.instance != null)
            {
                // TODO: Switch here if in UI mode?
                // Can you cast from two cameras at the same time? Probably not :/
                // Just switch like the overiiden method
                var laserPointerCamera = VRCameraRig.instance.EventCamera;
                __result = laserPointerCamera;
            }
        }
    }

    [HarmonyPatch(typeof(ManagedCanvasUpdate), nameof(ManagedCanvasUpdate.GetUICamera))]
    public static class PatchCameraTrackingDisabled
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int callIndex = 0;
            bool found = false;
            foreach (var code in codes)
            {
                if (code.Calls(typeof(XRDevice).GetMethod(nameof(XRDevice.DisableAutoXRCameraTracking)))) {
                    found = true;
                    break;
                }
                callIndex++;
            }

            if (!found) {
                throw new System.Exception("Could not find call to patch");

            }

            codes.RemoveRange(callIndex-2, 3);
            return codes.AsEnumerable();
        }
    }

    #endregion

}