using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrawlAnything.Network;
using BrawlAnything.AR;
using BrawlAnything.Managers;


namespace BrawlAnything.Spectator
{
    /// <summary>
    /// Manages spectator mode functionality, allowing players to watch ongoing battles.
    /// </summary>
    public class SpectatorManager : MonoBehaviour
    {
        [Header("Spectator Settings")]
        [SerializeField] private bool enableSpectatorMode = true;
        [SerializeField] private int maxSpectatorsPerMatch = 10;
        [SerializeField] private float spectatorSyncInterval = 0.1f;
        [SerializeField] private bool enableSpectatorChat = true;
        [SerializeField] private bool enableSpectatorEmotes = true;
        
        [Header("Camera Settings")]
        [SerializeField] private bool enableFreeCameraMode = true;
        [SerializeField] private bool enablePlayerPerspective = true;
        [SerializeField] private float cameraTransitionSpeed = 2.0f;
        [SerializeField] private float minCameraDistance = 2.0f;
        [SerializeField] private float maxCameraDistance = 10.0f;
        
        [Header("UI References")]
        [SerializeField] private GameObject spectatorUI;
        [SerializeField] private GameObject spectatorChatPanel;
        [SerializeField] private GameObject spectatorControlsPanel;
        [SerializeField] private GameObject spectatorStatsPanel;


        
        // Spectator state
        private int currentMatchId = -1;
        private bool isSpectating = false;
        private Dictionary<int, PlayerBattleState> playerStates = new Dictionary<int, PlayerBattleState>();
        private List<SpectatorChatMessage> chatMessages = new List<SpectatorChatMessage>();
        private SpectatorCameraMode currentCameraMode = SpectatorCameraMode.Overview;
        private int focusedPlayerId = -1;
        private float lastSyncTime;
        
        // Camera control
        private Transform spectatorCamera;
        private Vector3 targetCameraPosition;
        private Quaternion targetCameraRotation;
        private float targetCameraDistance = 5.0f;
        
        // Singleton instance
        private static SpectatorManager _instance;
        public static SpectatorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SpectatorManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SpectatorManager");
                        _instance = go.AddComponent<SpectatorManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        // Events
        public event Action<int> OnSpectatingStarted;
        public event Action OnSpectatingEnded;
        public event Action<Dictionary<int, PlayerBattleState>> OnBattleStateUpdated;
        public event Action<SpectatorChatMessage> OnChatMessageReceived;
        
        /// <summary>
        /// Spectator camera modes.
        /// </summary>
        public enum SpectatorCameraMode
        {
            Overview,
            FollowPlayer1,
            FollowPlayer2,
            FreeCam,
            TopDown,
            FirstPerson
        }
        
        /// <summary>
        /// Player battle state structure.
        /// </summary>
        [System.Serializable]
        public class PlayerBattleState
        {
            public int PlayerId;
            public string PlayerName;
            public string CharacterName;
            public Vector3 Position;
            public Quaternion Rotation;
            public float Health;
            public float Energy;
            public string CurrentAction;
            public float ActionProgress;
            public Dictionary<string, object> Stats;
            public long LastUpdated;
            
