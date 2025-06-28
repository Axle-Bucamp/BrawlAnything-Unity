using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace BrawlAnything.Deployment
{
    public class PhaseRolloutManager : MonoBehaviour
    {
        private static PhaseRolloutManager _instance;
        public static PhaseRolloutManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("PhaseRolloutManager");
                    _instance = go.AddComponent<PhaseRolloutManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [SerializeField] private DeploymentPhase _currentPhase = DeploymentPhase.Alpha;
        [SerializeField] private AccessControlMethod[] _activeAccessMethods;
        [SerializeField] private string _configEndpoint = "https://api.brawlanything.com/v1/deployment/config";
        [SerializeField] private float _configRefreshInterval = 300f;

        private Dictionary<DeploymentPhase, PhaseSettings> _phaseSettings = new();
        private bool _userHasAccess = false;
        private string _accessDeniedReason = "";
        private string _userInviteCode = "";
        private int _userWaitlistPosition = -1;

        public event Action<DeploymentPhase> OnPhaseChanged;
        public event Action<bool, string> OnAccessStatusChanged;
        public event Action<int> OnWaitlistPositionChanged;

        public DeploymentPhase CurrentPhase => _currentPhase;
        public bool UserHasAccess => _userHasAccess;
        public string AccessDeniedReason => _accessDeniedReason;
        public int WaitlistPosition => _userWaitlistPosition;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePhaseSettings();
        }

        private void Start()
        {
            StartCoroutine(RefreshConfigPeriodically());
            FetchDeploymentConfig();
        }

        private void InitializePhaseSettings()
        {
            _phaseSettings[DeploymentPhase.Alpha] = new PhaseSettings
            {
                MaxUsers = 1000,
                AccessMethods = new[] { AccessControlMethod.WaitingList, AccessControlMethod.InvitationCode },
                AllowedRegions = new[] { "US-WEST", "US-EAST", "EU-CENTRAL" },
                FeatureFlags = new()
                {
                    { "multiplayer", true },
                    { "model_generation", true },
                    { "animation_generation", true },
                    { "in_app_purchases", false },
                    { "leaderboards", false },
                    { "social_features", false }
                }
            };

            _phaseSettings[DeploymentPhase.Beta] = new PhaseSettings
            {
                MaxUsers = 10000,
                AccessMethods = new[] { AccessControlMethod.WaitingList, AccessControlMethod.InvitationCode, AccessControlMethod.GeographicRestriction },
                AllowedRegions = new[] { "US-WEST", "US-EAST", "EU-CENTRAL", "EU-WEST", "ASIA-EAST" },
                FeatureFlags = new()
                {
                    { "multiplayer", true },
                    { "model_generation", true },
                    { "animation_generation", true },
                    { "in_app_purchases", true },
                    { "leaderboards", true },
                    { "social_features", true }
                }
            };

            _phaseSettings[DeploymentPhase.EarlyAccess] = new PhaseSettings
            {
                MaxUsers = 100000,
                AccessMethods = new[] { AccessControlMethod.WaitingList, AccessControlMethod.GeographicRestriction },
                AllowedRegions = new[] { "US-WEST", "US-EAST", "EU-CENTRAL", "EU-WEST", "ASIA-EAST", "ASIA-SOUTH", "OCEANIA" },
                FeatureFlags = new()
                {
                    { "multiplayer", true },
                    { "model_generation", true },
                    { "animation_generation", true },
                    { "in_app_purchases", true },
                    { "leaderboards", true },
                    { "social_features", true }
                }
            };

            _phaseSettings[DeploymentPhase.PublicLaunch] = new PhaseSettings
            {
                MaxUsers = int.MaxValue,
                AccessMethods = new[] { AccessControlMethod.OpenAccess },
                AllowedRegions = new[] { "GLOBAL" },
                FeatureFlags = new()
                {
                    { "multiplayer", true },
                    { "model_generation", true },
                    { "animation_generation", true },
                    { "in_app_purchases", true },
                    { "leaderboards", true },
                    { "social_features", true }
                }
            };
        }

        public void FetchDeploymentConfig()
        {
            StartCoroutine(FetchConfigCoroutine());
        }

        private IEnumerator FetchConfigCoroutine()
        {
            string userId = PlayerPrefs.GetString("UserId", "");
            string authToken = PlayerPrefs.GetString("AuthToken", "");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(authToken))
            {
                Debug.LogWarning("User not authenticated.");
                yield break;
            }

            UnityWebRequest request = UnityWebRequest.Get(_configEndpoint);
            request.SetRequestHeader("Authorization", "Bearer " + authToken);
            request.SetRequestHeader("User-Id", userId);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Config fetch failed: {request.error}");
                yield break;
            }

            try
            {
                var config = JsonConvert.DeserializeObject<DeploymentConfig>(request.downloadHandler.text);

                var newPhase = (DeploymentPhase)config.CurrentPhase;
                if (newPhase != _currentPhase)
                {
                    _currentPhase = newPhase;
                    OnPhaseChanged?.Invoke(_currentPhase);
                }

                _activeAccessMethods = config.ActiveAccessMethods;
                _userHasAccess = config.UserHasAccess;
                _accessDeniedReason = config.AccessDeniedReason;

                if (config.WaitlistPosition != _userWaitlistPosition && config.WaitlistPosition >= 0)
                {
                    _userWaitlistPosition = config.WaitlistPosition;
                    OnWaitlistPositionChanged?.Invoke(_userWaitlistPosition);
                }

                if (_phaseSettings.ContainsKey(_currentPhase) && config.FeatureFlags != null)
                {
                    _phaseSettings[_currentPhase].FeatureFlags = config.FeatureFlags;
                }

                OnAccessStatusChanged?.Invoke(_userHasAccess, _accessDeniedReason);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse config: {e.Message}");
            }
        }

        private IEnumerator RefreshConfigPeriodically()
        {
            while (true)
            {
                yield return new WaitForSeconds(_configRefreshInterval);
                FetchDeploymentConfig();
            }
        }

        public bool IsFeatureEnabled(string featureKey)
        {
            if (_phaseSettings.TryGetValue(_currentPhase, out var settings) && settings.FeatureFlags.TryGetValue(featureKey, out var enabled))
                return enabled;

            Debug.LogWarning($"Feature '{featureKey}' not found in phase {_currentPhase}");
            return false;
        }
        /// <summary>
        /// Returns the current deployment phase.
        /// </summary>
        public DeploymentPhase GetCurrentPhase()
        {
            return _currentPhase;
        }
    }

    #region Support Types

    public enum DeploymentPhase { Alpha, Beta, EarlyAccess, PublicLaunch }

    public enum AccessControlMethod
    {
        WaitingList,
        InvitationCode,
        GeographicRestriction,
        OpenAccess
    }

    [Serializable]
    public class PhaseSettings
    {
        public int MaxUsers;
        public AccessControlMethod[] AccessMethods;
        public string[] AllowedRegions;
        public Dictionary<string, bool> FeatureFlags;
    }

    [Serializable]
    public class DeploymentConfig
    {
        public int CurrentPhase;
        public AccessControlMethod[] ActiveAccessMethods;
        public bool UserHasAccess;
        public string AccessDeniedReason;
        public int WaitlistPosition;
        public Dictionary<string, bool> FeatureFlags;
    }

    public static class DeploymentManager
    {
        public static PhaseRolloutManager Manager => PhaseRolloutManager.Instance;

        public static bool HasFeature(string key) => Manager.IsFeatureEnabled(key);
        public static bool HasAccess => Manager.UserHasAccess;
        public static DeploymentPhase Phase => Manager.CurrentPhase;
    }


    #endregion
}
