using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using BrawlAnything.Analytics;
using BrawlAnything.Network;
using BrawlAnything.Deployment;
using BrawlAnything.UI;

namespace BrawlAnything.UI
{
    /// <summary>
    /// Gestionnaire de feedback intégré avec le système de déploiement progressif.
    /// Étend la classe FeedbackUI existante pour ajouter l'intégration avec le backend.
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string feedbackApiEndpoint = "/api/v1/feedback";

        [Header("Feedback Settings")]
        [SerializeField] private bool enableFeedbackInAllPhases = true;
        [SerializeField] private List<string> enabledFeedbackTypes = new List<string>() { "bug", "performance", "general", "feature", "other" };

        // Référence au gestionnaire de déploiement par phase
        private PhaseRolloutManager phaseRolloutManager;

        // Référence au client API
        private APIClient apiClient;

        // Référence au gestionnaire d'analyse
        private AnalyticsManager analyticsManager;

        // Référence à l'UI de feedback
        private FeedbackManagerUI feedbackUI;

        private void Awake()
        {
            // Obtenir les références
            phaseRolloutManager = FindObjectOfType<PhaseRolloutManager>();
            apiClient = FindObjectOfType<APIClient>();
            analyticsManager = AnalyticsManager.Instance;
            feedbackUI = FindObjectOfType<FeedbackManagerUI>();

            if (phaseRolloutManager == null)
            {
                Debug.LogWarning("PhaseRolloutManager not found. Feedback phase restrictions will not be applied.");
            }

            if (apiClient == null)
            {
                Debug.LogError("APIClient not found. Feedback submission to backend will not work.");
            }

            if (feedbackUI == null)
            {
                Debug.LogError("FeedbackUI not found. Feedback UI will not be available.");
            }
        }

        private void Start()
        {
            // S'abonner à l'événement de soumission de feedback
            if (feedbackUI != null)
            {
                // later call ui side
                //feedbackUI.HandleFeedbackSubmission += HandleFeedbackSubmission;
            }

            // Vérifier si le feedback est activé dans la phase actuelle
            CheckFeedbackAvailability();
        }

        private void OnDestroy()
        {
            // Se désabonner de l'événement
            if (feedbackUI != null)
            {
                //feedbackUI.HandleFeedbackSubmission -= HandleFeedbackSubmission;
            }
        }

        /// <summary>
        /// Vérifie si le feedback est disponible dans la phase actuelle.
        /// </summary>
        public void CheckFeedbackAvailability()
        {
            if (enableFeedbackInAllPhases || phaseRolloutManager == null)
            {
                // Le feedback est toujours activé
                return;
            }

            // Vérifier la phase actuelle
            DeploymentPhase currentPhase = phaseRolloutManager.GetCurrentPhase();
            bool feedbackEnabled = true;

            // Logique pour activer/désactiver le feedback selon la phase
            // Par défaut, le feedback est activé dans toutes les phases

            // Si le feedback est désactivé, masquer le bouton/panneau
            if (!feedbackEnabled && feedbackUI != null)
            {
                feedbackUI.SetFeedbackButtonVisible(false);
            }
        }

        /// <summary>
        /// Gère la soumission d'un feedback.
        /// </summary>
        public void HandleFeedbackSubmission(string feedbackType, string subject, string description,
                                            string severity, string screenshotPath)
        {
            // Vérifier si le type de feedback est activé
            if (!enabledFeedbackTypes.Contains(feedbackType))
            {
                Debug.LogWarning($"Feedback type '{feedbackType}' is not enabled.");
                return;
            }

            // Créer l'objet de feedback
            Dictionary<string, object> feedbackData = new Dictionary<string, object>
            {
                { "user_id", GetUserID() },
                { "type", feedbackType },
                { "subject", subject },
                { "description", description },
                { "severity", severity },
                { "app_version", Application.version },
                { "platform", Application.platform.ToString() }
            };

            // Ajouter les informations système
            if (feedbackUI != null && feedbackUI.includeSystemInfoToggle.isOn)
            {
                feedbackData["system_info"] = GetSystemInfo();
            }

            // Ajouter la capture d'écran si disponible
            if (!string.IsNullOrEmpty(screenshotPath))
            {
                StartCoroutine(UploadScreenshotAndSubmitFeedback(screenshotPath, feedbackData));
            }
            else
            {
                // Soumettre le feedback sans capture d'écran
                SubmitFeedbackToBackend(feedbackData);
            }

            // Tracker l'événement de soumission
            if (analyticsManager != null)
            {
                analyticsManager.TrackUserAction("submit_feedback", "feedback_form", new Dictionary<string, object>
                {
                    { "feedback_type", feedbackType },
                    { "severity", severity },
                    { "has_screenshot", !string.IsNullOrEmpty(screenshotPath) },
                    { "deployment_phase", phaseRolloutManager != null ? phaseRolloutManager.GetCurrentPhase() : 0 }
                });
            }
        }

