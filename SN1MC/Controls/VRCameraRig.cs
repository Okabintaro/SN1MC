using HarmonyLib;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR;
using System.Collections;
using UWE;


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
    }

    class VRCameraRig : MonoBehaviour {
        // Setup and created in Awake()
        public Camera vrCamera;
        public GameObject leftController;
        public GameObject rightController;
        public LaserPointerNew laserPointer;
        public LaserPointerNew laserPointerLeft;

        public LaserPointerNew laserPointerUI;

        public GameObject modelL;
        public GameObject modelR;
        public ParentConstraint parentConstraint;

        public static VRCameraRig instance;

        public Camera EventCamera {
            get {
                if (laserPointer == null) {
                    return null;
                }
                return laserPointer.eventCamera;
            }
        }

        public void Start() {
            // TODO: Naming is inconsistent
            leftController = new GameObject(nameof(leftController)).WithParent(transform);
            rightController = new GameObject(nameof(rightController)).WithParent(transform);

            laserPointer = new GameObject(nameof(laserPointer)).WithParent(rightController.transform).AddComponent<LaserPointerNew>();
            laserPointerLeft = new GameObject(nameof(laserPointerLeft)).WithParent(leftController.transform).AddComponent<LaserPointerNew>();
            laserPointerLeft.gameObject.SetActive(false);

            // laserPointerUI = new GameObject(nameof(laserPointerUI)).WithParent(rightController.transform).AddComponent<LaserPointerNew>();

            vrCamera = new GameObject(nameof(vrCamera)).WithParent(transform).AddComponent<Camera>();
            vrCamera.gameObject.tag = "MainCamera";

            SetupControllerModels();

            // Connect Input module and layer pointer together
            FPSInputModule fpsInput = FindObjectOfType<FPSInputModule>();
            laserPointer.inputModule = fpsInput;
            laserPointerLeft.inputModule = fpsInput;
            // CoroutineHost.StartCoroutine(SwapInputModule());

            parentConstraint = gameObject.AddComponent<ParentConstraint>();
            parentConstraint.constraintActive = false;
        }

        private void SetupControllerModels()
        {
            // Create two cubes to show controller positions
            // TODO: Replace with actual models from steamvr
            modelR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelR.transform.parent = leftController.transform;
            modelR.transform.localPosition.Set(0, 0, 0);
            modelR.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Object.DestroyImmediate(modelR.GetComponent<BoxCollider>());

            modelL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelL.transform.parent = rightController.transform;
            modelL.transform.localPosition.Set(0, 0, 0);
            modelL.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Object.DestroyImmediate(modelL.GetComponent<BoxCollider>());
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

        private void UpdateSteamVRControllers() {
            rightController.transform.localPosition = VRInputManager.RightControllerPosition;
            leftController.transform.localPosition = VRInputManager.LeftControllerPosition;
            rightController.transform.localRotation = VRInputManager.RightControllerRotation;
            leftController.transform.localRotation = VRInputManager.LeftControllerRotation;

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
                var laserPointerCamera = VRCameraRig.instance.laserPointer.eventCamera;
                __result = laserPointerCamera;
            }
        }
    }
    #endregion

}