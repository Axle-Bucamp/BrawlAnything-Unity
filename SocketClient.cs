using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;
using BrawlAnything.Core;
using BrawlAnything.Character;

namespace BrawlAnything.Network
{
    /// <summary>
    /// Client socket pour la communication en temps réel avec le backend
    /// </summary>
    public class SocketClient : MonoBehaviour
    {
        [Header("Socket Settings")]
        [SerializeField] private string socketUrl = "wss://api.brawlanything.com/socket";
        [SerializeField] private float reconnectInterval = 5f;
        [SerializeField] private int maxReconnectAttempts = 5;
        [SerializeField] private bool autoReconnect = true;
        
        [Header("Debug Settings")]
        [SerializeField] private bool logMessages = true;
        
        // État du socket
        private WebSocket socket;
        private bool isConnected = false;
        private int reconnectAttempts = 0;
        private string userId;
        private string authToken;
        
        // Sérialiseur FEN pour optimiser les communications
        private FENSerializer fenSerializer;
        
        // Événements
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<BattleUpdateData> OnBattleUpdate;
        public event Action<string> OnARSessionCreated;
        public event Action<string> OnARSessionJoined;
        public event Action<string, int, Vector3, Quaternion> OnARUpdate;
        
        // Singleton instance
        private static SocketClient _instance;
        public static SocketClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SocketClient>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SocketClient");
                        _instance = go.AddComponent<SocketClient>();
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
            
            // Initialiser le sérialiseur FEN
            fenSerializer = new FENSerializer();
        }
        
        private void OnDestroy()
        {
            // Fermer la connexion
            Disconnect();
        }
        
        /// <summary>
        /// Connecte le client au serveur socket
        /// </summary>
        public void Connect(string userId, string authToken)
        {
            if (isConnected)
            {
                Debug.LogWarning("[SocketClient] Already connected");
                return;
            }
            
            this.userId = userId;
            this.authToken = authToken;
            
            // Construire l'URL avec les paramètres d'authentification
            string url = $"{socketUrl}?user_id={userId}&token={authToken}";
            
            try
            {
                // Créer et configurer le socket
                socket = new WebSocket(url);
                
                // Configurer les gestionnaires d'événements
                socket.OnOpen += OnSocketOpen;
                socket.OnClose += OnSocketClose;
                socket.OnError += OnSocketError;
                socket.OnMessage += OnSocketMessage;
                
                // Connecter le socket
                socket.Connect();
                
                if (logMessages)
                {
                    Debug.Log("[SocketClient] Connecting to socket server...");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketClient] Failed to connect: {e.Message}");
                
                // Tenter de se reconnecter si configuré
                if (autoReconnect && reconnectAttempts < maxReconnectAttempts)
                {
                    StartCoroutine(ReconnectCoroutine());
                }
            }
        }
        
        /// <summary>
        /// Déconnecte le client du serveur socket
        /// </summary>
        public void Disconnect()
        {
            if (socket != null && socket.ReadyState == WebSocketState.Open)
            {
                try
                {
                    // Envoyer un message de déconnexion
                    SendMessage("disconnect", new Dictionary<string, object>());
                    
                    // Fermer le socket
                    socket.Close();
                    
                    if (logMessages)
                    {
                        Debug.Log("[SocketClient] Disconnected from socket server");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SocketClient] Error during disconnect: {e.Message}");
                }
                finally
                {
                    // Nettoyer les gestionnaires d'événements
                    socket.OnOpen -= OnSocketOpen;
                    socket.OnClose -= OnSocketClose;
                    socket.OnError -= OnSocketError;
                    socket.OnMessage -= OnSocketMessage;
                    
                    socket = null;
                    isConnected = false;
                }
            }
        }
        
        /// <summary>
        /// Vérifie si le client est connecté
        /// </summary>
        public bool IsConnected()
        {
            return isConnected && socket != null && socket.ReadyState == WebSocketState.Open;
        }
        
