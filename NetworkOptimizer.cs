using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrawlAnything.Network;

namespace BrawlAnything.Network
{
    /// <summary>
    /// Optimizes network performance for multiplayer gameplay.
    /// Implements bandwidth optimization, latency compensation, and prediction techniques.
    /// </summary>
    public class NetworkOptimizer : MonoBehaviour
    {
        [Header("Bandwidth Optimization")]
        [SerializeField] private bool enableCompression = true;
        [SerializeField] private bool enableDeltaCompression = true;
        [SerializeField] private float positionPrecision = 0.01f;
        [SerializeField] private float rotationPrecision = 0.01f;
        [SerializeField] private int maxPacketSize = 1024;
        
        [Header("Latency Compensation")]
        [SerializeField] private bool enablePrediction = true;
        [SerializeField] private bool enableInterpolation = true;
        [SerializeField] private float interpolationDelay = 0.1f;
        [SerializeField] private int bufferSize = 10;
        
        [Header("Quality of Service")]
        [SerializeField] private bool enablePrioritization = true;
        [SerializeField] private bool adaptiveSendRate = true;
        [SerializeField] private float minSendRate = 10f;
        [SerializeField] private float maxSendRate = 30f;
        [SerializeField] private float adaptiveRateStep = 2f;
        
        [Header("Monitoring")]
        [SerializeField] private bool enableNetworkStats = true;
        [SerializeField] private float statsUpdateInterval = 1f;
        
        // Network statistics
        private float currentSendRate;
        private float currentLatency;
        private float currentPacketLoss;
        private float currentBandwidthUsage;
        private int sentPackets;
        private int receivedPackets;
        private int sentBytes;
        private int receivedBytes;
        private float lastStatsUpdateTime;
        
        // State caching for delta compression
        private Dictionary<string, object> lastSentState = new Dictionary<string, object>();
        private Dictionary<string, List<object>> stateBuffer = new Dictionary<string, List<object>>();
        
        // Singleton instance
        private static NetworkOptimizer _instance;
        public static NetworkOptimizer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkOptimizer>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("NetworkOptimizer");
                        _instance = go.AddComponent<NetworkOptimizer>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        // Events
        public event Action<NetworkStats> OnNetworkStatsUpdated;
        
        // Network statistics struct
        public struct NetworkStats
        {
            public float SendRate;
            public float Latency;
            public float PacketLoss;
            public float BandwidthUsage;
            public int SentPackets;
            public int ReceivedPackets;
            public int SentBytes;
            public int ReceivedBytes;
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
            
            // Initialize
            currentSendRate = minSendRate;
            lastStatsUpdateTime = Time.time;
        }
        
        private void OnEnable()
        {
            // Register for network events
            if (MultiplayerClient.Instance != null)
            {
                MultiplayerClient.Instance.OnMessageReceived += HandleMessageReceived;
            }
        }
        
        private void OnDisable()
        {
            // Unregister from network events
            if (MultiplayerClient.Instance != null)
            {
                MultiplayerClient.Instance.OnMessageReceived -= HandleMessageReceived;
            }
        }

        /// <summary>
        /// Dynamically adjusts the network send rate based on current latency and packet loss.
        /// </summary>
        private void AdjustSendRate()
        {
            const float highLatencyThreshold = 100f;   // ms
            const float highPacketLossThreshold = 0.05f; // 5%
            const float lowLatencyThreshold = 50f;     // ms
            const float lowPacketLossThreshold = 0.01f; // 1%

            bool shouldDecrease =
                currentLatency > highLatencyThreshold || currentPacketLoss > highPacketLossThreshold;

            bool shouldIncrease =
                currentLatency < lowLatencyThreshold && currentPacketLoss < lowPacketLossThreshold;

            if (shouldDecrease)
            {
                currentSendRate = Mathf.Max(minSendRate, currentSendRate - adaptiveRateStep);
            }
            else if (shouldIncrease)
            {
                currentSendRate = Mathf.Min(maxSendRate, currentSendRate + adaptiveRateStep);
            }

            // Clamp to bounds to be safe
            currentSendRate = Mathf.Clamp(currentSendRate, minSendRate, maxSendRate);
        }

        private void Update()
        {
            // Update network statistics
            if (enableNetworkStats && Time.time - lastStatsUpdateTime >= statsUpdateInterval)
            {
                UpdateNetworkStats();
                lastStatsUpdateTime = Time.time;
            }
            
            // Adjust send rate based on network conditions
            if (adaptiveSendRate)
            {
                AdjustSendRate();
            }
        }

