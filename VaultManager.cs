using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using BrawlAnything.Core;

namespace BrawlAnything.Network
{
    /// <summary>
    /// Gestionnaire pour l'intégration avec Hashicorp Vault pour le stockage sécurisé des secrets
    /// </summary>
    public class VaultManager : MonoBehaviour
    {
        [Header("Vault Settings")]
        [SerializeField] private string vaultAddress = "https://vault.guidry-cloud.com";
        [SerializeField] private string vaultToken = "";
        [SerializeField] private string secretsPath = "secret/brawlanything";
        
        [Header("Debug Settings")]
        [SerializeField] private bool logOperations = true;
        [SerializeField] private bool usePlayerPrefsAsFallback = true;
        
        // État du gestionnaire
        private bool isInitialized = false;
        private bool isAuthenticated = false;
        
        // Cache des secrets
        private Dictionary<string, string> secretsCache = new Dictionary<string, string>();
        
        /// <summary>
        /// Initialise le gestionnaire Vault
        /// </summary>
        public void Initialize(string address = null)
        {
            if (!string.IsNullOrEmpty(address))
            {
                vaultAddress = address;
            }
            
            // Vérifier si l'adresse est valide
            if (string.IsNullOrEmpty(vaultAddress))
            {
                Debug.LogError("[VaultManager] Vault address is empty");
                return;
            }
            
            // Charger le token depuis les PlayerPrefs si disponible
            if (string.IsNullOrEmpty(vaultToken) && usePlayerPrefsAsFallback)
            {
                vaultToken = PlayerPrefs.GetString("VaultToken", "");
            }
            
            isInitialized = true;
            
            if (logOperations)
            {
                Debug.Log($"[VaultManager] Initialized with address: {vaultAddress}");
            }
            
            // Vérifier l'authentification si un token est disponible
            if (!string.IsNullOrEmpty(vaultToken))
            {
                StartCoroutine(VerifyAuthentication());
            }
        }
        
        /// <summary>
        /// Authentifie le gestionnaire avec un token Vault
        /// </summary>
        public void Authenticate(string token, Action<bool> callback = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("[VaultManager] Not initialized");
                callback?.Invoke(false);
                return;
            }
            
            vaultToken = token;
            
            // Sauvegarder le token dans les PlayerPrefs si configuré
            if (usePlayerPrefsAsFallback)
            {
                PlayerPrefs.SetString("VaultToken", vaultToken);
                PlayerPrefs.Save();
            }
            
            // Vérifier l'authentification
            StartCoroutine(VerifyAuthentication(callback));
        }
        