        /// <summary>
        /// Envoie une mise à jour de bataille
        /// </summary>
        public void SendBattleUpdate(BattleUpdateData battleUpdate)
        {
            if (!IsConnected())
            {
                Debug.LogWarning("[SocketClient] Cannot send battle update: not connected");
                return;
            }
            
            // Sérialiser les données au format FEN pour optimiser la taille
            string fenData = fenSerializer.SerializeBattleUpdate(battleUpdate);
            
            // Créer le message
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "battle_id", battleUpdate.battleId },
                { "fen_data", fenData }
            };
            
            // Envoyer le message
            SendMessage("battle_update", data);
        }
        
        /// <summary>
        /// Envoie une demande de création de session AR
        /// </summary>
        public void SendCreateARSession()
        {
            if (!IsConnected())
            {
                Debug.LogWarning("[SocketClient] Cannot create AR session: not connected");
                return;
            }
            
            // Créer le message
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "user_id", userId }
            };
            
            // Envoyer le message
            SendMessage("create_ar_session", data);
        }
        
        /// <summary>
        /// Envoie une demande pour rejoindre une session AR
        /// </summary>
        public void SendJoinARSession(string sessionId)
        {
            if (!IsConnected())
            {
                Debug.LogWarning("[SocketClient] Cannot join AR session: not connected");
                return;
            }
            
            // Créer le message
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "user_id", userId }
            };
            
            // Envoyer le message
            SendMessage("join_ar_session", data);
        }
        
        /// <summary>
        /// Envoie une mise à jour AR
        /// </summary>
        public void SendARUpdate(string sessionId, int characterId, Vector3 position, Quaternion rotation)
        {
            if (!IsConnected())
            {
                Debug.LogWarning("[SocketClient] Cannot send AR update: not connected");
                return;
            }
            
            // Créer le message
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "character_id", characterId },
                { "position", new float[] { position.x, position.y, position.z } },
                { "rotation", new float[] { rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z } }
            };
            
            // Envoyer le message
            SendMessage("ar_update", data);
        }
        
        /// <summary>
        /// Envoie un message au serveur socket
        /// </summary>
        private void SendMessage(string type, Dictionary<string, object> data)
        {
            if (!IsConnected())
            {
                Debug.LogWarning($"[SocketClient] Cannot send message of type {type}: not connected");
                return;
            }
            
            try
            {
                // Créer le message
                Dictionary<string, object> message = new Dictionary<string, object>
                {
                    { "type", type },
                    { "data", data }
                };
                
                // Sérialiser le message
                string json = JsonConvert.SerializeObject(message);
                
                // Envoyer le message
                socket.Send(json);
                
                if (logMessages)
                {
                    Debug.Log($"[SocketClient] Sent message: {type}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketClient] Failed to send message: {e.Message}");
            }
        }
        
        /// <summary>
        /// Routine de reconnexion
        /// </summary>
        private IEnumerator ReconnectCoroutine()
        {
            reconnectAttempts++;
            
            Debug.Log($"[SocketClient] Attempting to reconnect ({reconnectAttempts}/{maxReconnectAttempts})...");
            
            // Attendre l'intervalle de reconnexion
            yield return new WaitForSeconds(reconnectInterval);
            
            // Tenter de se reconnecter
            Connect(userId, authToken);
        }
        
        #region Socket Event Handlers
        
        private void OnSocketOpen(object sender, EventArgs e)
        {
            isConnected = true;
            reconnectAttempts = 0;
            
            if (logMessages)
            {
                Debug.Log("[SocketClient] Connected to socket server");
            }
            
            // Notifier les écouteurs
            OnConnected?.Invoke();
        }
        
        private void OnSocketClose(object sender, CloseEventArgs e)
        {
            isConnected = false;
            
            if (logMessages)
            {
                Debug.Log($"[SocketClient] Disconnected from socket server: {e.Reason}");
            }
            
            // Notifier les écouteurs
            OnDisconnected?.Invoke();
            
            // Tenter de se reconnecter si configuré
            if (autoReconnect && reconnectAttempts < maxReconnectAttempts)
            {
                StartCoroutine(ReconnectCoroutine());
            }
        }
        
        private void OnSocketError(object sender, ErrorEventArgs e)
        {
            Debug.LogError($"[SocketClient] Socket error: {e.Message}");
        }
        
        private void OnSocketMessage(object sender, MessageEventArgs e)
        {
            try
            {
                // Désérialiser le message
                Dictionary<string, object> message = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Data);
                
                // Extraire le type et les données
                string type = message["type"].ToString();
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(message["data"].ToString());
                
                if (logMessages)
                {
                    Debug.Log($"[SocketClient] Received message: {type}");
                }
                
                // Traiter le message en fonction de son type
                switch (type)
                {
                    case "battle_update":
                        HandleBattleUpdate(data);
                        break;
                        
                    case "ar_session_created":
                        HandleARSessionCreated(data);
                        break;
                        
                    case "ar_session_joined":
                        HandleARSessionJoined(data);
                        break;
                        
                    case "ar_update":
                        HandleARUpdate(data);
                        break;
                        
                    default:
                        Debug.LogWarning($"[SocketClient] Unknown message type: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketClient] Failed to process message: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Message Handlers
        
        private void HandleBattleUpdate(Dictionary<string, object> data)
        {
            try
            {
                // Extraire les données
                int battleId = Convert.ToInt32(data["battle_id"]);
                string fenData = data["fen_data"].ToString();
                
                // Désérialiser les données FEN
                BattleUpdateData battleUpdate = fenSerializer.DeserializeBattleUpdate(fenData);
                
                if (battleUpdate != null)
                {
                    // Notifier les écouteurs
                    OnBattleUpdate?.Invoke(battleUpdate);
                }
                else
                {
                    Debug.LogError("[SocketClient] Failed to deserialize battle update");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketClient] Error handling battle update: {e.Message}");
            }
        }
        
        private void HandleARSessionCreated(Dictionary<string, object> data)
        {
            try
            {
                // Extraire les données
                string sessionId = data["session_id"].ToString();
                
                // Notifier les écouteurs
                OnARSessionCreated?.Invoke(sessionId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketClient] Error handling AR session created: {e.Message}");
            }
        }
        
        private void HandleARSessionJoined(Dictionary<string, object> data)
        {
            try
            {
                // Extraire les données
                string sessionId = data["session_id"].ToString();
                
                // Notifier les écouteurs
                OnARSessionJoined?.Invoke(sessionId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketClient] Error handling AR session joined: {e.Message}");
            }
        }
        
        private void HandleARUpdate(Dictionary<string, object> data)
        {
            try
            {
                // Extraire les données
                string sessionId = data["session_id"].ToString();
                int characterId = Convert.ToInt32(data["character_id"]);
                
                // Extraire la position
                float[] posArray = JsonConvert.DeserializeObject<float[]>(data["position"].ToString());
                Vector3 position = new Vector3(posArray[0], posArray[1], posArray[2]);
                
                // Extraire la rotation
                float[] rotArray = JsonConvert.DeserializeObject<float[]>(data["rotation"].ToString());
                Quaternion rotation = Quaternion.Euler(rotArray[0], rotArray[1], rotArray[2]);
                
                // Notifier les écouteurs
                OnARUpdate?.Invoke(sessionId, characterId, position, rotation);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketClient] Error handling AR update: {e.Message}");
            }
        }
        
        #endregion
    }
    
    #region Data Classes
    
    /// <summary>
    /// Données de mise à jour de bataille
    /// </summary>
    [Serializable]
    public class BattleUpdateData
    {
        public int battleId;
        public string status;
        public float timeRemaining;
        public List<CharacterStateData> characters;
        public Dictionary<string, object> customData;
    }
    
    /// <summary>
    /// Données d'état d'un personnage
    /// </summary>
    [Serializable]
    public class CharacterStateData
    {
        public int characterId;
        public int currentHealth;
        public Vector3 position;
        public Quaternion rotation;
        public string currentAnimation;
        public List<StatusEffectData> statusEffects;
    }
    
    /// <summary>
    /// Données d'un effet de statut
    /// </summary>
    [Serializable]
    public class StatusEffectData
    {
        public string type;
        public float duration;
        public float intensity;
    }
    
    #endregion
}
