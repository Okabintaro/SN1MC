using UnityEngine;
namespace SN1MC.Controls
{
    // Creates a laser pointer 
    // Attach it to a new gameobject to make a laserpointer
    public class LaserPointerNew : MonoBehaviour
    {
        // Properties
        public Color color;
        public static float thickness = 0.005f;
        public float defaultLength = 5.0f;

        // "Prefab Objects/Components"
        public GameObject pointerDot;

        public LineRenderer lineRenderer;
        public Camera eventCamera;
        public FPSInputModule inputModule;

        public bool doWorldRaycasts = true;
        public bool useUILayer = false;

        void Start()
        {
            Material newMaterial = new Material(ShaderManager.preloadedShaders.DebugDisplaySolid);
            newMaterial.SetColor(ShaderPropertyID._Color, VRCustomOptionsMenu.laserPointerColor);

            // Setup camera used for raycasting on canvases
            eventCamera = gameObject.AddComponent<Camera>();
            eventCamera.stereoTargetEye = StereoTargetEyeMask.None;
            eventCamera.nearClipPlane = 0.01f;
            eventCamera.farClipPlane = 10.0f;
            eventCamera.fieldOfView = 1.0f;
            eventCamera.enabled = false;

            // Setup PointerDot at the end
            pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointerDot.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            pointerDot.transform.parent = transform;
            pointerDot.GetComponent<SphereCollider>().enabled = false;
            pointerDot.GetComponent<Renderer>().material = newMaterial;

            // Setup Line Renderer
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = newMaterial;
            lineRenderer.startColor = new Color(0f, 1f, 1f, 1f);
            lineRenderer.endColor = new Color(1f, 0f, 0f, 1f);
            lineRenderer.startWidth = 0.004f;
            lineRenderer.endWidth = 0.005f;

            if (useUILayer) {
                pointerDot.layer = LayerMask.NameToLayer("UI");
                lineRenderer.gameObject.layer = LayerMask.NameToLayer("UI");
            }
        }

        public void SetEnd(Vector3 end)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, end);
            pointerDot.transform.position = end;
        }

        private bool RayCastGameObjects(out RaycastHit hit)
        {
            // TODO: Not sure if needed?
            float maxDistance = inputModule.maxInteractionDistance;
            Ray raycast = new Ray(transform.position, transform.forward);
            var layerNames = new string[] { "SubRigidbodyExclude", "Interior", "TerrainCollider", "Trigger", "UI", "Useable", "Default" };
            var layerMask = LayerMask.GetMask(layerNames);
            // TODO: Use maxDistance
            return Physics.Raycast(raycast, out hit, maxDistance, layerMask);
        }

        void Update()
        {
            if (inputModule == null)
            {
                return;
            }
            var hitDistance = inputModule.lastRaycastResult.distance;

            float length = defaultLength;
            if (hitDistance != 0)
            {
                length = hitDistance;
                VRInputManager.SwitchToUIBinding();
            }
            else if(doWorldRaycasts)
            {
                RaycastHit hit;
                if (RayCastGameObjects(out hit)) {
                    length = hit.distance;
                }
                VRInputManager.SwitchToGameBinding();
            }
            Vector3 endPos = transform.position + (transform.forward * length);

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, endPos);
            pointerDot.transform.position = endPos;
        }

        // void UpdateLaserPointer()
        // {
        //     Ray raycast = new Ray(transform.position, transform.forward);
        //     var layerNames = new string[] { "Default", "Interior", "TerrainCollider", "Trigger", "UI", "Useable" };
        //     var layerMask = LayerMask.GetMask(layerNames);
        //     bool triggerHit = Physics.Raycast(raycast, out triggerObject, layerMask);
        //     lineRenderer.SetPosition(0, transform.position);
        //     if (FPSInputModule.current.lastRaycastResult.isValid)
        //     {
        //         int layer = FPSInputModule.current.lastRaycastResult.gameObject.layer;
        //         var screenPointToRay = MainCamera.camera.ScreenPointToRay(FPSInputModule.current.lastRaycastResult.screenPosition).GetPoint(FPSInputModule.current.lastRaycastResult.distance);
        //         var screenPointToRay1 = FPSInputModule.current.lastRaycastResult.module.eventCamera.ScreenPointToRay(FPSInputModule.current.lastRaycastResult.worldPosition).GetPoint(FPSInputModule.current.lastRaycastResult.distance);

        //         if (layer == 0 && !VRHandsController.Started)
        //         {
        //             lineRenderer.gameObject.layer = LayerID.UI;
        //         }
        //         else
        //         {
        //             lineRenderer.gameObject.layer = LayerID.Default;
        //         }
        //         Vector3 pos = VRHandsController.rightController.transform.position + VRHandsController.rightController.transform.forward * FPSInputModule.current.maxInteractionDistance;
        //         lineRenderer.SetPosition(1, Vector3.MoveTowards(transform.position, pos, FPSInputModule.current.maxInteractionDistance));
        //         FPSInputModule.current.lastRaycastResult.Clear();
        //     }
        // }


    }
}