        /// <summary>
        /// Télécharge la capture d'écran et soumet le feedback.
        /// </summary>
        public IEnumerator UploadScreenshotAndSubmitFeedback(string screenshotPath, Dictionary<string, object> feedbackData)
        {
            if (apiClient == null)
            {
                Debug.LogError("APIClient not found. Cannot upload screenshot.");
                SubmitFeedbackToBackend(feedbackData);
                yield break;
            }

            // Télécharger la capture d'écran
            string uploadEndpoint = "/storage/upload";
            byte[] screenshotBytes = System.IO.File.ReadAllBytes(screenshotPath);

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", screenshotBytes, "feedback_screenshot.png", "image/png");

            using (UnityWebRequest www = UnityWebRequest.Post(apiClient.GetFullURL(uploadEndpoint), form))
            {
                apiClient.AddAuthHeaders(www);

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // Analyser la réponse pour obtenir l'URL
                    string response = www.downloadHandler.text;
                    Dictionary<string, object> responseData = JsonUtility.FromJson<Dictionary<string, object>>(response);

                    if (responseData != null && responseData.ContainsKey("file_url"))
                    {
                        string fileUrl = responseData["file_url"].ToString();
                        feedbackData["screenshot_url"] = fileUrl;
                    }
                    else
                    {
                        Debug.LogWarning("Screenshot upload succeeded but URL not found in response.");
                    }
                }
                else
                {
                    Debug.LogError($"Screenshot upload failed: {www.error}");
                }
            }

            // Soumettre le feedback avec ou sans l'URL de la capture d'écran
            SubmitFeedbackToBackend(feedbackData);
        }

        /// <summary>
        /// Soumet le feedback au backend.
        /// </summary>
        public void SubmitFeedbackToBackend(Dictionary<string, object> feedbackData)
        {
            if (apiClient == null)
            {
                Debug.LogError("APIClient not found. Cannot submit feedback to backend.");
                return;
            }

            // Ajouter la phase de déploiement actuelle
            if (phaseRolloutManager != null)
            {
                feedbackData["deployment_phase"] = phaseRolloutManager.GetCurrentPhase();
            }

            // Convertir en JSON
            string jsonData = JsonUtility.ToJson(feedbackData);

            // Envoyer au backend
            StartCoroutine(apiClient.PostJsonData(feedbackApiEndpoint, jsonData, OnFeedbackSubmitted));
        }

        /// <summary>
        /// Callback après la soumission du feedback.
        /// </summary>
        private void OnFeedbackSubmitted(bool success, string response)
        {
            if (success)
            {
                Debug.Log("Feedback submitted successfully to backend.");

                // Afficher un message de confirmation à l'utilisateur
                if (feedbackUI != null)
                {
                    feedbackUI.ShowConfirmation("Thank you for your feedback! Your submission has been received and will help us improve the game.");
                }
            }
            else
            {
                Debug.LogError($"Failed to submit feedback to backend: {response}");

                // Afficher un message d'erreur à l'utilisateur
                if (feedbackUI != null)
                {
                    feedbackUI.ShowError("Failed to submit feedback. Please try again later.");
                }

                // Stocker localement pour réessayer plus tard
                StoreFeedbackLocally(response);
            }
        }

        /// <summary>
        /// Stocke le feedback localement en cas d'échec de soumission.
        /// </summary>
        private void StoreFeedbackLocally(string feedbackJson)
        {
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "pending_feedback");
                System.IO.Directory.CreateDirectory(path);

                string filename = $"feedback_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.json";
                string fullPath = System.IO.Path.Combine(path, filename);

                System.IO.File.WriteAllText(fullPath, feedbackJson);
                Debug.Log($"Feedback stored locally at: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to store feedback locally: {e.Message}");
            }
        }

        /// <summary>
        /// Obtient l'ID utilisateur.
        /// </summary>
        private string GetUserID()
        {
            // Utiliser l'ID utilisateur du gestionnaire d'authentification si disponible
            // Sinon, utiliser un ID anonyme
            string userId = "anonymous";

            // Exemple: si un AuthManager existe
            // AuthManager authManager = FindObjectOfType<AuthManager>();
            // if (authManager != null && authManager.IsLoggedIn)
            // {
            //     userId = authManager.GetUserID();
            // }

            return userId;
        }

        /// <summary>
        /// Obtient les informations système.
        /// </summary>
        private Dictionary<string, string> GetSystemInfo()
        {
            return new Dictionary<string, string>
            {
                { "device_model", SystemInfo.deviceModel },
                { "device_type", SystemInfo.deviceType.ToString() },
                { "graphics_device", SystemInfo.graphicsDeviceName },
                { "graphics_memory_size", SystemInfo.graphicsMemorySize.ToString() },
                { "os", SystemInfo.operatingSystem }
            };
        }
    }
}