        /// <summary>
        /// Updates current network statistics and triggers OnNetworkStatsUpdated event.
        /// </summary>
        private void UpdateNetworkStats()
        {
            float elapsed = Time.time - lastStatsUpdateTime;
            if (elapsed <= 0f) return;

            currentSendRate = Mathf.Clamp(currentSendRate, minSendRate, maxSendRate);

            // Estimate bandwidth usage in KB/s
            currentBandwidthUsage = (sentBytes + receivedBytes) / 1024f / elapsed;

            // Placeholder for latency and packet loss simulation (in real scenario, calculate from ping/ack)
            currentLatency = UnityEngine.Random.Range(30f, 80f); // Simulated ping in ms
            currentPacketLoss = UnityEngine.Random.Range(0f, 0.02f); // Simulated loss 0–2%

            NetworkStats stats = new NetworkStats
            {
                SendRate = currentSendRate,
                Latency = currentLatency,
                PacketLoss = currentPacketLoss,
                BandwidthUsage = currentBandwidthUsage,
                SentPackets = sentPackets,
                ReceivedPackets = receivedPackets,
                SentBytes = sentBytes,
                ReceivedBytes = receivedBytes
            };

            // Invoke event
            OnNetworkStatsUpdated?.Invoke(stats);

            // Reset counters
            sentPackets = 0;
            receivedPackets = 0;
            sentBytes = 0;
            receivedBytes = 0;
        }


        /// <summary>
        /// Optimizes a position vector for network transmission.
        /// </summary>
        /// <param name="position">Original position</param>
        /// <returns>Optimized position</returns>
        public Vector3 OptimizePosition(Vector3 position)
        {
            if (!enableCompression) return position;
            
            // Quantize position based on precision
            return new Vector3(
                Mathf.Round(position.x / positionPrecision) * positionPrecision,
                Mathf.Round(position.y / positionPrecision) * positionPrecision,
                Mathf.Round(position.z / positionPrecision) * positionPrecision
            );
        }
        
        /// <summary>
        /// Optimizes a rotation quaternion for network transmission.
        /// </summary>
        /// <param name="rotation">Original rotation</param>
        /// <returns>Optimized rotation</returns>
        public Quaternion OptimizeRotation(Quaternion rotation)
        {
            if (!enableCompression) return rotation;
            
            // Quantize rotation based on precision
            return new Quaternion(
                Mathf.Round(rotation.x / rotationPrecision) * rotationPrecision,
                Mathf.Round(rotation.y / rotationPrecision) * rotationPrecision,
                Mathf.Round(rotation.z / rotationPrecision) * rotationPrecision,
                Mathf.Round(rotation.w / rotationPrecision) * rotationPrecision
            ).normalized;
        }
        
        /// <summary>
        /// Optimizes a dictionary for network transmission using delta compression.
        /// </summary>
        /// <param name="stateId">Unique identifier for this state type</param>
        /// <param name="state">Original state dictionary</param>
        /// <returns>Optimized state dictionary</returns>
        public Dictionary<string, object> OptimizeState(string stateId, Dictionary<string, object> state)
        {
            if (!enableDeltaCompression || !lastSentState.ContainsKey(stateId))
            {
                // Store full state for future delta compression
                lastSentState[stateId] = DeepCopy(state);
                return state;
            }
            
            // Get last sent state
            var lastState = lastSentState[stateId] as Dictionary<string, object>;
            if (lastState == null)
            {
                lastSentState[stateId] = DeepCopy(state);
                return state;
            }
            
            // Create delta state
            Dictionary<string, object> deltaState = new Dictionary<string, object>();
            deltaState["_isDelta"] = true;
            
            // Compare and add only changed values
            foreach (var kvp in state)
            {
                if (!lastState.ContainsKey(kvp.Key) || !DeepEquals(lastState[kvp.Key], kvp.Value))
                {
                    deltaState[kvp.Key] = kvp.Value;
                }
            }
            
            // Add keys for removed values
            foreach (var kvp in lastState)
            {
                if (!state.ContainsKey(kvp.Key))
                {
                    deltaState[kvp.Key] = null;
                }
            }
            
            // Update last sent state
            lastSentState[stateId] = DeepCopy(state);
            
            // If delta is larger than original, just send original
            if (EstimateSize(deltaState) >= EstimateSize(state))
            {
                return state;
            }
            
            return deltaState;
        }
        
