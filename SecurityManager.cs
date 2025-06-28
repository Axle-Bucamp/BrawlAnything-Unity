using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using BrawlAnything.Network;

namespace BrawlAnything.Security
{
    /// <summary>
    /// Implements security measures and anti-cheat mechanisms for the multiplayer game.
    /// </summary>
    public class SecurityManager : MonoBehaviour
    {
        [Header("Security Settings")]
        [SerializeField] private bool enableClientValidation = true;
        [SerializeField] private bool enableServerAuthoritative = true;
        [SerializeField] private bool enableActionValidation = true;
        [SerializeField] private bool enableAntiCheatMeasures = true;
        
        [Header("Anti-Cheat Settings")]
        [SerializeField] private bool enableMovementValidation = true;
        [SerializeField] private bool enableRateValidation = true;
        [SerializeField] private bool enableStateValidation = true;
        [SerializeField] private float maxMovementSpeed = 10f;
        [SerializeField] private float maxActionRate = 5f;
        [SerializeField] private float suspicionThreshold = 0.8f;
        
        [Header("Encryption")]
        [SerializeField] private bool enableMessageEncryption = false;
        [SerializeField] private bool enableMessageSigning = true;
        
        // Security state
        private Dictionary<int, PlayerSecurityState> playerSecurityStates = new Dictionary<int, PlayerSecurityState>();
        private Dictionary<string, long> messageNonces = new Dictionary<string, long>();
        private string clientSecretKey;
        
        // Singleton instance
        private static SecurityManager _instance;
        public static SecurityManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SecurityManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SecurityManager");
                        _instance = go.AddComponent<SecurityManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        // Events
        public event Action<int, string, float> OnCheatDetected;
        public event Action<string> OnSecurityViolation;
        
        // Player security state class
        private class PlayerSecurityState
        {
            public int PlayerId;
            public Vector3 LastPosition;
            public float LastActionTime;
            public int ActionCount;
            public float ActionCountResetTime;
            public float SuspicionLevel;
            public List<string> DetectedViolations;
            
            public PlayerSecurityState(int playerId)
            {
                PlayerId = playerId;
                LastPosition = Vector3.zero;
                LastActionTime = 0f;
                ActionCount = 0;
                ActionCountResetTime = Time.time + 1f;
                SuspicionLevel = 0f;
                DetectedViolations = new List<string>();
            }
            
            public void AddViolation(string violation, float severity)
            {
                DetectedViolations.Add($"{Time.time}: {violation} (Severity: {severity})");
                SuspicionLevel += severity;
                
                // Cap suspicion level
                SuspicionLevel = Mathf.Clamp01(SuspicionLevel);
            }
            
            public void DecaySuspicion(float amount)
            {
                SuspicionLevel = Mathf.Max(0f, SuspicionLevel - amount);
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
            
            // Generate client secret key
            clientSecretKey = GenerateRandomKey();
        }
        
        private void OnEnable()
        {
            // Register for network events
            if (MultiplayerClient.Instance != null)
            {
                MultiplayerClient.Instance.OnMessageReceived += ValidateIncomingMessage;
                MultiplayerClient.Instance.OnConnected += OnConnected;
            }
        }
        
        private void OnDisable()
        {
            // Unregister from network events
            if (MultiplayerClient.Instance != null)
            {
                MultiplayerClient.Instance.OnMessageReceived -= ValidateIncomingMessage;
                MultiplayerClient.Instance.OnConnected -= OnConnected;
            }
        }
        
        private void Update()
        {
            // Decay suspicion levels over time
            foreach (var state in playerSecurityStates.Values)
            {
                state.DecaySuspicion(0.01f * Time.deltaTime);
                
                // Reset action count periodically
                if (Time.time >= state.ActionCountResetTime)
                {
                    state.ActionCount = 0;
                    state.ActionCountResetTime = Time.time + 1f;
                }
            }
            
            // Clean up old nonces
            CleanupOldNonces();
        }
        
        private void OnConnected()
        {
            // Perform initial security handshake
            if (enableMessageSigning || enableMessageEncryption)
            {
                PerformSecurityHandshake();
            }
        }
        
