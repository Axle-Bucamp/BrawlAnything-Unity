using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using BrawlAnything.Network;

namespace BrawlAnything.AR
{
    public class SharedARExperience : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private ARRaycastManager raycastManager;

        [Header("Synchronization Settings")]
        [SerializeField] private float syncInterval = 1.0f;
        [SerializeField] private bool autoSyncPlanes = true;
        [SerializeField] private bool autoSyncAnchors = true;
        [SerializeField] private int maxPlanesToSync = 3;

        [Header("Battle Arena")]
        [SerializeField] private GameObject arenaPrefab;
        [SerializeField] private float arenaScale = 1.0f;
        [SerializeField] private Vector3 arenaOffset = new Vector3(0, 0.01f, 0);

        private int battleId;
        private float lastSyncTime;
        private bool isSyncActive = false;

        private GameObject arenaInstance;
        private ARAnchor arenaAnchor;
        private string arenaAnchorId;
        private bool isArenaHost = false;

        private Dictionary<string, ARPlane> syncedPlanes = new();
        private Dictionary<string, ARAnchor> syncedAnchors = new();
        private Dictionary<string, GameObject> syncedObjects = new();

        public event Action<GameObject> OnArenaCreated;
        public event Action<List<ARPlane>> OnPlanesUpdated;
        public event Action<Dictionary<string, object>> OnARDataReceived;

        private static SharedARExperience _instance;
        public static SharedARExperience Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<SharedARExperience>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (arSession == null) arSession = FindObjectOfType<ARSession>();
            if (planeManager == null) planeManager = FindObjectOfType<ARPlaneManager>();
            if (anchorManager == null) anchorManager = FindObjectOfType<ARAnchorManager>();
            if (raycastManager == null) raycastManager = FindObjectOfType<ARRaycastManager>();

            if (planeManager != null)
                planeManager.planesChanged += HandlePlanesChanged;

            if (anchorManager != null)
                anchorManager.anchorsChanged += HandleAnchorsChanged;
        }

        private void OnEnable()
        {
            if (MultiplayerClient.Instance != null)
            {
                MultiplayerClient.Instance.RegisterMessageHandler("ar_sync", HandleARSyncMessage);
                MultiplayerClient.Instance.RegisterMessageHandler("game_start", HandleGameStartMessage);
                MultiplayerClient.Instance.RegisterMessageHandler("game_end", HandleGameEndMessage);
            }
        }

        private void OnDisable()
        {
            if (MultiplayerClient.Instance != null)
            {
                MultiplayerClient.Instance.UnregisterMessageHandler("ar_sync");
                MultiplayerClient.Instance.UnregisterMessageHandler("game_start");
                MultiplayerClient.Instance.UnregisterMessageHandler("game_end");
            }

            if (planeManager != null)
                planeManager.planesChanged -= HandlePlanesChanged;

            if (anchorManager != null)
                anchorManager.anchorsChanged -= HandleAnchorsChanged;
        }

        private void Update()
        {
            if (!isSyncActive) return;

            if (Time.time - lastSyncTime >= syncInterval)
            {
                lastSyncTime = Time.time;
                SyncARData();
            }
        }

        // ✅ Correct signature for ARPlaneManager.planesChanged
        private void HandlePlanesChanged(ARPlanesChangedEventArgs args)
        {
            if (args.added.Count > 0 || args.updated.Count > 0 || args.removed.Count > 0)
            {
                List<ARPlane> updatedPlanes = new();
                updatedPlanes.AddRange(args.added);
                updatedPlanes.AddRange(args.updated);
                OnPlanesUpdated?.Invoke(updatedPlanes);
            }
        }

        // ✅ Correct signature for ARAnchorManager.anchorsChanged
        private void HandleAnchorsChanged(ARAnchorsChangedEventArgs args)
        {
            foreach (var added in args.added)
            {
                Debug.Log($"Anchor added: {added.trackableId}");
            }

            foreach (var removed in args.removed)
            {
                Debug.Log($"Anchor removed: {removed.trackableId}");
                syncedAnchors.Remove(removed.trackableId.ToString());
            }
        }

        private void SyncARData()
        {
            if (isArenaHost && arenaAnchor != null)
            {
                SyncArena();
            }

            if (autoSyncPlanes)
            {
                SyncPlanes();
            }

            if (autoSyncAnchors)
            {
                SyncAnchors();
            }
        }

        // Placeholder for actual implementations
        private void SyncArena() { }
        private void SyncPlanes() { }
        private void SyncAnchors() { }

        private void HandleARSyncMessage(Dictionary<string, object> payload) { }
        private void HandleGameStartMessage(Dictionary<string, object> payload) { }
        private void HandleGameEndMessage(Dictionary<string, object> payload) { }
    }
}