            public PlayerBattleState(int playerId, string playerName)
            {
                PlayerId = playerId;
                PlayerName = playerName;
                CharacterName = "";
                Position = Vector3.zero;
                Rotation = Quaternion.identity;
                Health = 100f;
                Energy = 100f;
                CurrentAction = "idle";
                ActionProgress = 0f;
                Stats = new Dictionary<string, object>();
                LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
        
        /// <summary>
        /// Spectator chat message structure.
        /// </summary>
        [System.Serializable]
        public class SpectatorChatMessage
        {
            public int SenderId;
            public string SenderName;
            public string Message;
            public string MessageType; // "text", "emote", "system"
            public long Timestamp;
            
            public SpectatorChatMessage(int senderId, string senderName, string message, string messageType)
            {
                SenderId = senderId;
                SenderName = senderName;
                Message = message;
                MessageType = messageType;
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
            
            // Create spectator camera if needed
            if (spectatorCamera == null)
            {
                GameObject cameraObj = new GameObject("SpectatorCamera");
                spectatorCamera = cameraObj.transform;
                UnityEngine.Camera cam = cameraObj.AddComponent<UnityEngine.Camera>();
                cam.enabled = false;
                cameraObj.SetActive(false);
                cameraObj.transform.SetParent(transform);
            }
        }
        
        private void OnEnable()
        {
            // Register for network events
            if (MultiplayerClient.Instance != null)
            {
                MultiplayerClient.Instance.RegisterMessageHandler("spectator_battle_state", HandleBattleStateUpdate); 
                // local method comucate to network player id voir si effacer cote manager
                MultiplayerClient.Instance.RegisterMessageHandler("spectator_chat", HandleChatMessage);
                MultiplayerClient.Instance.RegisterMessageHandler("spectator_join_result", HandleJoinResult);
                MultiplayerClient.Instance.RegisterMessageHandler("spectator_leave_result", HandleLeaveResult);
            }
        }
        
        private void OnDisable()
        {
            // Unregister from network events
            if (MultiplayerClient.Instance != null)
            {
                MultiplayerClient.Instance.UnregisterMessageHandler("spectator_battle_state");
                MultiplayerClient.Instance.UnregisterMessageHandler("spectator_chat");
                MultiplayerClient.Instance.UnregisterMessageHandler("spectator_join_result");
                MultiplayerClient.Instance.UnregisterMessageHandler("spectator_leave_result");
            }
        }
        
        private void Update()
        {
            if (!isSpectating) return;
            
            // Update spectator camera
            UpdateSpectatorCamera();
            
            // Request battle state update periodically
            if (Time.time - lastSyncTime >= spectatorSyncInterval)
            {
                RequestBattleStateUpdate();
                lastSyncTime = Time.time;
            }
        }
        
        /// <summary>
        /// Starts spectating a match.
        /// </summary>
        /// <param name="matchId">ID of the match to spectate</param>
        public void StartSpectating(int matchId)
        {
            if (!enableSpectatorMode) return;
            
            // Request to join as spectator
            Dictionary<string, object> joinData = new Dictionary<string, object>
            {
                { "match_id", matchId }
            };
            
            MultiplayerClient.Instance.SendMessage("spectator_join", joinData);
            
            Debug.Log($"Requested to spectate match {matchId}");
        }
        
        /// <summary>
        /// Stops spectating the current match.
        /// </summary>
        public void StopSpectating()
        {
            if (!isSpectating) return;
            
            // Request to leave as spectator
            Dictionary<string, object> leaveData = new Dictionary<string, object>
            {
                { "match_id", currentMatchId }
            };
            
            MultiplayerClient.Instance.SendMessage("spectator_leave", leaveData);
            
            Debug.Log($"Requested to stop spectating match {currentMatchId}");
        }
        
        /// <summary>
        /// Sends a chat message as a spectator.
        /// </summary>
        /// <param name="message">Message text</param>
        /// <param name="messageType">Type of message ("text" or "emote")</param>
        public void SendChatMessage(string message, string messageType = "text")
        {
            if (!isSpectating || !enableSpectatorChat) return;
            
            if (messageType == "emote" && !enableSpectatorEmotes) return;
            
            Dictionary<string, object> chatData = new Dictionary<string, object>
            {
                { "match_id", currentMatchId },
                { "message", message },
                { "message_type", messageType }
            };
            
            MultiplayerClient.Instance.SendMessage("spectator_chat", chatData);
            
            Debug.Log($"Sent spectator {messageType}: {message}");
        }

        private void HandleBattleStateUpdate(Dictionary<string, object> data)
        {
            if (data == null || !data.ContainsKey("players")) return;

            var playersData = data["players"] as List<object>;
            if (playersData == null) return;

            foreach (var playerObj in playersData)
            {
                var playerDict = playerObj as Dictionary<string, object>;
                if (playerDict == null) continue;

                int playerId = Convert.ToInt32(playerDict["player_id"]);
                string playerName = playerDict["player_name"].ToString();

                PlayerBattleState state;
                if (!playerStates.TryGetValue(playerId, out state))
                {
                    state = new PlayerBattleState(playerId, playerName);
                    playerStates[playerId] = state;
                }

                state.CharacterName = playerDict["character_name"].ToString();
                state.Position = ParseVector3(playerDict["position"]);
                state.Rotation = ParseQuaternion(playerDict["rotation"]);
                state.Health = Convert.ToSingle(playerDict["health"]);
                state.Energy = Convert.ToSingle(playerDict["energy"]);
                state.CurrentAction = playerDict["current_action"].ToString();
                state.ActionProgress = Convert.ToSingle(playerDict["action_progress"]);
                state.Stats = playerDict["stats"] as Dictionary<string, object>;
                state.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            OnBattleStateUpdated?.Invoke(playerStates);
        }

        private Vector3 ParseVector3(object obj)
        {
            var dict = obj as Dictionary<string, object>;
            if (dict == null) return Vector3.zero;

            float x = Convert.ToSingle(dict["x"]);
            float y = Convert.ToSingle(dict["y"]);
            float z = Convert.ToSingle(dict["z"]);

            return new Vector3(x, y, z);
        }

        private Quaternion ParseQuaternion(object obj)
        {
            var dict = obj as Dictionary<string, object>;
            if (dict == null) return Quaternion.identity;

            float x = Convert.ToSingle(dict["x"]);
            float y = Convert.ToSingle(dict["y"]);
            float z = Convert.ToSingle(dict["z"]);
            float w = Convert.ToSingle(dict["w"]);

            return new Quaternion(x, y, z, w);
        }

        private void HandleChatMessage(Dictionary<string, object> data)
        {
            if (data == null) return;

            int senderId = Convert.ToInt32(data["sender_id"]);
            string senderName = data["sender_name"].ToString();
            string message = data["message"].ToString();
            string messageType = data["message_type"].ToString();

            var chatMessage = new SpectatorChatMessage(senderId, senderName, message, messageType);
            chatMessages.Add(chatMessage);

            OnChatMessageReceived?.Invoke(chatMessage);
        }

        private void HandleJoinResult(Dictionary<string, object> data)
        {
            if (data == null) return;

            bool success = Convert.ToBoolean(data["success"]);
            if (success)
            {
                currentMatchId = Convert.ToInt32(data["match_id"]);
                isSpectating = true;

                spectatorCamera.gameObject.SetActive(true);
                spectatorCamera.GetComponent<UnityEngine.Camera>().enabled = true;

                spectatorUI?.SetActive(true);
                spectatorChatPanel?.SetActive(enableSpectatorChat);
                spectatorControlsPanel?.SetActive(true);
                spectatorStatsPanel?.SetActive(true);

                OnSpectatingStarted?.Invoke(currentMatchId);
            }
            else
            {
                Debug.LogWarning("Failed to join as spectator.");
            }
        }

        private void HandleLeaveResult(Dictionary<string, object> data)
        {
            if (data == null) return;

            bool success = Convert.ToBoolean(data["success"]);
            if (success)
            {
                isSpectating = false;
                currentMatchId = -1;
                playerStates.Clear();
                chatMessages.Clear();

                spectatorCamera.gameObject.SetActive(false);
                spectatorCamera.GetComponent<UnityEngine.Camera>().enabled = false;

                spectatorUI?.SetActive(false);
                spectatorChatPanel?.SetActive(false);
                spectatorControlsPanel?.SetActive(false);
                spectatorStatsPanel?.SetActive(false);

                OnSpectatingEnded?.Invoke();
            }
            else
            {
                Debug.LogWarning("Failed to leave spectator mode.");
            }
        }

        /// <summary>
        /// Changes the spectator camera mode.
        /// </summary>
        /// <param name="mode">New camera mode</param>
        /// <param name="playerId">Player ID to focus on (for player-specific modes)</param>
        public void ChangeCameraMode(SpectatorCameraMode mode, int playerId = -1)
        {
            if (!isSpectating) return;
            
            // Validate camera mode
            if (mode == SpectatorCameraMode.FreeCam && !enableFreeCameraMode)
            {
                mode = SpectatorCameraMode.Overview;
            }
            
            if ((mode == SpectatorCameraMode.FirstPerson || mode == SpectatorCameraMode.FollowPlayer1 || mode == SpectatorCameraMode.FollowPlayer2) && !enablePlayerPerspective)
            {
                mode = SpectatorCameraMode.Overview;
            }
            
            currentCameraMode = mode;
            
            // Set focused player
            if (playerId >= 0)
            {
                focusedPlayerId = playerId;
            }
            else if (mode == SpectatorCameraMode.FollowPlayer1)
            {
                // Find player 1
                foreach (var player in playerStates.Values)
                {
                    focusedPlayerId = player.PlayerId;
                    break;
                }
            }
            else if (mode == SpectatorCameraMode.FollowPlayer2)
            {
                // Find player 2
                int count = 0;
                foreach (var player in playerStates.Values)
                {
                    if (count == 1)
                    {
                        focusedPlayerId = player.PlayerId;
                        break;
                    }
                    count++;
                }
            }
            
            Debug.Log($"Changed spectator camera mode to {mode}, focused on player {focusedPlayerId}");
        }
        
        /// <summary>
        /// Gets the current battle state.
        /// </summary>
        /// <returns>Dictionary of player battle states</returns>
        public Dictionary<int, PlayerBattleState> GetBattleState()
        {
            return playerStates;
        }
        
        /// <summary>
        /// Gets recent chat messages.
        /// </summary>
        /// <param name="count">Maximum number of messages to return</param>
        /// <returns>List of chat messages</returns>
        public List<SpectatorChatMessage> GetRecentChatMessages(int count = 20)
        {
            if (chatMessages.Count <= count)
            {
                return new List<SpectatorChatMessage>(chatMessages);
            }
            
            return chatMessages.GetRange(chatMessages.Count - count, count);
        }
        
        /// <summary>
        /// Checks if the player is currently spectating.
        /// </summary>
        /// <returns>True if spectating</returns>
        public bool IsSpectating()
        {
            return isSpectating;
        }
        
        /// <summary>
        /// Gets the ID of the match being spectated.
        /// </summary>
        /// <returns>Match ID, or -1 if not spectating</returns>
        public int GetSpectatedMatchId()
        {
            return currentMatchId;
        }
        
        /// <summary>
        /// Requests a battle state update from the server.
        /// </summary>
        private void RequestBattleStateUpdate()
        {
            if (!isSpectating) return;
            
            Dictionary<string, object> requestData = new Dictionary<string, object>
            {
                { "match_id", currentMatchId }
            };
            
            MultiplayerClient.Instance.SendMessage("request_battle_state", requestData);
        }
        
        /// <summary>
        /// Updates the spectator camera based on the current mode.
        /// </summary>
        private void UpdateSpectatorCamera()
        {
            if (spectatorCamera == null) return;
            
            // Calculate target camera position and rotation based on mode
            switch (currentCameraMode)
            {
                case SpectatorCameraMode.Overview:
                    CalculateOverviewCamera();
                    break;
                    
                case SpectatorCameraMode.FollowPlayer1:
                case SpectatorCameraMode.FollowPlayer2:
                    CalculateFollowCamera();
                    break;
                    
                case SpectatorCameraMode.FirstPerson:
                    CalculateFirstPersonCamera();
                    break;
                    
                case SpectatorCameraMode.TopDown:
                    CalculateTopDownCamera();
                    break;
                    
                case SpectatorCameraMode.FreeCam:
                    // Free cam is controlled by user input, no automatic updates
                    break;
            }
            
            // Smoothly move camera to target position and rotation
            spectatorCamera.position = Vector3.Lerp(spectatorCamera.position, targetCameraPosition, Time.deltaTime * cameraTransitionSpeed);
            spectatorCamera.rotation = Quaternion.Slerp(spectatorCamera.rotation, targetCameraRotation, Time.deltaTime * cameraTransitionSpeed);
        }
        
        /// <summary>
        /// Calculates camera position and rotation for overview mode.
        /// </summary>
        private void CalculateOverviewCamera()
        {
            if (playerStates.Count == 0) return;
            
            // Calculate center point between players
            Vector3 centerPoint = Vector3.zero;
            float maxDistance = 0f;
            
            foreach (var player in playerStates.Values)
            {
                centerPoint += player.Position;
            }
            
            centerPoint /= playerStates.Count;
            
            // Calculate maximum distance from center
            foreach (var player in playerStates.Values)
            {

                float distance = Vector3.Distance(centerPoint, player.Position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }

            // Set overview camera height and distance based on max player spread
            float overviewHeight = Mathf.Clamp(maxDistance * 1.5f, 10f, 50f);
            Vector3 offset = new Vector3(0f, overviewHeight, -overviewHeight);
            targetCameraPosition = centerPoint + offset;
            targetCameraRotation = Quaternion.LookRotation(centerPoint - targetCameraPosition);
        }

        /// <summary>
        /// Calculates camera position and rotation to follow a player.
        /// </summary>
        private void CalculateFollowCamera()
        {
            if (!playerStates.ContainsKey(focusedPlayerId)) return;

            PlayerBattleState player = playerStates[focusedPlayerId];
            Vector3 behindOffset = new Vector3(0f, 2f, -5f);
            targetCameraPosition = player.Position + player.Rotation * behindOffset;
            targetCameraRotation = Quaternion.LookRotation(player.Position - spectatorCamera.position);
        }

        /// <summary>
        /// Calculates camera position and rotation for first-person mode.
        /// </summary>
        private void CalculateFirstPersonCamera()
        {
            if (!playerStates.ContainsKey(focusedPlayerId)) return;

            PlayerBattleState player = playerStates[focusedPlayerId];
            targetCameraPosition = player.Position + new Vector3(0f, 1.6f, 0f); // eye-level
            targetCameraRotation = player.Rotation;
        }

        /// <summary>
        /// Calculates camera position and rotation for top-down view.
        /// </summary>
        private void CalculateTopDownCamera()
        {
            if (playerStates.Count == 0) return;

            Vector3 center = Vector3.zero;
            foreach (var player in playerStates.Values)
            {
                center += player.Position;
            }

            center /= playerStates.Count;

            float height = 30f;
            targetCameraPosition = center + new Vector3(0f, height, 0f);
            targetCameraRotation = Quaternion.Euler(90f, 0f, 0f); // look straight down
        }
    }
}