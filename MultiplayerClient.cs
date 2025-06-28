using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using NativeWebSocket;
using BrawlAnything.Models;
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;

namespace BrawlAnything.Network
{
    /// <summary>
    /// Handles WebSocket communication with the backend server for real-time multiplayer functionality.
    /// </summary>
    public class MultiplayerClient : MonoBehaviour
    {
        [Header("Server Configuration")]
        [SerializeField] private string webSocketUrl = "ws://localhost:8000/ws";
        [SerializeField] private float reconnectDelay = 3f;
        [SerializeField] private int maxReconnectAttempts = 5;
        [SerializeField] private bool autoReconnect = true;
        
        [Header("Debug")]
        [SerializeField] private bool logMessages = true;
        [SerializeField] private bool logErrors = true;
        
        // WebSocket instance
        private WebSocket webSocket;
        private string authToken;
        private string sessionId;
        private bool isConnecting = false;
        private int reconnectAttempts = 0;
        private bool intentionalClose = false;
        
        // Message handlers
        private Dictionary<string, Action<Dictionary<string, object>>> messageHandlers = new Dictionary<string, Action<Dictionary<string, object>>>();
        
        // Events
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;
        public event Action<string, Dictionary<string, object>> OnMessageReceived;
        
        // Singleton instance
        private static MultiplayerClient _instance;
        public static MultiplayerClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MultiplayerClient>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MultiplayerClient");
                        _instance = go.AddComponent<MultiplayerClient>();
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
            
            // Register default message handlers
            RegisterMessageHandler("connection", HandleConnectionMessage);
            RegisterMessageHandler("matchmaking", HandleMatchmakingMessage);
            RegisterMessageHandler("game_start", HandleGameStartMessage);
            RegisterMessageHandler("player_action", HandlePlayerActionMessage);
            RegisterMessageHandler("game_state", HandleGameStateMessage);
            RegisterMessageHandler("ar_sync", HandleARSyncMessage);
            RegisterMessageHandler("game_end", HandleGameEndMessage);
        }
        
