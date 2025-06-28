using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BrawlAnything.Models;
using System.Text;

namespace BrawlAnything.Network
{
    /// <summary>
    /// Handles API communication with the backend and asset services
    /// </summary>
    public class APIClient : MonoBehaviour
    {
        [SerializeField] private string backendBaseUrl = "http://localhost:8000/api/v1";
        [SerializeField] private string assetServiceBaseUrl = "http://localhost:8001/api/v1";
        
        private string authToken;
        
        // Singleton instance
        private static APIClient _instance;
        public static APIClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<APIClient>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("APIClient");
                        _instance = go.AddComponent<APIClient>();
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
        
        public IEnumerator Login(string email, string password, Action<bool, string> callback)
        {
            WWWForm form = new WWWForm();
            form.AddField("username", email);
            form.AddField("password", password);
            
            using (UnityWebRequest request = UnityWebRequest.Post($"{backendBaseUrl}/login", form))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    TokenResponse tokenResponse = JsonUtility.FromJson<TokenResponse>(response);
                    authToken = tokenResponse.access_token;
                    callback(true, null);
                }
                else
                {
                    callback(false, request.error);
                }
            }
        }
        
        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(authToken);
        }
        
        public void Logout()
        {
            authToken = null;
        }

        #endregion

        #region CharacterData Management

        public IEnumerator GetUserCharacters(Action<List<CharacterData>, string> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{backendBaseUrl}/characters"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    CharacterListResponse characterList = JsonUtility.FromJson<CharacterListResponse>($"{{\"characters\":{response}}}");
                    callback(characterList.characters, null);
                }
                else
                {
                    callback(null, request.error);
                }
            }
        }

        public string GetFullURL(string endpoint)
        {
            return this.backendBaseUrl + endpoint;
        }
        
        public IEnumerator GetCharacterDetails(int characterId, Action<CharacterData, string> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{backendBaseUrl}/characters/{characterId}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    CharacterData character = JsonUtility.FromJson<CharacterData>(response);
                    callback(character, null);
                }
                else
                {
                    callback(null, request.error);
                }
            }
        }
        
        public IEnumerator UploadCharacterImage(int characterId, byte[] imageData, Action<bool, string> callback)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("image", imageData, "character.jpg", "image/jpeg");
            
            using (UnityWebRequest request = UnityWebRequest.Post($"{backendBaseUrl}/characters/{characterId}/image", form))
            {
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback(true, null);
                }
                else
                {
                    callback(false, request.error);
                }
            }
        }
        
        #endregion
        
        #region Model Generation
        
        public IEnumerator GenerateModel(int characterId, byte[] imageData, Action<string, string> callback)
        {
            WWWForm form = new WWWForm();
            form.AddField("character_id", characterId);
            form.AddBinaryData("image", imageData, "character.jpg", "image/jpeg");
            
            using (UnityWebRequest request = UnityWebRequest.Post($"{assetServiceBaseUrl}/models", form))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    ModelGenerationResponse generationResponse = JsonUtility.FromJson<ModelGenerationResponse>(response);
                    callback(generationResponse.request_id, null);
                }
                else
                {
                    callback(null, request.error);
                }
            }
        }
        
        public IEnumerator CheckModelGenerationStatus(string requestId, Action<ModelGenerationResponse, string> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{assetServiceBaseUrl}/models/{requestId}"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    ModelGenerationResponse generationResponse = JsonUtility.FromJson<ModelGenerationResponse>(response);
                    callback(generationResponse, null);
                }
                else
                {
                    callback(null, request.error);
                }
            }
        }
        
        #endregion
        
        #region Animation Generation
        
        public IEnumerator GenerateAnimation(int characterId, int animationId, string modelUrl, string animationType, byte[] maskData, Action<string, string> callback)
        {
            WWWForm form = new WWWForm();
            form.AddField("character_id", characterId);
            form.AddField("animation_id", animationId);
            form.AddField("model_url", modelUrl);
            form.AddField("animation_type", animationType);
            
            if (maskData != null)
            {
                form.AddBinaryData("mask", maskData, "mask.png", "image/png");
            }
            
            using (UnityWebRequest request = UnityWebRequest.Post($"{assetServiceBaseUrl}/animations", form))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    AnimationGenerationResponse generationResponse = JsonUtility.FromJson<AnimationGenerationResponse>(response);
                    callback(generationResponse.request_id, null);
                }
                else
                {
                    callback(null, request.error);
                }
            }
        }

        /// <summary>
        /// Adds the authentication headers to the UnityWebRequest if the user is authenticated.
        /// </summary>
        /// <param name="www">The UnityWebRequest to modify.</param>
        public void AddAuthHeaders(UnityWebRequest www)
        {
            if (www == null)
            {
                Debug.LogWarning("[APIClient] Attempted to add auth headers to a null request.");
                return;
            }

            if (string.IsNullOrEmpty(authToken))
            {
                Debug.LogWarning("[APIClient] No auth token available. Headers not added.");
                return;
            }

            www.SetRequestHeader("Authorization", $"Bearer {authToken}");
        }


        public IEnumerator CheckAnimationGenerationStatus(string requestId, Action<AnimationGenerationResponse, string> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{assetServiceBaseUrl}/animations/{requestId}"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    AnimationGenerationResponse generationResponse = JsonUtility.FromJson<AnimationGenerationResponse>(response);
                    callback(generationResponse, null);
                }
                else
                {
                    callback(null, request.error);
                }
            }
        }
        
        #endregion
        
        #region Asset Download
        
        public IEnumerator DownloadAsset(string url, Action<byte[], string> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback(request.downloadHandler.data, null);
                }
                else
                {
                    callback(null, request.error);
                }
            }
        }

        /// <summary>
        /// / basic json data post form
        /// </summary>
        public IEnumerator PostJsonData(string url, string jsonData, Action<bool, string> callback)
        {
            // post json .PostJsonData(feedbackApiEndpoint, jsonData, OnFeedbackSubmitted)
            WWWForm form = new WWWForm();
            form.AddField("json", jsonData); // better json type management

            using (UnityWebRequest request = UnityWebRequest.Post($"{url}/feedback", form))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
 
                    callback(true, response);
                }
                else
                {
                    callback(false, request.error);
                }
            }
        }

        #endregion
    }
    
    #region Response Classes
    
    [Serializable]
    public class TokenResponse
    {
        public string access_token;
        public string token_type;
    }
   
    
    [Serializable]
    public class AnimationLoader
    {
        public int id;
        public string name;
        public int character_id;
        public string animation_url;
        public string animation_type;
        public string created_at;
        public string updated_at;
    }
    
    [Serializable]
    public class CharacterListResponse
    {
        public List<CharacterData> characters;
    }
    
    [Serializable]
    public class ModelGenerationResponse
    {
        public string request_id;
        public string status;
        public int character_id;
        public string created_at;
        public string updated_at;
        public List<string> model_urls;
        public string error;
    }
    
    [Serializable]
    public class AnimationGenerationResponse
    {
        public string request_id;
        public string status;
        public int character_id;
        public int animation_id;
        public string created_at;
        public string updated_at;
        public string animation_url;
        public string error;
    }
    
    #endregion
}
