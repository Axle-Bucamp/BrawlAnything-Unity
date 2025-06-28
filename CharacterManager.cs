using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using static BrawlAnything.Models.CharacterManager;
using BrawlAnything.Network;


namespace BrawlAnything.Models
{
    /// <summary>
    /// Manages character models, animations and game state
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        [SerializeField] private GameObject characterContainer;
        [SerializeField] private GameObject defaultCharacterPrefab;
        
        private Dictionary<int, GameObject> characterInstances = new Dictionary<int, GameObject>();
        private CharacterData activeCharacter;
        
        // Singleton instance
        private static CharacterManager _instance;
        public static CharacterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CharacterManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterManager");
                        _instance = go.AddComponent<CharacterManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }



        [Serializable]
        public class CharacterResponse
        {
            public List<CharacterData> characters;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (characterContainer == null)
            {
                characterContainer = new GameObject("CharacterContainer");
                characterContainer.transform.SetParent(transform);
            }
        }
        
        public void LoadUserCharacters(Action<List<CharacterData>> callback)
        {
            StartCoroutine(LoadUserCharactersCoroutine(callback));
        }
        
        private IEnumerator LoadUserCharactersCoroutine(Action<List<CharacterData>> callback)
        {
            yield return StartCoroutine(APIClient.Instance.GetUserCharacters((characters, error) => {
                if (string.IsNullOrEmpty(error) && characters != null)
                {
                    callback?.Invoke(characters);
                }
                else
                {
                    Debug.LogError($"Failed to load characters: {error}");
                    callback?.Invoke(new List<CharacterData>());
                }
            }));
        }

        public void SetActiveCharacter(CharacterData character)
        {
            if (character == null)
            {
                Debug.LogWarning("Attempted to set a null character as active.");
                return;
            }

            activeCharacter = character;

            var arManager = FindObjectOfType<AR.ARManager>();
            if (arManager != null && defaultCharacterPrefab != null)
            {
                arManager.SetCharacterPrefab(defaultCharacterPrefab);
            }
            else
            {
                Debug.LogWarning("AR Manager or defaultCharacterPrefab is missing.");
            }
        }

        public CharacterData GetActiveCharacter()
        {
            return activeCharacter;
        }
        
        public void GenerateCharacterModel(int characterId, byte[] imageData, Action<bool, string> callback)
        {
            StartCoroutine(GenerateCharacterModelCoroutine(characterId, imageData, callback));
        }
        
        private IEnumerator GenerateCharacterModelCoroutine(int characterId, byte[] imageData, Action<bool, string> callback)
        {
            string requestId = null;
            
            // Start model generation
            yield return StartCoroutine(APIClient.Instance.GenerateModel(characterId, imageData, (id, error) => {
                if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(id))
                {
                    requestId = id;
                }
                else
                {
                    Debug.LogError($"Failed to start model generation: {error}");
                    callback?.Invoke(false, error);
                }
            }));
            
            if (string.IsNullOrEmpty(requestId))
            {
                yield break;
            }
            
            // Poll for completion
            bool isComplete = false;
            float timeout = 300f; // 5 minutes timeout
            float elapsed = 0f;
            
            while (!isComplete && elapsed < timeout)
            {
                yield return StartCoroutine(APIClient.Instance.CheckModelGenerationStatus(requestId, (response, error) => {
                    if (string.IsNullOrEmpty(error) && response != null)
                    {
                        if (response.status == "completed")
                        {
                            isComplete = true;
                            callback?.Invoke(true, null);
                        }
                        else if (response.status == "failed")
                        {
                            isComplete = true;
                            callback?.Invoke(false, response.error);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Failed to check model generation status: {error}");
                    }
                }));
                
                if (!isComplete)
                {
                    yield return new WaitForSeconds(5f);
                    elapsed += 5f;
                }
            }
            
            if (!isComplete)
            {
                callback?.Invoke(false, "Timeout waiting for model generation");
            }
        }
        
        public void GenerateCharacterAnimation(int characterId, int animationId, string modelUrl, string animationType, byte[] maskData, Action<bool, string> callback)
        {
            StartCoroutine(GenerateCharacterAnimationCoroutine(characterId, animationId, modelUrl, animationType, maskData, callback));
        }
        
        private IEnumerator GenerateCharacterAnimationCoroutine(int characterId, int animationId, string modelUrl, string animationType, byte[] maskData, Action<bool, string> callback)
        {
            string requestId = null;
            
            // Start animation generation
            yield return StartCoroutine(APIClient.Instance.GenerateAnimation(characterId, animationId, modelUrl, animationType, maskData, (id, error) => {
                if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(id))
                {
                    requestId = id;
                }
                else
                {
                    Debug.LogError($"Failed to start animation generation: {error}");
                    callback?.Invoke(false, error);
                }
            }));
            
            if (string.IsNullOrEmpty(requestId))
            {
                yield break;
            }
            
            // Poll for completion
            bool isComplete = false;
            float timeout = 300f; // 5 minutes timeout
            float elapsed = 0f;
            
            while (!isComplete && elapsed < timeout)
            {
                yield return StartCoroutine(APIClient.Instance.CheckAnimationGenerationStatus(requestId, (response, error) => {
                    if (string.IsNullOrEmpty(error) && response != null)
                    {
                        if (response.status == "completed")
                        {
                            isComplete = true;
                            callback?.Invoke(true, null);
                        }
                        else if (response.status == "failed")
                        {
                            isComplete = true;
                            callback?.Invoke(false, response.error);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Failed to check animation generation status: {error}");
                    }
                }));
                
                if (!isComplete)
                {
                    yield return new WaitForSeconds(5f);
                    elapsed += 5f;
                }
            }
            
            if (!isComplete)
            {
                callback?.Invoke(false, "Timeout waiting for animation generation");
            }
        }

        public void InstantiateCharacter(CharacterData character, Vector3 position, Quaternion rotation)
        {
            if (character == null)
            {
                Debug.LogWarning("Cannot instantiate null character.");
                return;
            }

            if (characterInstances.ContainsKey(character.id))
            {
                characterInstances[character.id].transform.SetPositionAndRotation(position, rotation);
                return;
            }

            if (defaultCharacterPrefab == null)
            {
                Debug.LogError("DefaultCharacterPrefab is not assigned.");
                return;
            }

            GameObject characterInstance = Instantiate(defaultCharacterPrefab, position, rotation, characterContainer.transform);
            characterInstance.name = $"Character_{character.id}_{character.name}";

            var behaviour = characterInstance.AddComponent<CharacterBehaviour>();
            behaviour.Initialize(character);

            characterInstances[character.id] = characterInstance;
        }


        public void RemoveCharacter(int characterId)
        {
            if (characterInstances.TryGetValue(characterId, out GameObject instance))
            {
                Destroy(instance);
                characterInstances.Remove(characterId);
            }
            else
            {
                Debug.LogWarning($"No character instance found with ID {characterId}.");
            }
        }

        public void ClearAllCharacters()
        {
            foreach (var characterInstance in characterInstances.Values)
            {
                Destroy(characterInstance);
            }
            
            characterInstances.Clear();
        }
    }

    [Serializable]
    public class CharacterData
    {
        public int id;
        public string name;
        public string model_url;
        public string description;
        public int owner_id;
        public string image_url;
        public bool is_public;
        public string created_at;
        public string updated_at;
        public List<Animation> animations;
        public string color_primary;
        public string color_secondary;
        public string color_accent;
    }
    /// <summary>
    /// Represents a UI card for displaying a character in the character selection screen.
    /// </summary>
    public class CharacterCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private RawImage characterPreviewImage;
        [SerializeField] private Button selectButton;

        public event Action<CharacterData> OnSelected;

        private CharacterData characterData;

        /// <summary>
        /// Initializes the character card with character data and optionally loads the preview image.
        /// </summary>
        /// <param name="character">Character data object</param>
        public void Initialize(CharacterData character)
        {
            characterData = character;

            if (characterNameText != null)
                characterNameText.text = character.name;

            if (!string.IsNullOrEmpty(character.image_url))
                StartCoroutine(LoadPreviewImage(character.image_url));
            else
                Debug.LogWarning("Character image_url is empty.");

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => OnSelected?.Invoke(characterData));
            }
        }

        /// <summary>
        /// Asynchronously loads a preview image from the given URL.
        /// </summary>
        private IEnumerator LoadPreviewImage(string url)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (characterPreviewImage != null)
                        characterPreviewImage.texture = texture;
                }
                else
                {
                    Debug.LogWarning($"Failed to load preview image from {url}: {request.error}");
                }
            }
        }
    }

    /// <summary>
    /// Component attached to character instances to store reference to character data
    /// </summary>
    public class CharacterBehaviour : MonoBehaviour
    {
        public CharacterData CharacterData { get; private set; }
        
        public void Initialize(CharacterData character)
        {
            CharacterData = character;
        }

        // Add methods for character-specific behaviors like animations, attacks, etc.
        public void PlayAnimation(string animationType)
        {
            if (CharacterData == null)
            {
                Debug.LogWarning("CharacterData not initialized for animation.");
                return;
            }

            Animator animator = GetComponent<Animator>();
            if (animator != null && animator.HasState(0, Animator.StringToHash(animationType)))
            {
                animator.Play(animationType);
            }
            else
            {
                Debug.LogWarning($"Animator missing or state '{animationType}' not found for character {CharacterData.name}.");
            }
        }

        public void Attack()
        {
            PlayAnimation("Attack");
        }
        
        public void Defend()
        {
            PlayAnimation("Defend");
        }
        
        public void Idle()
        {
            PlayAnimation("Idle");
        }
        
        public void Victory()
        {
            PlayAnimation("Victory");
        }
        
        public void Defeat()
        {
            PlayAnimation("Defeat");
        }
    }
}
