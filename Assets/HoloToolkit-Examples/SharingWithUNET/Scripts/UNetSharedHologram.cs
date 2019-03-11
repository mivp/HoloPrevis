// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using UnityEngine;
using UnityEngine.Networking;

namespace HoloToolkit.Unity.SharingWithUNET
{
    public class UNetSharedHologram : NetworkBehaviour, IInputClickHandler, INavigationHandler, IManipulationHandler
    {
        
        /*
        [SyncVar(hook = "xformchange")]
        private Vector3 localPosition;

        private void xformchange(Vector3 update)
        {
            Debug.Log(localPosition + " xform change " + update);
            localPosition = update;
        }
        */

        /// <summary>
        /// The position relative to the shared world anchor.
        /// </summary>
        [SyncVar]
        private Vector3 localPosition;

        // <summary>
        /// The rotation relative to the shared world anchor.
        /// </summary>
        [SyncVar]
        private Quaternion localRotation;

        // <summary>
        /// The scale relative to the shared world anchor.
        /// </summary>
        [SyncVar]
        private Vector3 localScale;


        [SerializeField]
        private float RotationSensitivity = 10.0f;

        [SerializeField]
        float ResizeSpeedFactor = 1.5f;

        [SerializeField]
        float ResizeScaleFactor = 1.5f;

        private Vector3 lastScale;

        /// <summary>
        /// Sets the localPosition and localRotation on clients.
        /// </summary>
        /// <param name="postion">the localPosition to set</param>
        /// <param name="rotation">the localRotation to set</param>
        [Command]
        public void CmdTransform(Vector3 postion, Quaternion rotation, Vector3 scale)
        {
            if (!isLocalPlayer)
            {
                localPosition = postion;
                localRotation = rotation;
                localScale = scale;
            }
        }

        private bool Moving = false;
        private int layerMask;
        private InputManager inputManager;
        public Vector3 movementOffset = Vector3.zero;
        private bool isOpaque = false;

        void INavigationHandler.OnNavigationStarted(NavigationEventData eventData)
        {
            if (MyUIManager.Instance.CurrentModelEditMode == MyUIManager.ModelEditType.Rotate)
            {
                InputManager.Instance.PushModalInputHandler(gameObject);
            }
        }

        void INavigationHandler.OnNavigationUpdated(NavigationEventData eventData)
        {
            if(MyUIManager.Instance.CurrentModelEditMode == MyUIManager.ModelEditType.Rotate)
            {
                // This will help control the amount of rotation.
                float rotationFactor = eventData.NormalizedOffset.x * RotationSensitivity;

                // 2.c: transform.Rotate around the Y axis using rotationFactor.
                transform.Rotate(new Vector3(0, -1 * rotationFactor, 0));

                UpdateTransformNetwork();
            }
        }

        void INavigationHandler.OnNavigationCompleted(NavigationEventData eventData)
        {
            InputManager.Instance.PopModalInputHandler();
        }

        void INavigationHandler.OnNavigationCanceled(NavigationEventData eventData)
        {
            InputManager.Instance.PopModalInputHandler();
        }

        void IManipulationHandler.OnManipulationStarted(ManipulationEventData eventData)
        {
            if (MyUIManager.Instance.CurrentModelEditMode == MyUIManager.ModelEditType.Scale)
            {
                InputManager.Instance.PushModalInputHandler(gameObject);
                lastScale = transform.localScale;
            }
        }

        void IManipulationHandler.OnManipulationUpdated(ManipulationEventData eventData)
        {
            if (MyUIManager.Instance.CurrentModelEditMode == MyUIManager.ModelEditType.Scale)
            {
                // 4.a: Make this transform's position be the manipulationOriginalPosition + eventData.CumulativeDelta
                float resizeX, resizeY, resizeZ;
                resizeX = resizeY = resizeZ = eventData.CumulativeDelta.x * ResizeScaleFactor;
                float MinScale = 0.4f;
                float MaxScale = 2.5f;
                resizeX = Mathf.Clamp(lastScale.x + resizeX, MinScale, MaxScale);
                resizeY = Mathf.Clamp(lastScale.y + resizeY, MinScale, MaxScale);
                resizeZ = Mathf.Clamp(lastScale.z + resizeZ, MinScale, MaxScale);

                transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(resizeX, resizeY, resizeZ), 0.1f); // resize speed factor
                UpdateTransformNetwork();
            }
        }

        void IManipulationHandler.OnManipulationCompleted(ManipulationEventData eventData)
        {
            InputManager.Instance.PopModalInputHandler();
        }

        void IManipulationHandler.OnManipulationCanceled(ManipulationEventData eventData)
        {
            InputManager.Instance.PopModalInputHandler();
        }

        // Use this for initialization
        private void Start()
        {
#if UNITY_WSA
#if UNITY_2017_2_OR_NEWER
            isOpaque = UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque;
#else
            isOpaque = !UnityEngine.VR.VRDevice.isPresent;
#endif
#endif
            transform.SetParent(SharedCollection.Instance.transform, true);
            if (isServer)
            {
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            layerMask = SpatialMappingManager.Instance.LayerMask;
            inputManager = InputManager.Instance;

        }

        // Update is called once per frame
        private void Update()
        {
            if (Moving)
            {
                transform.position = Vector3.Lerp(transform.position, ProposeTransformPosition(), 0.2f);
            }
            else
            {

                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
            
        }

        private Vector3 ProposeTransformPosition()
        {
            // Put the model 3m in front of the user.
            Vector3 retval = Camera.main.transform.position + Camera.main.transform.forward * 3 + movementOffset;
            RaycastHit hitInfo;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, 5.0f, layerMask))
            {
                retval = hitInfo.point + movementOffset;
            }
            return retval;
        }

        private void UpdateTransformNetwork()
        {
            // Depending on if you are host or client, either setting the SyncVar (host) 
            // or calling the Cmd (client) will update the other users in the session.
            // So we have to do both.
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SendSharedTransform(gameObject, localPosition, localRotation, localScale);
            }
        }

        public void OnInputClicked(InputClickedEventData eventData)
        {
            if (isOpaque == false && MyUIManager.Instance.CurrentModelEditMode == MyUIManager.ModelEditType.Move)
            {
                Moving = !Moving;
                if (Moving)
                {
                    inputManager.AddGlobalListener(gameObject);
                    if (SpatialMappingManager.Instance != null)
                    {
                        SpatialMappingManager.Instance.DrawVisualMeshes = true;
                    }
                }
                else
                {
                    inputManager.RemoveGlobalListener(gameObject);
                    if (SpatialMappingManager.Instance != null)
                    {
                        SpatialMappingManager.Instance.DrawVisualMeshes = false;
                    }
                    UpdateTransformNetwork();
                }

                eventData.Use();
            }
        }
    }
}