        /// <summary>
        /// Applies delta compression to reconstruct the full state.
        /// </summary>
        /// <param name="stateId">Unique identifier for this state type</param>
        /// <param name="state">Received state dictionary</param>
        /// <returns>Reconstructed state dictionary</returns>
        public Dictionary<string, object> ReconstructState(string stateId, Dictionary<string, object> state)
        {
            // Check if this is a delta state
            if (!state.ContainsKey("_isDelta") || !(bool)state["_isDelta"])
            {
                // Store full state for future delta reconstruction
                if (!lastSentState.ContainsKey(stateId))
                {
                    lastSentState[stateId] = DeepCopy(state);
                }
                return state;
            }
            
            // Get last state
            if (!lastSentState.ContainsKey(stateId))
            {
                // No previous state to apply delta to
                state.Remove("_isDelta");
                return state;
            }
            
            var lastState = lastSentState[stateId] as Dictionary<string, object>;
            if (lastState == null)
            {
                state.Remove("_isDelta");
                return state;
            }
            
            // Create reconstructed state
            Dictionary<string, object> reconstructedState = DeepCopy(lastState) as Dictionary<string, object>;
            
            // Apply delta changes
            foreach (var kvp in state)
            {
                if (kvp.Key == "_isDelta") continue;
                
                if (kvp.Value == null)
                {
                    // Remove key
                    reconstructedState.Remove(kvp.Key);
                }
                else
                {
                    // Update key
                    reconstructedState[kvp.Key] = kvp.Value;
                }
            }
            
            // Store reconstructed state
            lastSentState[stateId] = DeepCopy(reconstructedState);
            
            return reconstructedState;
        }
        
