using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using BrawlAnything.Models;

namespace BrawlAnything.Network
{
    /// <summary>
    /// Handles communication between the Unity frontend and the FastAPI backend.
    /// Provides methods for authentication, character management, and battle operations.
    /// </summary>
    public class BackendCommunicator : MonoBehaviour
    {
        [Header("Server Configuration")]
        [SerializeField] private string apiBaseUrl = "http://localhost:8000/api/v1";
        [SerializeField] private float requestTimeout = 10f;
        [SerializeField] private bool logRequests = false;

        [Header("Authentication")]
        [SerializeField] private bool useTokenAuth = true;
        
        // Events
        public event Action<bool, string> OnLoginComplete;
        public event Action<bool, string> OnRegistrationComplete;
        public event Action<bool, List<CharacterData>> OnCharactersLoaded;
        public event Action<bool, CharacterData> OnCharacterCreated;
        public event Action<bool, BattleData> OnBattleCreated;
        public event Action<bool, BattleData> OnBattleCompleted;
        
        // Private variables
        private string authToken;
        private UserData currentUser;
        
        // Singleton instance
        private static BackendCommunicator _instance;
        public static BackendCommunicator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<BackendCommunicator>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("BackendCommunicator");
                        _instance = go.AddComponent<BackendCommunicator>();
                        DontDestroyOnLoad(go);
                    }
                }
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
            DontDestroyOnLoad(gameObject);
        }
        
        #region Authentication
        
        /// <summary>
        /// Logs in a user with the provided credentials.
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="password">User password</param>
        public void Login(string email, string password)
        {
            StartCoroutine(LoginCoroutine(email, password));
        }
        
        private IEnumerator LoginCoroutine(string email, string password)
        {
            // Create login data
            var loginData = new Dictionary<string, string>
            {
                { "username", email },
                { "password", password }
            };
            
            // Convert to JSON
            string jsonData = JsonConvert.SerializeObject(loginData);
            
            // Create request
            using (UnityWebRequest request = CreateJsonRequest($"{apiBaseUrl}/login/access-token", "POST", jsonData))
            {
                // Send request
                yield return request.SendWebRequest();
                
                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Login failed: {request.error}");
                    OnLoginComplete?.Invoke(false, request.error);
                    yield break;
                }
                
                // Parse response
                string responseJson = request.downloadHandler.text;
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseJson);
                
                // Store token
                authToken = tokenResponse.access_token;
                
                // Get user data
                yield return StartCoroutine(GetCurrentUserCoroutine());
                
                OnLoginComplete?.Invoke(true, "Login successful");
            }
        }
        
        /// <summary>
        /// Registers a new user with the provided information.
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <param name="password">User password</param>
        /// <param name="displayName">Optional display name</param>
        public void Register(string email, string username, string password, string displayName = null)
        {
            StartCoroutine(RegisterCoroutine(email, username, password, displayName));
        }
        
        private IEnumerator RegisterCoroutine(string email, string username, string password, string displayName)
        {
            // Create registration data
            var registrationData = new Dictionary<string, string>
            {
                { "email", email },
                { "username", username },
                { "password", password }
            };
            
            if (!string.IsNullOrEmpty(displayName))
            {
                registrationData.Add("display_name", displayName);
            }
            
            // Convert to JSON
            string jsonData = JsonConvert.SerializeObject(registrationData);
            
            // Create request
            using (UnityWebRequest request = CreateJsonRequest($"{apiBaseUrl}/users/", "POST", jsonData))
            {
                // Send request
                yield return request.SendWebRequest();
                
                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Registration failed: {request.error}");
                    OnRegistrationComplete?.Invoke(false, request.error);
                    yield break;
                }
                
                // Parse response
                string responseJson = request.downloadHandler.text;
                var userResponse = JsonConvert.DeserializeObject<UserData>(responseJson);
                
                OnRegistrationComplete?.Invoke(true, "Registration successful");
            }
        }
        
        /// <summary>
        /// Gets the current user's data.
        /// </summary>
        private IEnumerator GetCurrentUserCoroutine()
        {
            // Create request
            using (UnityWebRequest request = CreateAuthenticatedRequest($"{apiBaseUrl}/users/me", "GET"))
            {
                // Send request
                yield return request.SendWebRequest();
                
                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to get user data: {request.error}");
                    yield break;
                }
                
                // Parse response
                string responseJson = request.downloadHandler.text;
                currentUser = JsonConvert.DeserializeObject<UserData>(responseJson);
                
                Debug.Log($"Got user data for {currentUser.username}");
            }
        }
        
        /// <summary>
        /// Logs out the current user.
        /// </summary>
        public void Logout()
        {
            authToken = null;
            currentUser = null;
        }
        
        #endregion
        
        #region Character Management
        
        /// <summary>
        /// Gets all characters owned by the current user.
        /// </summary>
        public void GetUserCharacters()
        {
            StartCoroutine(GetUserCharactersCoroutine());
        }
        
        private IEnumerator GetUserCharactersCoroutine()
        {
            // Create request
            using (UnityWebRequest request = CreateAuthenticatedRequest($"{apiBaseUrl}/characters/", "GET"))
            {
                // Send request
                yield return request.SendWebRequest();
                
                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to get characters: {request.error}");
                    OnCharactersLoaded?.Invoke(false, null);
                    yield break;
                }
                
                // Parse response
                string responseJson = request.downloadHandler.text;
                var characters = JsonConvert.DeserializeObject<List<CharacterData>>(responseJson);
                
                OnCharactersLoaded?.Invoke(true, characters);
            }
        }
        
        /// <summary>
        /// Creates a new character with the provided information.
        /// </summary>
        /// <param name="name">Character name</param>
        /// <param name="description">Character description</param>
        /// <param name="isPublic">Whether the character is public</param>
        /// <param name="imageTexture">Character image texture</param>
        /// <param name="colorPrimary">Primary color (hex format)</param>
        /// <param name="colorSecondary">Secondary color (hex format)</param>
        /// <param name="colorAccent">Accent color (hex format)</param>
        public void CreateCharacter(string name, string description, bool isPublic, Texture2D imageTexture, 
                                   string colorPrimary = "#FFFFFF", string colorSecondary = "#CCCCCC", string colorAccent = "#888888")
        {
            StartCoroutine(CreateCharacterCoroutine(name, description, isPublic, imageTexture, colorPrimary, colorSecondary, colorAccent));
        }
        
        private IEnumerator CreateCharacterCoroutine(string name, string description, bool isPublic, Texture2D imageTexture,
                                                   string colorPrimary, string colorSecondary, string colorAccent)
        {
            // Convert texture to PNG
            byte[] imageBytes = imageTexture.EncodeToPNG();
            
            // Create form data
            WWWForm form = new WWWForm();
            form.AddField("name", name);
            form.AddField("description", description ?? "");
            form.AddField("is_public", isPublic.ToString().ToLower());
            form.AddField("color_primary", colorPrimary);
            form.AddField("color_secondary", colorSecondary);
            form.AddField("color_accent", colorAccent);
            form.AddBinaryData("image", imageBytes, "character.png", "image/png");
            
            // Create request
            using (UnityWebRequest request = CreateAuthenticatedFormRequest($"{apiBaseUrl}/characters/", form))
            {
                // Send request
                yield return request.SendWebRequest();
                
                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to create character: {request.error}");
                    OnCharacterCreated?.Invoke(false, null);
                    yield break;
                }
                
                // Parse response
                string responseJson = request.downloadHandler.text;
                var character = JsonConvert.DeserializeObject<CharacterData>(responseJson);
                
                OnCharacterCreated?.Invoke(true, character);
            }
        }
        
        /// <summary>
        /// Uploads a 3D model for a character.
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="modelData">Model data (GLB format)</param>
        /// <param name="altModelData">Optional alternative model variations</param>
        public void UploadCharacterModel(int characterId, byte[] modelData, List<byte[]> altModelData = null)
        {
            StartCoroutine(UploadCharacterModelCoroutine(characterId, modelData, altModelData));
        }
        
        private IEnumerator UploadCharacterModelCoroutine(int characterId, byte[] modelData, List<byte[]> altModelData)
        {
            // Create form data
            WWWForm form = new WWWForm();
            form.AddBinaryData("model", modelData, "model.glb", "model/gltf-binary");
            
            if (altModelData != null && altModelData.Count > 0)
            {
                for (int i = 0; i < altModelData.Count; i++)
                {
                    form.AddBinaryData($"alt_models", altModelData[i], $"alt_model_{i}.glb", "model/gltf-binary");
                }
            }
            
            // Create request
            using (UnityWebRequest request = CreateAuthenticatedFormRequest($"{apiBaseUrl}/characters/{characterId}/model", form))
            {
                // Send request
                yield return request.SendWebRequest();
                
                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to upload model: {request.error}");
                    yield break;
                }
                
                Debug.Log("Model uploaded successfully");
            }
        }
        
        #endregion
        
        #region Battle Management
        
        /// <summary>
        /// Creates a new battle.
        /// </summary>
        /// <param name="battleType">Type of battle (pvp, pve, training)</param>
        /// <param name="opponentId">Optional opponent user ID</param>
        /// <param name="mapId">Optional map ID</param>
        public void CreateBattle(string battleType, int? opponentId = null, int? mapId = null)
        {
            StartCoroutine(CreateBattleCoroutine(battleType, opponentId, mapId));
        }
        
        private IEnumerator CreateBattleCoroutine(string battleType, int? opponentId, int? mapId)
        {
            // Create battle data
            var battleData = new Dictionary<string, object>
            {
                { "battle_type", battleType }
            };
            
            if (opponentId.HasValue)
            {
                battleData.Add("opponent_id", opponentId.Value);
            }
            
            if (mapId.HasValue)
            {
                battleData.Add("map_id", mapId.Value);
            }
            
            // Convert to JSON
            string jsonData = JsonConvert.SerializeObject(battleData);
            
            // Create request
            using (UnityWebRequest request = CreateAuthenticatedJsonRequest($"{apiBaseUrl}/battles/", "POST", jsonData))
            {
                // Send request
                yield return request.SendWebRequest();
                
                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to create battle: {request.error}");
                    OnBattleCreated?.Invoke(false, null);
                    yield break;
                }
                
                // Parse response
                string responseJson = request.downloadHandler.text;
                var battle = JsonConvert.DeserializeObject<BattleData>(responseJson);
                
                OnBattleCreated?.Invoke(true, battle);
            }
        }

        /// <summary>
        /// Adds a character to a battle.
        /// </summary>
        /// <param name="battleId">Battle ID</param>
        /// <param name="characterId">Character ID</param>
        public void AddCharacterToBattle(int battleId, int characterId)
        {
            StartCoroutine(AddCharacterToBattleCoroutine(battleId, characterId));
        }


        /// <summary>
        /// Adds a character to a battle.
        /// </summary>
        /// <param name="battleId">Battle ID</param>
        /// <param name="characterId">Character ID</param>
        private IEnumerator AddCharacterToBattleCoroutine(int battleId, int characterId)
        {
            var data = new Dictionary<string, object>
            {
                { "character_id", characterId }
            };

            string jsonData = JsonConvert.SerializeObject(data);

            using (UnityWebRequest request = CreateAuthenticatedJsonRequest($"{apiBaseUrl}/battles/{battleId}/add-character", "POST", jsonData))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to add character to battle: {request.error}");
                    yield break;
                }

                Debug.Log("Character added to battle successfully");
            }
        }

        /// <summary>
        /// Completes a battle and submits results.
        /// </summary>
        /// <param name="battle">The completed battle data</param>
        public void CompleteBattle(BattleData battle)
        {
            StartCoroutine(CompleteBattleCoroutine(battle));
        }

        private IEnumerator CompleteBattleCoroutine(BattleData battle)
        {
            string jsonData = JsonConvert.SerializeObject(battle);

            using (UnityWebRequest request = CreateAuthenticatedJsonRequest($"{apiBaseUrl}/battles/{battle.id}/complete", "POST", jsonData))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to complete battle: {request.error}");
                    OnBattleCompleted?.Invoke(false, null);
                    yield break;
                }

                string responseJson = request.downloadHandler.text;
                var completedBattle = JsonConvert.DeserializeObject<BattleData>(responseJson);
                OnBattleCompleted?.Invoke(true, completedBattle);
            }
        }

        #endregion

        #region Utility Methods

        private UnityWebRequest CreateJsonRequest(string url, string method, string json)
        {
            UnityWebRequest request = new UnityWebRequest(url, method);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)requestTimeout;

            if (logRequests) Debug.Log($"[Request] {method} {url}\nPayload: {json}");

            return request;
        }

        private UnityWebRequest CreateAuthenticatedRequest(string url, string method)
        {
            UnityWebRequest request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {authToken}");
            request.timeout = (int)requestTimeout;

            if (logRequests) Debug.Log($"[Auth Request] {method} {url}");

            return request;
        }

        private UnityWebRequest CreateAuthenticatedJsonRequest(string url, string method, string json)
        {
            var request = CreateJsonRequest(url, method, json);
            request.SetRequestHeader("Authorization", $"Bearer {authToken}");
            return request;
        }

        private UnityWebRequest CreateAuthenticatedFormRequest(string url, WWWForm form)
        {
            UnityWebRequest request = UnityWebRequest.Post(url, form);
            request.SetRequestHeader("Authorization", $"Bearer {authToken}");
            request.timeout = (int)requestTimeout;

            if (logRequests) Debug.Log($"[Form Request] POST {url}");

            return request;
        }

        #endregion
    }

    [Serializable]
    public class UserData
    {
        public int id;
        public string username;
        public string email;
        public string display_name;
    }

    [Serializable]
    public class BattleData
    {
        public int id;
        public string battle_type;
        public string result;
        public int experience_gained;
        public List<int> character_ids;
    }
}
