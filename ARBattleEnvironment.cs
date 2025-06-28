using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace BrawlAnything.AR
{
    /// <summary>
    /// Manages the AR battle environment, including plane detection, character placement,
    /// and battle arena setup.
    /// </summary>
    public class ARBattleEnvironment : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;
        
        [Header("Battle Arena")]
        [SerializeField] private GameObject arenaIndicatorPrefab;
        [SerializeField] private GameObject arenaPrefab;
        [SerializeField] private float arenaSize = 3.0f;
        [SerializeField] private float minPlaneSize = 2.0f;
        
        [Header("Character Placement")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform opponentSpawnPoint;
        [SerializeField] private float characterScale = 0.5f;
        
        [Header("Battle Effects")]
        [SerializeField] private ParticleSystem[] battleEffects;
        [SerializeField] private GameObject boundaryVisualizer;
        
        // Private variables
        private GameObject arenaIndicator;
        private GameObject currentArena;
        private bool arenaPlaced = false;
        private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
        private Dictionary<int, GameObject> characterInstances = new Dictionary<int, GameObject>();
        
        // Events
        public event Action<Vector3> OnArenaPlaced;
        public event Action<bool> OnSuitablePlaneFound;
        
        private void Awake()
        {
            // Create arena indicator
            arenaIndicator = Instantiate(arenaIndicatorPrefab);
            arenaIndicator.SetActive(false);
        }
        
        private void OnEnable()
        {
            planeManager.planesChanged += OnPlanesChanged;
        }
        
        private void OnDisable()
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
        
        private void Update()
        {
            if (arenaPlaced)
                return;
                
            // Check for touch input to place arena
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                if (TryGetTouchPosition(out Vector2 touchPosition))
                {
                    if (raycastManager.Raycast(touchPosition, raycastHits, TrackableType.PlaneWithinPolygon))
                    {
                        // Check if the plane is large enough
                        ARPlane plane = planeManager.GetPlane(raycastHits[0].trackableId);
                        if (IsPlaneSuitable(plane))
                        {
                            PlaceArena(raycastHits[0].pose.position);
                        }
                        else
                        {
                            Debug.Log("Selected plane is too small for battle arena");
                        }
                    }
                }
            }
            
            // Update arena indicator position
            UpdateArenaIndicator();
        }
        
        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            if (arenaPlaced)
                return;
                
            // Check if any of the planes are suitable for battle arena
            bool foundSuitablePlane = false;
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (IsPlaneSuitable(plane))
                {
                    foundSuitablePlane = true;
                    break;
                }
            }
            
            OnSuitablePlaneFound?.Invoke(foundSuitablePlane);
        }
        
        private bool IsPlaneSuitable(ARPlane plane)
        {
            // Check if plane is horizontal
            if (plane.alignment != PlaneAlignment.HorizontalUp)
                return false;
                
            // Check if plane is large enough
            float planeArea = plane.size.x * plane.size.y;
            float minArea = minPlaneSize * minPlaneSize;
            
            return planeArea >= minArea;
        }
        
        private void UpdateArenaIndicator()
        {
            if (arenaPlaced)
                return;
                
            // Raycast from center of screen
            Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
            if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                ARPlane plane = planeManager.GetPlane(raycastHits[0].trackableId);
                bool isSuitable = IsPlaneSuitable(plane);
                
                arenaIndicator.SetActive(isSuitable);
                if (isSuitable)
                {
                    // Position and rotate indicator
                    Pose hitPose = raycastHits[0].pose;
                    arenaIndicator.transform.position = hitPose.position;
                    arenaIndicator.transform.rotation = hitPose.rotation;
                    
                    // Scale indicator to match arena size
                    arenaIndicator.transform.localScale = new Vector3(arenaSize, 0.01f, arenaSize);
                }
            }
            else
            {
                arenaIndicator.SetActive(false);
            }
        }
        
        private void PlaceArena(Vector3 position)
        {
            // Create battle arena
            currentArena = Instantiate(arenaPrefab, position, Quaternion.identity);
            currentArena.transform.localScale = new Vector3(arenaSize, 1.0f, arenaSize);
            
            // Hide indicator
            arenaIndicator.SetActive(false);
            
            // Set arena placed flag
            arenaPlaced = true;
            
            // Enable boundary visualizer
            if (boundaryVisualizer != null)
            {
                boundaryVisualizer.transform.position = position;
                boundaryVisualizer.transform.localScale = new Vector3(arenaSize, 1.0f, arenaSize);
                boundaryVisualizer.SetActive(true);
            }
            
            // Notify listeners
            OnArenaPlaced?.Invoke(position);
            
            // Disable plane manager to stop tracking new planes
            planeManager.enabled = false;
            
            // Hide existing planes
            foreach (ARPlane plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Places a character in the battle arena.
        /// </summary>
        /// <param name="characterId">The ID of the character to place</param>
        /// <param name="characterPrefab">The character prefab to instantiate</param>
        /// <param name="isPlayer">Whether this is the player's character or opponent</param>
        /// <returns>The instantiated character GameObject</returns>
        public GameObject PlaceCharacter(int characterId, GameObject characterPrefab, bool isPlayer)
        {
            if (!arenaPlaced)
            {
                Debug.LogError("Cannot place character before arena is placed");
                return null;
            }
            
            // Determine spawn position
            Transform spawnPoint = isPlayer ? playerSpawnPoint : opponentSpawnPoint;
            if (spawnPoint == null)
            {
                // Use default positions if spawn points not set
                Vector3 arenaPos = currentArena.transform.position;
                Vector3 spawnPos = arenaPos + (isPlayer ? new Vector3(-arenaSize/3, 0, 0) : new Vector3(arenaSize/3, 0, 0));
                
                // Instantiate character
                GameObject character = Instantiate(characterPrefab, spawnPos, Quaternion.identity);
                
                // Make character face center
                character.transform.LookAt(new Vector3(arenaPos.x, character.transform.position.y, arenaPos.z));
                
                // Scale character
                character.transform.localScale = Vector3.one * characterScale;
                
                // Store reference
                characterInstances[characterId] = character;
                
                return character;
            }
            else
            {
                // Instantiate character at spawn point
                GameObject character = Instantiate(characterPrefab, spawnPoint.position, spawnPoint.rotation);
                
                // Scale character
                character.transform.localScale = Vector3.one * characterScale;
                
                // Store reference
                characterInstances[characterId] = character;
                
                return character;
            }
        }
        
        /// <summary>
        /// Gets a placed character by ID.
        /// </summary>
        /// <param name="characterId">The ID of the character to get</param>
        /// <returns>The character GameObject, or null if not found</returns>
        public GameObject GetCharacter(int characterId)
        {
            if (characterInstances.TryGetValue(characterId, out GameObject character))
            {
                return character;
            }
            
            return null;
        }
        
        /// <summary>
        /// Plays a battle effect at the specified position.
        /// </summary>
        /// <param name="effectIndex">Index of the effect to play</param>
        /// <param name="position">Position to play the effect at</param>
        public void PlayBattleEffect(int effectIndex, Vector3 position)
        {
            if (effectIndex < 0 || effectIndex >= battleEffects.Length)
            {
                Debug.LogError($"Invalid battle effect index: {effectIndex}");
                return;
            }
            
            ParticleSystem effect = battleEffects[effectIndex];
            effect.transform.position = position;
            effect.Play();
        }
        
        /// <summary>
        /// Resets the battle environment, removing the arena and all characters.
        /// </summary>
        public void ResetEnvironment()
        {
            // Destroy arena
            if (currentArena != null)
            {
                Destroy(currentArena);
                currentArena = null;
            }
            
            // Destroy all characters
            foreach (var character in characterInstances.Values)
            {
                Destroy(character);
            }
            characterInstances.Clear();
            
            // Reset flags
            arenaPlaced = false;
            
            // Hide boundary visualizer
            if (boundaryVisualizer != null)
            {
                boundaryVisualizer.SetActive(false);
            }
            
            // Re-enable plane manager
            planeManager.enabled = true;
            
            // Show planes
            foreach (ARPlane plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }
        
        private bool TryGetTouchPosition(out Vector2 touchPosition)
        {
            if (Input.touchCount > 0)
            {
                touchPosition = Input.GetTouch(0).position;
                return true;
            }
            
            touchPosition = default;
            return false;
        }
    }
}