        /// <summary>
        /// Adds a state to the interpolation buffer.
        /// </summary>
        /// <param name="stateId">Unique identifier for this state type</param>
        /// <param name="state">State to buffer</param>
        /// <param name="timestamp">Timestamp of the state</param>
        public void BufferState(string stateId, object state, float timestamp)
        {
            if (!enableInterpolation) return;
            
            // Create buffer entry
            Dictionary<string, object> entry = new Dictionary<string, object>
            {
                { "state", state },
                { "timestamp", timestamp }
            };
            
            // Initialize buffer if needed
            if (!stateBuffer.ContainsKey(stateId))
            {
                stateBuffer[stateId] = new List<object>();
            }
            
            // Add to buffer
            stateBuffer[stateId].Add(entry);
            
            // Trim buffer if too large
            while (stateBuffer[stateId].Count > bufferSize)
            {
                stateBuffer[stateId].RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Gets interpolated state from the buffer.
        /// </summary>
        /// <param name="stateId">Unique identifier for this state type</param>
        /// <param name="renderTime">Time to interpolate for</param>
        /// <returns>Interpolated state, or null if not available</returns>
        public object GetInterpolatedState(string stateId, float renderTime)
        {
            if (!enableInterpolation || !stateBuffer.ContainsKey(stateId) || stateBuffer[stateId].Count < 2)
            {
                return null;
            }
            
            // Find states to interpolate between
            Dictionary<string, object> beforeState = null;
            Dictionary<string, object> afterState = null;
            float beforeTime = 0;
            float afterTime = 0;
            
            foreach (var entry in stateBuffer[stateId])
            {
                var stateEntry = entry as Dictionary<string, object>;
                if (stateEntry == null) continue;
                
                float timestamp = Convert.ToSingle(stateEntry["timestamp"]);
                
                if (timestamp <= renderTime)
                {
                    // This state is before or at render time
                    if (beforeState == null || timestamp > beforeTime)
                    {
                        beforeState = stateEntry;
                        beforeTime = timestamp;
                    }
                }
                else
                {
                    // This state is after render time
                    if (afterState == null || timestamp < afterTime)
                    {
                        afterState = stateEntry;
                        afterTime = timestamp;
                    }
                }
            }
            
            // If we don't have both states, return the closest one
            if (beforeState == null) return afterState?["state"];
            if (afterState == null) return beforeState?["state"];
            
            // Calculate interpolation factor
            float t = (renderTime - beforeTime) / (afterTime - beforeTime);
            t = Mathf.Clamp01(t);
            
            // Interpolate between states
            return InterpolateStates(beforeState["state"], afterState["state"], t);
        }
        
        /// <summary>
        /// Interpolates between two states.
        /// </summary>
        /// <param name="stateA">First state</param>
        /// <param name="stateB">Second state</param>
        /// <param name="t">Interpolation factor (0-1)</param>
        /// <returns>Interpolated state</returns>
        private object InterpolateStates(object stateA, object stateB, float t)
        {
            // Handle different state types
            if (stateA is Dictionary<string, object> && stateB is Dictionary<string, object>)
            {
                return InterpolateDictionaries(stateA as Dictionary<string, object>, stateB as Dictionary<string, object>, t);
            }
            else if (stateA is Vector3 && stateB is Vector3)
            {
                return Vector3.Lerp((Vector3)stateA, (Vector3)stateB, t);
            }
            else if (stateA is Quaternion && stateB is Quaternion)
            {
                return Quaternion.Slerp((Quaternion)stateA, (Quaternion)stateB, t);
            }
            else if (stateA is float && stateB is float)
            {
                return Mathf.Lerp((float)stateA, (float)stateB, t);
            }
            else
            {
                // For non-interpolatable types, return the second state
                return stateB;
            }
        }

        /// <summary>
        /// Interpolates between two dictionaries.
        /// </summary>
        /// <param name="dictA">First dictionary</param>
        /// <param name="dictB">Second dictionary</param>
        /// <param name="t">Interpolation factor (0-1)</param>
        /// <returns>Interpolated dictionary</returns>
        private Dictionary<string, object> InterpolateDictionaries(Dictionary<string, object> dictA, Dictionary<string, object> dictB, float t)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            foreach (var key in dictA.Keys)
            {
                if (!dictB.ContainsKey(key)) continue;

                object valueA = dictA[key];
                object valueB = dictB[key];

                if (valueA is float && valueB is float)
                {
                    result[key] = Mathf.Lerp((float)valueA, (float)valueB, t);
                }
                else if (valueA is int && valueB is int)
                {
                    float interpolated = Mathf.Lerp((int)valueA, (int)valueB, t);
                    result[key] = Mathf.RoundToInt(interpolated);
                }
                else if (valueA is Vector3 && valueB is Vector3)
                {
                    result[key] = Vector3.Lerp((Vector3)valueA, (Vector3)valueB, t);
                }
                else if (valueA is Quaternion && valueB is Quaternion)
                {
                    result[key] = Quaternion.Slerp((Quaternion)valueA, (Quaternion)valueB, t);
                }
                else
                {
                    // Default to valueB if not interpolatable
                    result[key] = valueB;
                }
            }

            return result;
        }

        /// <summary>
        /// Clears all buffered states.
        /// </summary>
        public void ClearBuffer()
        {
            stateBuffer.Clear();
        }

        /// <summary>
        /// Enables or disables interpolation.
        /// </summary>
        /// <param name="enabled">Whether interpolation should be active</param>
        public void SetInterpolationEnabled(bool enabled)
        {
            enableInterpolation = enabled;
            if (!enabled)
            {
                ClearBuffer();
            }
        }

        private void HandleMessageReceived(string messageType, Dictionary<string, object> messageData)
        {
            if (string.IsNullOrEmpty(messageType) || messageData == null)
            {
                Debug.LogWarning("Received invalid or empty network message.");
                return;
            }

            string stateId = messageType;

            // Reconstruct full state if delta compressed
            Dictionary<string, object> fullState = ReconstructState(stateId, messageData);

            // Timestamp handling
            float timestamp = Time.time; // or use server timestamp if provided

            // Buffer the reconstructed state
            BufferState(stateId, fullState, timestamp);

            // If interpolation is disabled, you might want to apply the state immediately
            if (!IsInterpolationEnabled)
            {
                ApplyStateDirectly(stateId, fullState);
            }

            // Optionally, log receipt for debug or stats
            receivedPackets++;
            receivedBytes += EstimateSize(fullState);
        }

        private void ApplyStateDirectly(string stateId, Dictionary<string, object> state)
        {
            // Placeholder — replace with actual logic to apply state to a game object or system
            Debug.Log($"Applying state '{stateId}' directly: {JsonUtility.ToJson(new SerializableDictionaryWrapper(state))}");
        }

        [Serializable]
        public class SerializableKeyValuePair
        {
            public string key;
            public string value;
        }

        [Serializable]
        public class SerializableDictionaryWrapper
        {
            public List<SerializableKeyValuePair> entries = new();

            public SerializableDictionaryWrapper(Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    entries.Add(new SerializableKeyValuePair
                    {
                        key = kvp.Key,
                        value = kvp.Value?.ToString() ?? "null"
                    });
                }
            }
        }