        private void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            if (webSocket != null)
            {
                webSocket.DispatchMessageQueue();
            }
            #endif
        }
        
        private void OnApplicationQuit()
        {
            CloseConnection();
        }
        
        /// <summary>
        /// Connects to the WebSocket server with authentication token.
        /// </summary>
        /// <param name="token">JWT authentication token</param>
        public void Connect(string token)
        {
            if (webSocket != null)
            {
                CloseConnection();
            }
            
            authToken = token;
            StartCoroutine(ConnectCoroutine());
        }
        
        private IEnumerator ConnectCoroutine()
        {
            isConnecting = true;
            intentionalClose = false;
            
            // Create WebSocket with token in URL
            string url = $"{webSocketUrl}?token={authToken}";
            webSocket = new WebSocket(url);
            
            // Set up WebSocket event handlers
            webSocket.OnOpen += () =>
            {
                if (logMessages) Debug.Log("WebSocket connected");
                isConnecting = false;
                reconnectAttempts = 0;
                OnConnected?.Invoke();
            };
            
            webSocket.OnMessage += (bytes) =>
            {
                string message = Encoding.UTF8.GetString(bytes);
                if (logMessages) Debug.Log($"WebSocket message received: {message}");
                
                try
                {
                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                    string messageType = data["type"].ToString();
                    
                    // Update session ID if present
                    if (data.ContainsKey("session_id") && data["session_id"] != null)
                    {
                        sessionId = data["session_id"].ToString();
                    }
                    
                    // Invoke specific handler if registered
                    if (messageHandlers.ContainsKey(messageType))
                    {
                        messageHandlers[messageType].Invoke(data);
                    }
                    
                    // Invoke general message event
                    OnMessageReceived?.Invoke(messageType, data);
                }
                catch (Exception e)
                {
                    if (logErrors) Debug.LogError($"Error parsing WebSocket message: {e.Message}");
                }
            };
            
            webSocket.OnError += (e) =>
            {
                if (logErrors) Debug.LogError($"WebSocket error: {e}");
                OnError?.Invoke(e);
            };
            
            webSocket.OnClose += (code) =>
            {
                if (logMessages) Debug.Log($"WebSocket closed with code {code}");
                isConnecting = false;
                
                OnDisconnected?.Invoke($"Connection closed with code {code}");
                
                // Auto reconnect if enabled and not intentionally closed
                if (autoReconnect && !intentionalClose && reconnectAttempts < maxReconnectAttempts)
                {
                    reconnectAttempts++;
                    float delay = reconnectDelay * reconnectAttempts;
                    if (logMessages) Debug.Log($"Reconnecting in {delay} seconds (attempt {reconnectAttempts}/{maxReconnectAttempts})");
                    StartCoroutine(ReconnectAfterDelay(delay));
                }
            };
            
            // Connect to WebSocket server
            yield return webSocket.Connect();
        }
        
        private IEnumerator ReconnectAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Connect(authToken);
        }
        
        /// <summary>
        /// Closes the WebSocket connection.
        /// </summary>
        public void CloseConnection()
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                intentionalClose = true;
                webSocket.Close();
            }
        }
        
        /// <summary>
        /// Registers a message handler for a specific message type.
        /// </summary>
        /// <param name="messageType">Type of message to handle</param>
        /// <param name="handler">Handler function</param>
        public void RegisterMessageHandler(string messageType, Action<Dictionary<string, object>> handler)
        {
            messageHandlers[messageType] = handler;
        }
        
        /// <summary>
        /// Unregisters a message handler for a specific message type.
        /// </summary>
        /// <param name="messageType">Type of message to unregister</param>
        public void UnregisterMessageHandler(string messageType)
        {
            if (messageHandlers.ContainsKey(messageType))
            {
                messageHandlers.Remove(messageType);
            }
        }
        
        /// <summary>
        /// Sends a message to the WebSocket server.
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <param name="data">Message data</param>
        public void SendMessage(string messageType, Dictionary<string, object> data)
        {
            if (webSocket == null || webSocket.State != WebSocketState.Open)
            {
                if (logErrors) Debug.LogError("Cannot send message: WebSocket not connected");
                return;
            }
            
            Dictionary<string, object> message = new Dictionary<string, object>
            {
                { "type", messageType },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                { "data", data }
            };
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                message["session_id"] = sessionId;
            }
            
            string json = JsonConvert.SerializeObject(message);
            webSocket.SendText(json);
        }
        
        #region Message Handlers
        
        private void HandleConnectionMessage(Dictionary<string, object> data)
        {
            // Connection confirmation message
            if (logMessages) Debug.Log("Connection confirmed by server");
        }
        
        private void HandleMatchmakingMessage(Dictionary<string, object> data)
        {
            // Matchmaking status update
            if (data.ContainsKey("data"))
            {
                var messageData = data["data"] as Dictionary<string, object>;
                if (messageData != null && messageData.ContainsKey("status"))
                {
                    string status = messageData["status"].ToString();
                    if (logMessages) Debug.Log($"Matchmaking status: {status}");
                    
                    // Handle specific matchmaking statuses
                    switch (status)
                    {
                        case "in_queue":
                            // Player added to matchmaking queue
                            break;
                        case "left_queue":
                            // Player removed from matchmaking queue
                            break;
                        case "error":
                            // Matchmaking error
                            if (messageData.ContainsKey("message"))
                            {
                                if (logErrors) Debug.LogError($"Matchmaking error: {messageData["message"]}");
                            }
                            break;
                    }
                }
            }
        }
        
        private void HandleGameStartMessage(Dictionary<string, object> data)
        {
            // Game start notification
            if (data.ContainsKey("data"))
            {
                var messageData = data["data"] as Dictionary<string, object>;
                if (messageData != null && messageData.ContainsKey("battle_id"))
                {
                    int battleId = Convert.ToInt32(messageData["battle_id"]);
                    if (logMessages) Debug.Log($"Game starting: Battle ID {battleId}");
                    
                    // Store session ID for this battle
                    if (data.ContainsKey("session_id"))
                    {
                        sessionId = data["session_id"].ToString();
                    }
                    
                    // Additional game start data
                    if (messageData.ContainsKey("opponent"))
                    {
                        var opponent = messageData["opponent"] as Dictionary<string, object>;
                        if (opponent != null)
                        {
                            if (logMessages) Debug.Log($"Opponent: {JsonConvert.SerializeObject(opponent)}");
                        }
                    }
                }
            }
        }
        
        private void HandlePlayerActionMessage(Dictionary<string, object> data)
        {
            // Player action notification
            if (data.ContainsKey("data"))
            {
                var messageData = data["data"] as Dictionary<string, object>;
                if (messageData != null && messageData.ContainsKey("action_type"))
                {
                    string actionType = messageData["action_type"].ToString();
                    int userId = Convert.ToInt32(messageData["user_id"]);
                    
                    if (logMessages) Debug.Log($"Player {userId} performed action: {actionType}");
                    
                    // Additional action data
                    if (messageData.ContainsKey("action_data"))
                    {
                        var actionData = messageData["action_data"] as Dictionary<string, object>;
                        if (actionData != null)
                        {
                            if (logMessages) Debug.Log($"Action data: {JsonConvert.SerializeObject(actionData)}");
                        }
                    }
                }
            }
        }
        
        private void HandleGameStateMessage(Dictionary<string, object> data)
        {
            // Game state update
            if (data.ContainsKey("data"))
            {
                var messageData = data["data"] as Dictionary<string, object>;
                if (messageData != null && messageData.ContainsKey("state"))
                {
                    var state = messageData["state"] as Dictionary<string, object>;
                    if (state != null)
                    {
                        // Process game state update
                        if (logMessages) Debug.Log("Received game state update");
                        
                        // Character states
                        if (state.ContainsKey("characters"))
                        {
                            var characters = state["characters"] as Dictionary<string, object>;
                            if (characters != null)
                            {
                                // Update character states
                            }
                        }
                        
                        // Turn information
                        if (state.ContainsKey("turn"))
                        {
                            var turn = state["turn"];
                            if (turn != null)
                            {
                                if (logMessages) Debug.Log($"Current turn: {turn}");
                            }
                        }
                        
                        // Battle status
                        if (state.ContainsKey("status"))
                        {
                            string status = state["status"].ToString();
                            if (logMessages) Debug.Log($"Battle status: {status}");
                        }
                    }
                }
            }
        }
        
        private void HandleARSyncMessage(Dictionary<string, object> data)
        {
            // AR synchronization data
            if (data.ContainsKey("data"))
            {
                var messageData = data["data"] as Dictionary<string, object>;
                if (messageData != null && messageData.ContainsKey("sync_type"))
                {
                    string syncType = messageData["sync_type"].ToString();
                    int userId = Convert.ToInt32(messageData["user_id"]);

                    if (logMessages) Debug.Log($"AR sync from user {userId} | Type: {syncType}");

                    // Handle specific sync types
                    if (messageData.ContainsKey("sync_data"))
                    {
                        var syncData = messageData["sync_data"] as Dictionary<string, object>;
                        if (syncData != null && logMessages)
                        {
                            Debug.Log($"AR Sync Data: {JsonConvert.SerializeObject(syncData)}");
                        }
                    }
                }
            }
        }

        private void HandleGameEndMessage(Dictionary<string, object> data)
        {
            if (data.ContainsKey("data"))
            {
                var messageData = data["data"] as Dictionary<string, object>;
                if (messageData != null)
                {
                    string result = messageData.ContainsKey("result") ? messageData["result"].ToString() : "unknown";
                    int winnerId = messageData.ContainsKey("winner_id") ? Convert.ToInt32(messageData["winner_id"]) : -1;
                    int battleId = messageData.ContainsKey("battle_id") ? Convert.ToInt32(messageData["battle_id"]) : -1;

                    if (logMessages)
                    {
                        Debug.Log($"Game Ended | Battle ID: {battleId} | Winner ID: {winnerId} | Result: {result}");
                    }

                    // Log any end summary details
                    if (messageData.ContainsKey("summary"))
                    {
                        var summary = messageData["summary"];
                        if (summary != null)
                        {
                            Debug.Log($"Game Summary: {JsonConvert.SerializeObject(summary)}");
                        }
                    }

                    // Optional: trigger any end-game events or UI updates
                    // GameManager.Instance.OnGameEnded?.Invoke(...);
                }
                else
                {
                    if (logErrors) Debug.LogError("Game end message missing 'data' section.");
                }
            }
            else
            {
                if (logErrors) Debug.LogError("Game end message received without data.");
            }
        }



        /// <summary>
        /// Retrieves the current Firebase authenticated user's ID.
        /// </summary>
        /// <returns>User ID as a string, or null if not authenticated.</returns>
        public string GetCurrentUserId()
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null)
            {
                return user.UserId;
            }
            else
            {
                if (logErrors) Debug.LogWarning("No authenticated user found.");
                return null;
            }
        }

        #endregion

        #region Message Dispatcher

        /// <summary>
        /// Dispatches incoming WebSocket messages based on their type.
        /// </summary>
        /// <param name="json">The raw JSON string from the WebSocket</param>
        public void HandleIncomingMessage(string json)
        {
            Dictionary<string, object> message = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (!message.ContainsKey("type"))
            {
                if (logErrors) Debug.LogError("Received message without type field.");
                return;
            }

            string messageType = message["type"].ToString();

            if (logMessages) Debug.Log($"Received message of type: {messageType}");

            switch (messageType)
            {
                case "connection":
                    HandleConnectionMessage(message);
                    break;
                case "matchmaking":
                    HandleMatchmakingMessage(message);
                    break;
                case "game_start":
                    HandleGameStartMessage(message);
                    break;
                case "player_action":
                    HandlePlayerActionMessage(message);
                    break;
                case "game_state":
                    HandleGameStateMessage(message);
                    break;
                case "ar_sync":
                    HandleARSyncMessage(message);
                    break;
                default:
                    if (logErrors) Debug.LogWarning($"Unhandled message type: {messageType}");
                    break;
            }
        }

        #endregion
    }
}