        /// <summary>
        /// Performs security handshake with the server.
        /// </summary>
        private void PerformSecurityHandshake()
        {
            // In a real implementation, this would involve a proper key exchange
            // For now, we'll just simulate it
            
            // Generate a nonce
            string nonce = GenerateNonce();
            
            // Send handshake message
            Dictionary<string, object> handshakeData = new Dictionary<string, object>
            {
                { "client_id", SystemInfo.deviceUniqueIdentifier },
                { "nonce", nonce },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                { "client_version", Application.version }
            };
            
            // Add signature
            if (enableMessageSigning)
            {
                handshakeData["signature"] = SignMessage(handshakeData);
            }
            
            // Send handshake message
            MultiplayerClient.Instance.SendMessage("security_handshake", handshakeData);
            
            Debug.Log("Security handshake initiated");
        }
        
        /// <summary>
        /// Validates an incoming network message.
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <param name="data">Message data</param>
        private void ValidateIncomingMessage(string messageType, Dictionary<string, object> data)
        {
            if (!enableClientValidation) return;
            
            // Validate message signature if enabled
            if (enableMessageSigning && data.ContainsKey("signature"))
            {
                string signature = data["signature"].ToString();
                Dictionary<string, object> messageData = new Dictionary<string, object>(data);
                messageData.Remove("signature");
                
                if (!VerifySignature(messageData, signature))
                {
                    // Invalid signature
                    ReportSecurityViolation("Invalid message signature");
                    return;
                }
            }
            
            // Validate message nonce if present
            if (data.ContainsKey("nonce"))
            {
                string nonce = data["nonce"].ToString();
                if (!ValidateNonce(nonce))
                {
                    // Replay attack detected
                    ReportSecurityViolation("Message replay detected");
                    return;
                }
            }
            
            // Validate message timestamp if present
            if (data.ContainsKey("timestamp"))
            {
                long timestamp = Convert.ToInt64(data["timestamp"]);
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long timeDiff = Math.Abs(currentTime - timestamp);
                
                if (timeDiff > 30000)  // 30 seconds
                {
                    // Message too old or from the future
                    ReportSecurityViolation("Message timestamp invalid");
                    return;
                }
            }
            
            // Process specific message types
            switch (messageType)
            {
                case "player_action":
                    ValidatePlayerAction(data);
                    break;
                    
                case "game_state":
                    ValidateGameState(data);
                    break;
                    
                case "ar_sync":
                    ValidateARSync(data);
                    break;
            }
        }
        
        /// <summary>
        /// Validates a player action message.
        /// </summary>
        /// <param name="data">Action data</param>
        private void ValidatePlayerAction(Dictionary<string, object> data)
        {
            if (!enableActionValidation) return;
            
            if (data.ContainsKey("data"))
            {
                var actionData = data["data"] as Dictionary<string, object>;
                if (actionData != null)
                {
                    // Get player ID
                    int playerId = 0;
                    if (actionData.ContainsKey("user_id"))
                    {
                        playerId = Convert.ToInt32(actionData["user_id"]);
                    }
                    
                    // Get or create player security state
                    if (!playerSecurityStates.ContainsKey(playerId))
                    {
                        playerSecurityStates[playerId] = new PlayerSecurityState(playerId);
                    }
                    
                    var securityState = playerSecurityStates[playerId];
                    
                    // Validate action rate
                    if (enableRateValidation)
                    {
                        securityState.ActionCount++;
                        
                        if (securityState.ActionCount > maxActionRate)
                        {
                            // Too many actions in a short time
                            securityState.AddViolation("Action rate exceeded", 0.2f);
                            
                            if (securityState.SuspicionLevel >= suspicionThreshold)
                            {
                                ReportCheat(playerId, "Action rate manipulation", securityState.SuspicionLevel);
                            }
                        }
                    }
                    
                    // Validate movement if present
                    if (enableMovementValidation && actionData.ContainsKey("action_type") && actionData["action_type"].ToString() == "move")
                    {
                        if (actionData.ContainsKey("position"))
                        {
                            var positionData = actionData["position"] as Dictionary<string, object>;
                            if (positionData != null)
                            {
                                Vector3 newPosition = new Vector3(
                                    Convert.ToSingle(positionData["x"]),
                                    Convert.ToSingle(positionData["y"]),
                                    Convert.ToSingle(positionData["z"])
                                );
                                
                                // Check if movement is valid
                                if (securityState.LastPosition != Vector3.zero)
                                {
                                    float distance = Vector3.Distance(newPosition, securityState.LastPosition);
                                    float timeDelta = Time.time - securityState.LastActionTime;
                                    
                                    if (timeDelta > 0)
                                    {
                                        float speed = distance / timeDelta;
                                        
                                        if (speed > maxMovementSpeed)
                                        {
                                            // Movement speed too high
                                            securityState.AddViolation($"Speed hack detected ({speed} > {maxMovementSpeed})", 0.3f);
                                            
                                            if (securityState.SuspicionLevel >= suspicionThreshold)
                                            {
                                                ReportCheat(playerId, "Speed hack", securityState.SuspicionLevel);
                                            }
                                        }
                                    }
                                }
                                
                                // Update last position
                                securityState.LastPosition = newPosition;
                            }
                        }
                    }
                    
                    // Update last action time
                    securityState.LastActionTime = Time.time;
                }
            }
        }
        
