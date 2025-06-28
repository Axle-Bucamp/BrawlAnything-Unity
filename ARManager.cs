using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using BrawlAnything.Camera;


namespace BrawlAnything.AR
{
    /// <summary>
    /// Manages AR session and placement of objects in AR space
    /// </summary>
    public class ARManager : MonoBehaviour
    {
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARSessionOrigin arSessionOrigin;
        [SerializeField] private ARRaycastManager arRaycastManager;
        [SerializeField] private ARPlaneManager arPlaneManager;
        
        [SerializeField] private GameObject placementIndicator;
        [SerializeField] private GameObject characterPrefab;
        
        private Pose placementPose;
        private bool placementPoseIsValid = false;
        
        // Event for when a character is placed
        public event Action<GameObject> OnCharacterPlaced;
        
        private void Awake()
        {
            // Find components if not assigned
            if (arSession == null) arSession = FindObjectOfType<ARSession>();
            if (arSessionOrigin == null) arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            if (arRaycastManager == null) arRaycastManager = FindObjectOfType<ARRaycastManager>();
            if (arPlaneManager == null) arPlaneManager = FindObjectOfType<ARPlaneManager>();
        }
        
        private void Start()
        {
            // Initialize AR session
            StartCoroutine(CheckARSupport());
        }
        
        private IEnumerator CheckARSupport()
        {
            yield return ARSession.CheckAvailability();
            
            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                yield return ARSession.Install();
            }
            
            if (ARSession.state == ARSessionState.Ready || ARSession.state == ARSessionState.SessionInitializing)
            {
                arSession.enabled = true;
                arPlaneManager.enabled = true;
                
                // Enable placement indicator
                if (placementIndicator != null)
                {
                    placementIndicator.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("AR is not supported on this device");
            }
        }
        
        private void Update()
        {
            UpdatePlacementPose();
            UpdatePlacementIndicator();
            
            // Check for tap to place character
            if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                PlaceCharacter();
            }
        }
        
        private void UpdatePlacementPose()
        {
            var screenCenter = UnityEngine.Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
            var hits = new List<ARRaycastHit>();
            
            arRaycastManager.Raycast(screenCenter, hits, TrackableType.Planes);
            
            placementPoseIsValid = hits.Count > 0;
            if (placementPoseIsValid)
            {
                placementPose = hits[0].pose;
                
                // Adjust pose to be aligned with the plane
                var cameraForward = UnityEngine.Camera.main.transform.forward;
                var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                placementPose.rotation = Quaternion.LookRotation(cameraBearing);
            }
        }
        
        private void UpdatePlacementIndicator()
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(placementPoseIsValid);
                if (placementPoseIsValid)
                {
                    placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
                }
            }
        }
        
        public void PlaceCharacter()
        {
            if (placementPoseIsValid && characterPrefab != null)
            {
                GameObject character = Instantiate(characterPrefab, placementPose.position, placementPose.rotation);
                OnCharacterPlaced?.Invoke(character);
            }
        }
        
        public void SetCharacterPrefab(GameObject prefab)
        {
            characterPrefab = prefab;
        }
        
        public void TogglePlaneDetection(bool enabled)
        {
            arPlaneManager.enabled = enabled;
            
            // Hide/show existing planes
            foreach (var plane in arPlaneManager.trackables)
            {
                plane.gameObject.SetActive(enabled);
            }
        }
    }
}
