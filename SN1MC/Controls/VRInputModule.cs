using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;

namespace SN1MC.Controls
{
	extern alias SteamVRActions;
	extern alias SteamVRRef;

    public class VRInputModule : BaseInputModule
    {
        public Camera eventCamera;

        //public SteamVRRef.Valve.VR.SteamVR_Input_Sources targetSource;
        //public SteamVRRef.Valve.VR.SteamVR_Action_Boolean clickAction;

        private GameObject currentObject;
        private PointerEventData eventData;
        public PointerEventData EventData { get => eventData; }

        protected override void Awake()
        {
            base.Awake();
            eventData = new PointerEventData(eventSystem);
        }

        public override void Process()
        {
            eventData.Reset();

            // Raycast using event system and controller camera at the middle of the cameras frustum
            eventData.position = new Vector2(eventCamera.pixelWidth / 2, eventCamera.pixelHeight / 2);
            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            eventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            currentObject = eventData.pointerCurrentRaycast.gameObject;

            // Clear
            m_RaycastResultCache.Clear();

            // HandleHover
            HandlePointerExitAndEnter(eventData, currentObject);
            
            // TODO: Bad performance and style
            VRInputManager vrInput = new VRInputManager();
            // Press
            if (vrInput.GetButtonDown(GameInput.Button.UISubmit, SteamVRRef.Valve.VR.SteamVR_Input_Sources.Any))
            {

                ErrorMessage.AddDebug("Button Down");
                ProcessPress(eventData);
            }

            // Release
            if (vrInput.GetButtonUp(GameInput.Button.UISubmit, SteamVRRef.Valve.VR.SteamVR_Input_Sources.Any))
            {
                // ErrorMessage.AddDebug("Button Up");
                ProcessRelease(eventData);
            }
        }

        private void ProcessPress(PointerEventData data)
        {
            // Debug.Log($"Press: {data}");

            data.pointerPressRaycast = data.pointerCurrentRaycast;
            GameObject pointerPress = ExecuteEvents.ExecuteHierarchy(currentObject, data, ExecuteEvents.pointerDownHandler);
            if (pointerPress == null)
            {
                pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentObject);
            }

            data.pressPosition = data.position;
            data.pointerPress = pointerPress;
            data.rawPointerPress = currentObject;
        }

        private void ProcessRelease(PointerEventData data)
        {
            // Debug.Log($"Release: {data}");
            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);
            GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentObject);

            // Why do it again here?
            if (data.pointerPress == pointerUpHandler)
            {
                // Debug.Log($"Clicking: {data.pointerPress}");
                ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
            }

            eventSystem.SetSelectedGameObject(null, data);
            data.pressPosition = Vector2.zero;
            data.pointerPress = null;
            data.rawPointerPress = null;
        }
    }
}