        /// <summary>
        /// Validates a game state message.
        /// </summary>
        /// <param name="data">Game state data</param>
        private void ValidateGameState(Dictionary<string, object> data)
        {
            if (!enableStateValidation) return;
            
            // In a real implementation, this would validate the game state against expected values
            // For now, we'll just do basic checks
            
            if (data.ContainsKey("data"))
            {
                var stateData = data["data"] as Dictionary<string, object>;
                if (stateData != null && stateData.ContainsKey("state"))
                {
                    var state = stateData["state"] as Dictionary<string, object>;
                    if (state != null)
                    {
                        // Check for invalid character stats
                        if (state.ContainsKey("characters"))
                        {
                            var characters = state["characters"] as Dictionary<string, object>;
                            if (characters != null)
                            {
                                foreach (var characterEntry in characters)
                                {
                                    var character = characterEntry.Value as Dictionary<string, object>;
                                    if (character != null)
                                    {
                                        // Check for invalid health values
                                        if (character.ContainsKey("health"))
                                        {
                                            float health = Convert.ToSingle(character["health"]);
                                            if (health < 0 || health > 100)
                                            {
                                                // Invalid health value

                                                if (health < 0 || health > 100)
                                                {
                                                    ReportSecurityViolation($"Invalid character health value: {health}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates AR synchronization messages.
        /// </summary>
        /// <param name="data">AR sync data</param>
        private void ValidateARSync(Dictionary<string, object> data)
        {
            // Placeholder for AR data validation
            // Could include anchor integrity, transform mismatch, timing, etc.
        }

        /// <summary>
        /// Generates a random nonce string.
        /// </summary>
        private string GenerateNonce()
        {
            byte[] bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Validates nonce to prevent replay attacks.
        /// </summary>
        private bool ValidateNonce(string nonce)
        {
            if (messageNonces.ContainsKey(nonce)) return false;
            messageNonces[nonce] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }

        /// <summary>
        /// Cleans up expired nonces.
        /// </summary>
        private void CleanupOldNonces()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            List<string> toRemove = new List<string>();

            foreach (var kvp in messageNonces)
            {
                if (now - kvp.Value > 60) // 1 minute validity
                    toRemove.Add(kvp.Key);
            }

            foreach (string key in toRemove)
                messageNonces.Remove(key);
        }

        /// <summary>
        /// Generates a secure random key for message signing.
        /// </summary>
        private string GenerateRandomKey()
        {
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);
            return Convert.ToBase64String(key);
        }

        /// <summary>
        /// Signs a message using HMAC-SHA256.
        /// </summary>
        private string SignMessage(Dictionary<string, object> message)
        {
            string json = JsonUtility.ToJson(new SerializationWrapper(message));
            byte[] key = Encoding.UTF8.GetBytes(clientSecretKey);
            byte[] data = Encoding.UTF8.GetBytes(json);

            using (var hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Verifies message signature.
        /// </summary>
        private bool VerifySignature(Dictionary<string, object> message, string signature)
        {
            string expectedSignature = SignMessage(message);
            return expectedSignature == signature;
        }

        /// <summary>
        /// Reports a cheat detection event.
        /// </summary>
        private void ReportCheat(int playerId, string reason, float severity)
        {
            Debug.LogWarning($"[SECURITY] Cheat detected for player {playerId}: {reason} (Suspicion: {severity})");
            OnCheatDetected?.Invoke(playerId, reason, severity);
        }

        /// <summary>
        /// Reports a security violation.
        /// </summary>
        private void ReportSecurityViolation(string reason)
        {
            Debug.LogError($"[SECURITY] Violation: {reason}");
            OnSecurityViolation?.Invoke(reason);
        }

        /// <summary>
        /// Helper class to serialize dictionary to JSON.
        /// </summary>
        [Serializable]
        private class SerializationWrapper
        {
            public Dictionary<string, object> data;
            public SerializationWrapper(Dictionary<string, object> dict)
            {
                data = dict;
            }
        }
    }
}