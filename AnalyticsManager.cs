using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace BrawlAnything.Analytics
{
    /// <summary>
    /// Client d'analyse pour Unity qui collecte et envoie des données d'utilisation et de performance.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string apiKey = "";
        [SerializeField] private string serverUrl = "https://analytics.brawlanything.com/ingest";
        [SerializeField] private int batchSize = 10;
        [SerializeField] private float flushInterval = 30f;
        [SerializeField] private bool debugMode = false;

        [Header("Privacy Settings")]
        [SerializeField] private bool requireExplicitConsent = true;
        [SerializeField] private bool showPrivacyPromptOnStart = true;

        // Singleton
        private static AnalyticsManager _instance;
        public static AnalyticsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AnalyticsManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AnalyticsManager");
                        _instance = go.AddComponent<AnalyticsManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Internal State
        private string userId;
        private string sessionId;
        private bool isInitialized;
        private float lastFlushTime;
        private Coroutine flushCoroutine;

        // Consent & batching
        private UserConsent userConsent = new UserConsent();
        private List<AnalyticsEvent> eventBatch = new List<AnalyticsEvent>();
        private object batchLock = new object();

        // Structures
        [Serializable]
        public class UserConsent
        {
            public bool analytics;
            public bool crashReporting;
            public bool performanceMonitoring;
            public bool personalization;

            public UserConsent() { }

            public UserConsent(bool all)
            {
                analytics = crashReporting = performanceMonitoring = personalization = all;
            }
        }

        [Serializable]
        private class AnalyticsEvent
        {
            public string event_id;
            public string event_name;
            public long timestamp;
            public string user_id;
            public string session_id;
            public Dictionary<string, object> properties;
            public string app_version;
            public string platform;
            public string device_model;

            public AnalyticsEvent(string name, Dictionary<string, object> props = null)
            {
                event_id = Guid.NewGuid().ToString();
                event_name = name;
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                user_id = Instance.userId;
                session_id = Instance.sessionId;
                properties = props ?? new Dictionary<string, object>();
                app_version = Application.version;
                platform = Application.platform.ToString();
                device_model = SystemInfo.deviceModel;
            }
        }

        [Serializable]
        private class EventBatch
        {
            public List<AnalyticsEvent> events;
            public EventBatch(List<AnalyticsEvent> events) => this.events = events;
        }

        [Serializable]
        private class SerializableDictionary
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();

            public SerializableDictionary(Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value?.ToString() ?? "null");
                }
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

            sessionId = Guid.NewGuid().ToString();
            userId = LoadUserId();
            LoadUserConsent();

            if (debugMode) Debug.Log($"[Analytics] Initialized with Session ID: {sessionId}");
        }

        private void Start()
        {
            if (showPrivacyPromptOnStart && requireExplicitConsent && !HasUserConsent())
            {
                ShowPrivacyPrompt();
            }
            else if (!requireExplicitConsent)
            {
                SetUserConsent(new UserConsent(true));
                Initialize();
            }
            else if (HasUserConsent())
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            if (isInitialized) Flush();
        }

        public void Initialize()
        {
            if (isInitialized) return;

            isInitialized = true;
            lastFlushTime = Time.time;
            flushCoroutine = StartCoroutine(AutoFlushCoroutine());

            TrackEvent("app_start", new Dictionary<string, object> { { "first_run", IsFirstRun() } });

            if (debugMode) Debug.Log("[Analytics] Client initialized");
        }

        public void TrackEvent(string eventName, Dictionary<string, object> properties = null)
        {
            if (!isInitialized || !CanTrackEvent(eventName)) return;

            var evt = new AnalyticsEvent(eventName, properties);

            lock (batchLock)
            {
                eventBatch.Add(evt);
                if (eventBatch.Count >= batchSize) Flush();
            }

            if (debugMode) Debug.Log($"[Analytics] Event tracked: {eventName}");
        }

        public void Flush()
        {
            List<AnalyticsEvent> batchCopy;
            lock (batchLock)
            {
                if (eventBatch.Count == 0) return;
                batchCopy = new List<AnalyticsEvent>(eventBatch);
                eventBatch.Clear();
            }

            foreach (var evt in batchCopy)
            {
                if (debugMode) Debug.Log($"[Analytics] Flushing event: {JsonConvert.SerializeObject(evt)}");
                // TODO: send to server
            }
        }

        private IEnumerator AutoFlushCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(flushInterval);
                Flush();
            }
        }

        private string LoadUserId()
        {
            return PlayerPrefs.GetString("analytics_user_id", Guid.NewGuid().ToString());
        }

        private void SaveUserId(string id)
        {
            PlayerPrefs.SetString("analytics_user_id", id);
            PlayerPrefs.Save();
        }

        private bool IsFirstRun()
        {
            return PlayerPrefs.GetInt("analytics_first_run", 1) == 1;
        }

        private void SaveUserConsent(UserConsent consent)
        {
            PlayerPrefs.SetInt("consent_analytics", consent.analytics ? 1 : 0);
            PlayerPrefs.SetInt("consent_crash", consent.crashReporting ? 1 : 0);
            PlayerPrefs.SetInt("consent_perf", consent.performanceMonitoring ? 1 : 0);
            PlayerPrefs.SetInt("consent_personal", consent.personalization ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadUserConsent()
        {
            userConsent = new UserConsent
            {
                analytics = PlayerPrefs.GetInt("consent_analytics", 0) == 1,
                crashReporting = PlayerPrefs.GetInt("consent_crash", 0) == 1,
                performanceMonitoring = PlayerPrefs.GetInt("consent_perf", 0) == 1,
                personalization = PlayerPrefs.GetInt("consent_personal", 0) == 1
            };
        }

        public void SetUserId(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                userId = id;
                SaveUserId(id);
                if (debugMode) Debug.Log($"[Analytics] User ID set to: {id}");
            }
        }

        public void SetUserConsent(UserConsent consent)
        {
            userConsent = consent;
            SaveUserConsent(consent);
            TrackEvent("user_consent_updated", new Dictionary<string, object>
            {
                { "analytics", consent.analytics },
                { "crash_reporting", consent.crashReporting },
                { "performance_monitoring", consent.performanceMonitoring },
                { "personalization", consent.personalization }
            });

            if (!isInitialized && (consent.analytics || consent.crashReporting || consent.performanceMonitoring))
            {
                Initialize();
            }
        }

        /// <summary>
        /// Tracks a user action with contextual source and metadata.
        /// </summary>
        /// <param name="eventName">The name of the user action (e.g., "submit_feedback").</param>
        /// <param name="source">The source or origin of the action (e.g., "feedback_form").</param>
        /// <param name="properties">Additional metadata describing the action.</param>
        public void TrackUserAction(string eventName, string source, Dictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(source))
            {
                if (debugMode) Debug.LogWarning("[Analytics] TrackUserAction called with empty event name or source.");
                return;
            }

            if (!CanTrackEvent("user_action"))
            {
                if (debugMode) Debug.Log($"[Analytics] Skipping user action '{eventName}' due to consent settings.");
                return;
            }

            var payload = properties != null ? new Dictionary<string, object>(properties) : new Dictionary<string, object>();
            payload["event"] = eventName;
            payload["source"] = source;

            TrackEvent("user_action", payload);
        }



        public bool HasUserConsent()
        {
            return userConsent.analytics || userConsent.crashReporting || userConsent.performanceMonitoring;
        }

        public void ShowPrivacyPrompt()
        {
            Debug.Log("[Analytics] Simulated privacy prompt shown");
            SetUserConsent(new UserConsent(true));
        }

        private bool CanTrackEvent(string name)
        {
            return name switch
            {
                "error" => userConsent.crashReporting,
                "performance" => userConsent.performanceMonitoring,
                "user_action" or "screen_view" or "user_feedback" => userConsent.analytics || userConsent.personalization,
                _ => userConsent.analytics
            };
        }
    }
}