        /// <summary>
        /// Vérifie l'authentification avec Vault
        /// </summary>
        private IEnumerator VerifyAuthentication(Action<bool> callback = null)
        {
            if (string.IsNullOrEmpty(vaultToken))
            {
                isAuthenticated = false;
                callback?.Invoke(false);
                yield break;
            }
            
            string url = $"{vaultAddress}/v1/auth/token/lookup-self";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Ajouter l'en-tête d'authentification
                request.SetRequestHeader("X-Vault-Token", vaultToken);
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    isAuthenticated = true;
                    
                    if (logOperations)
                    {
                        Debug.Log("[VaultManager] Authentication verified");
                    }
                    
                    callback?.Invoke(true);
                }
                else
                {
                    isAuthenticated = false;
                    
                    Debug.LogError($"[VaultManager] Authentication failed: {request.error}");
                    
                    callback?.Invoke(false);
                }
            }
        }
        
        /// <summary>
        /// Stocke un secret dans Vault
        /// </summary>
        public void StoreSecret(string key, string value, Action<bool, string> callback = null)
        {
            if (!isInitialized)
            {
                string error = "Not initialized";
                Debug.LogError($"[VaultManager] {error}");
                callback?.Invoke(false, error);
                return;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                string error = "Key cannot be empty";
                Debug.LogError($"[VaultManager] {error}");
                callback?.Invoke(false, error);
                return;
            }
            
            // Stocker dans le cache
            secretsCache[key] = value;
            
            // Stocker dans les PlayerPrefs si configuré comme fallback
            if (usePlayerPrefsAsFallback)
            {
                PlayerPrefs.SetString($"Vault_{key}", value);
                PlayerPrefs.Save();
            }
            
            // Si non authentifié, utiliser uniquement le fallback
            if (!isAuthenticated || string.IsNullOrEmpty(vaultToken))
            {
                if (usePlayerPrefsAsFallback)
                {
                    callback?.Invoke(true, null);
                }
                else
                {
                    string error = "Not authenticated";
                    Debug.LogError($"[VaultManager] {error}");
                    callback?.Invoke(false, error);
                }
                return;
            }
            
            // Stocker dans Vault
            StartCoroutine(StoreSecretCoroutine(key, value, callback));
        }
        
        /// <summary>
        /// Récupère un secret depuis Vault
        /// </summary>
        public string GetSecret(string key)
        {
            if (!isInitialized)
            {
                Debug.LogError("[VaultManager] Not initialized");
                return null;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[VaultManager] Key cannot be empty");
                return null;
            }
            
            // Vérifier le cache
            if (secretsCache.TryGetValue(key, out string cachedValue))
            {
                return cachedValue;
            }
            
            // Vérifier les PlayerPrefs si configuré comme fallback
            if (usePlayerPrefsAsFallback)
            {
                string value = PlayerPrefs.GetString($"Vault_{key}", null);
                if (!string.IsNullOrEmpty(value))
                {
                    // Mettre en cache
                    secretsCache[key] = value;
                    return value;
                }
            }
            
            // Si non authentifié, impossible de récupérer depuis Vault
            if (!isAuthenticated || string.IsNullOrEmpty(vaultToken))
            {
                Debug.LogWarning($"[VaultManager] Not authenticated, cannot retrieve secret: {key}");
                return null;
            }
            
            // Lancer la récupération asynchrone
            StartCoroutine(GetSecretCoroutine(key));
            
            // Retourner null pour l'instant, la valeur sera mise en cache lors de la récupération
            return null;
        }
        
        /// <summary>
        /// Récupère un secret depuis Vault de manière asynchrone
        /// </summary>
        public void GetSecretAsync(string key, Action<string, string> callback)
        {
            if (!isInitialized)
            {
                string error = "Not initialized";
                Debug.LogError($"[VaultManager] {error}");
                callback?.Invoke(null, error);
                return;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                string error = "Key cannot be empty";
                Debug.LogError($"[VaultManager] {error}");
                callback?.Invoke(null, error);
                return;
            }
            
            // Vérifier le cache
            if (secretsCache.TryGetValue(key, out string cachedValue))
            {
                callback?.Invoke(cachedValue, null);
                return;
            }
            
            // Vérifier les PlayerPrefs si configuré comme fallback
            if (usePlayerPrefsAsFallback)
            {
                string value = PlayerPrefs.GetString($"Vault_{key}", null);
                if (!string.IsNullOrEmpty(value))
                {
                    // Mettre en cache
                    secretsCache[key] = value;
                    callback?.Invoke(value, null);
                    return;
                }
            }
            
            // Si non authentifié, impossible de récupérer depuis Vault
            if (!isAuthenticated || string.IsNullOrEmpty(vaultToken))
            {
                string error = "Not authenticated";
                Debug.LogWarning($"[VaultManager] {error}, cannot retrieve secret: {key}");
                callback?.Invoke(null, error);
                return;
            }
            
            // Lancer la récupération asynchrone
            StartCoroutine(GetSecretCoroutine(key, callback));
        }
        
        /// <summary>
        /// Supprime un secret de Vault
        /// </summary>
        public void DeleteSecret(string key, Action<bool, string> callback = null)
        {
            if (!isInitialized)
            {
                string error = "Not initialized";
                Debug.LogError($"[VaultManager] {error}");
                callback?.Invoke(false, error);
                return;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                string error = "Key cannot be empty";
                Debug.LogError($"[VaultManager] {error}");
                callback?.Invoke(false, error);
                return;
            }
            
            // Supprimer du cache
            secretsCache.Remove(key);
            
            // Supprimer des PlayerPrefs si configuré comme fallback
            if (usePlayerPrefsAsFallback)
            {
                PlayerPrefs.DeleteKey($"Vault_{key}");
                PlayerPrefs.Save();
            }
            
            // Si non authentifié, utiliser uniquement le fallback
            if (!isAuthenticated || string.IsNullOrEmpty(vaultToken))
            {
                if (usePlayerPrefsAsFallback)
                {
                    callback?.Invoke(true, null);
                }
                else
                {
                    string error = "Not authenticated";
                    Debug.LogError($"[VaultManager] {error}");
                    callback?.Invoke(false, error);
                }
                return;
            }
            
            // Supprimer de Vault
            StartCoroutine(DeleteSecretCoroutine(key, callback));
        }
        
        /// <summary>
        /// Vérifie si le gestionnaire est initialisé
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }
        
        /// <summary>
        /// Vérifie si le gestionnaire est authentifié
        /// </summary>
        public bool IsAuthenticated()
        {
            return isAuthenticated;
        }
        
        #region Coroutines
        
        /// <summary>
        /// Coroutine pour stocker un secret dans Vault
        /// </summary>
        private IEnumerator StoreSecretCoroutine(string key, string value, Action<bool, string> callback)
        {
            string url = $"{vaultAddress}/v1/{secretsPath}/data/{key}";
            
            // Créer les données à envoyer
            var requestData = new Dictionary<string, object>
            {
                { "data", new Dictionary<string, string> { { "value", value } } }
            };
            
            string jsonData = JsonConvert.SerializeObject(requestData);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Vault-Token", vaultToken);
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (logOperations)
                    {
                        Debug.Log($"[VaultManager] Secret stored: {key}");
                    }
                    
                    callback?.Invoke(true, null);
                }
                else
                {
                    string error = $"Failed to store secret: {request.error}";
                    Debug.LogError($"[VaultManager] {error}");
                    callback?.Invoke(false, error);
                }
            }
        }
        
        /// <summary>
        /// Coroutine pour récupérer un secret depuis Vault
        /// </summary>
        private IEnumerator GetSecretCoroutine(string key, Action<string, string> callback = null)
        {
            string url = $"{vaultAddress}/v1/{secretsPath}/data/{key}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("X-Vault-Token", vaultToken);
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string response = request.downloadHandler.text;
                        var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                        
                        if (responseData.TryGetValue("data", out object dataObj))
                        {
                            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataObj.ToString());
                            
                            if (data.TryGetValue("data", out object secretDataObj))
                            {
                                var secretData = JsonConvert.DeserializeObject<Dictionary<string, string>>(secretDataObj.ToString());
                                
                                if (secretData.TryGetValue("value", out string value))
                                {
                                    // Mettre en cache
                                    secretsCache[key] = value;
                                    
                                    if (logOperations)
                                    {
                                        Debug.Log($"[VaultManager] Secret retrieved: {key}");
                                    }
                                    
                                    callback?.Invoke(value, null);
                                    yield break;
                                }
                            }
                        }
                        
                        string error = "Secret not found in response";
                        Debug.LogError($"[VaultManager] {error}");
                        callback?.Invoke(null, error);
                    }
                    catch (Exception e)
                    {
                        string error = $"Failed to parse response: {e.Message}";
                        Debug.LogError($"[VaultManager] {error}");
                        callback?.Invoke(null, error);
                    }
                }
                else
                {
                    string error = $"Failed to retrieve secret: {request.error}";
                    Debug.LogError($"[VaultManager] {error}");
                    callback?.Invoke(null, error);
                }
            }
        }
        
        /// <summary>
        /// Coroutine pour supprimer un secret de Vault
        /// </summary>
        private IEnumerator DeleteSecretCoroutine(string key, Action<bool, string> callback)
        {
            string url = $"{vaultAddress}/v1/{secretsPath}/metadata/{key}";
            
            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                request.SetRequestHeader("X-Vault-Token", vaultToken);
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (logOperations)
                    {
                        Debug.Log($"[VaultManager] Secret deleted: {key}");
                    }
                    
                    callback?.Invoke(true, null);
                }
                else
                {
                    string error = $"Failed to delete secret: {request.error}";
                    Debug.LogError($"[VaultManager] {error}");
                    callback?.Invoke(false, error);
                }
            }
        }
        
        #endregion
    }
}