        // Private state
        //private Dictionary<string, List<object>> stateBuffer = new Dictionary<string, List<object>>();
        //private bool enableInterpolation = true;
        //private int bufferSize = 20;

        // Optionally expose these via properties
        public bool IsInterpolationEnabled => enableInterpolation;
        public int BufferSize
        {
            get => bufferSize;
            set => bufferSize = Mathf.Max(2, value);
        }

        private Dictionary<string, object> DeepCopy(Dictionary<string, object> original)
        {
            var copy = new Dictionary<string, object>(original.Count);
            foreach (var kvp in original)
            {
                if (kvp.Value is Dictionary<string, object> subDict)
                {
                    copy[kvp.Key] = DeepCopy(subDict);  // Recursively deep copy dictionaries
                }
                else
                {
                    copy[kvp.Key] = kvp.Value;  // Simple value copy
                }
            }
            return copy;
        }

        private bool DeepEquals(object objA, object objB)
        {
            if (objA == null && objB == null) return true;
            if (objA == null || objB == null) return false;

            // If objects are dictionaries, compare their contents
            if (objA is Dictionary<string, object> dictA && objB is Dictionary<string, object> dictB)
            {
                if (dictA.Count != dictB.Count) return false;
                foreach (var kvp in dictA)
                {
                    if (!dictB.ContainsKey(kvp.Key) || !DeepEquals(kvp.Value, dictB[kvp.Key]))
                    {
                        return false;
                    }
                }
                return true;
            }

            // Handle other types (e.g., primitives, structs, etc.)
            return objA.Equals(objB);
        }
        /// <summary>
        /// Estimates the size of a given state object in bytes.
        /// </summary>
        /// <param name="state">The state object, typically a dictionary, to estimate the size of.</param>
        /// <returns>Estimated size in bytes.</returns>
        private int EstimateSize(Dictionary<string, object> state)
        {
            int size = 0;

            foreach (var kvp in state)
            {
                // Add the size of the key
                size += EstimateStringSize(kvp.Key);

                // Add the size of the value based on its type
                if (kvp.Value is int)
                {
                    size += sizeof(int); // 4 bytes for an int
                }
                else if (kvp.Value is float)
                {
                    size += sizeof(float); // 4 bytes for a float
                }
                else if (kvp.Value is Vector3)
                {
                    size += 3 * sizeof(float); // 3 floats for Vector3
                }
                else if (kvp.Value is Quaternion)
                {
                    size += 4 * sizeof(float); // 4 floats for Quaternion
                }
                else if (kvp.Value is string str)
                {
                    size += EstimateStringSize(str);
                }
                else if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    size += EstimateSize(nestedDict); // Recursively estimate size for nested dictionaries
                }
                else if (kvp.Value is List<object> list)
                {
                    foreach (var item in list)
                    {
                        size += EstimateObjectSize(item);
                    }
                }
                else
                {
                    // Fallback for other types, assuming they are reference types
                    size += sizeof(int); // Use a reference size estimate (e.g., pointer size)
                }
            }

            return size;
        }

        /// <summary>
        /// Estimates the size of a string in bytes.
        /// </summary>
        /// <param name="str">The string to estimate the size of.</param>
        /// <returns>Estimated size in bytes.</returns>
        private int EstimateStringSize(string str)
        {
            if (str == null) return 0;

            // Estimate based on UTF-16 encoding (2 bytes per character)
            return str.Length * 2;
        }

        /// <summary>
        /// Estimates the size of an object in bytes.
        /// </summary>
        /// <param name="obj">The object to estimate the size of.</param>
        /// <returns>Estimated size in bytes.</returns>
        private int EstimateObjectSize(object obj)
        {
            if (obj is int)
            {
                return sizeof(int);
            }
            else if (obj is float)
            {
                return sizeof(float);
            }
            else if (obj is Vector3)
            {
                return 3 * sizeof(float);
            }
            else if (obj is Quaternion)
            {
                return 4 * sizeof(float);
            }
            else if (obj is string str)
            {
                return EstimateStringSize(str);
            }
            else if (obj is Dictionary<string, object> dict)
            {
                return EstimateSize(dict);
            }
            else if (obj is List<object> list)
            {
                int listSize = 0;
                foreach (var item in list)
                {
                    listSize += EstimateObjectSize(item);
                }
                return listSize;
            }
            else
            {
                // Fallback for unknown types
                return sizeof(int); // Reference size estimate
            }
        }

    }
}