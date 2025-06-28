using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Globalization;
using System.Collections.Generic;
using TMPro;
using BrawlAnything.Network;

namespace BrawlAnything.UI
{
    /// <summary>
    /// Gère l'envoi de feedback utilisateur avec confirmation, erreur et réinitialisation du formulaire.
    /// </summary>
    public class FeedbackManagerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TextMeshProUGUI confirmationText;

        [SerializeField] private TMP_InputField subjectInputField;
        [SerializeField] private TMP_InputField descriptionInputField;
        [SerializeField] private TMP_Dropdown feedbackTypeDropdown;
        [SerializeField] private TMP_Dropdown severityDropdown;
        [SerializeField] private Toggle includeScreenshotToggle;
        [SerializeField] public Toggle includeSystemInfoToggle;

        [Header("Settings")]
        [SerializeField] private float autoCloseConfirmationDelay = 3f;
        [SerializeField] private bool clearFormAfterSubmission = true;
        [Header("Feedback Settings")]
        [SerializeField]
        private List<string> enabledFeedbackTypes = new List<string>
        {
            "bug",
            "performance",
            "general",
            "feature",
            "other"
        };
        /// <summary>
        /// Appelé lors de la soumission du formulaire.
        /// </summary>
        public void OnSubmitButtonClicked()
        {
            bool panelWasActive = feedbackPanel != null && feedbackPanel.activeSelf;

            // Valider les champs requis
            if (string.IsNullOrEmpty(subjectInputField.text) || string.IsNullOrEmpty(descriptionInputField.text))
            {
                ShowError("Veuillez remplir tous les champs requis.");
                return;
            }

            // Cacher le panneau de feedback
            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(false);
            }

            // Afficher confirmation
            ShowConfirmation("Merci pour votre retour !");

            // Déclencher l'auto-fermeture de la confirmation après un délai
            StartCoroutine(AutoCloseConfirmationCoroutine());

            // Restaurer le panneau de feedback si nécessaire
            if (feedbackPanel != null && panelWasActive)
            {
                feedbackPanel.SetActive(true);
            }

            // Préparer les données de feedback
            string feedbackType = "general"; // Exemple, vous pouvez changer en fonction du type choisi
            string subject = subjectInputField.text;
            string description = descriptionInputField.text;
            string severity = severityDropdown.options[severityDropdown.value].text; // Exemple de gravité via dropdown
            string screenshotPath = GetScreenshotPath(); // Supposez que vous ayez une méthode pour obtenir le chemin de la capture d'écran

            // Soumettre le feedback via la fonction dédiée
            HandleFeedbackSubmission(feedbackType, subject, description, severity, screenshotPath);
        }

        /// <summary>
        /// Prend une capture d'écran et retourne le chemin où elle est stockée.
        /// </summary>
        /// <returns>Le chemin de la capture d'écran sauvegardée.</returns>
        public string GetScreenshotPath()
        {
            // Définir le chemin où la capture d'écran sera sauvegardée
            string screenshotDirectory = Application.persistentDataPath + "/Screenshots";

            // Créer le répertoire si nécessaire
            if (!System.IO.Directory.Exists(screenshotDirectory))
            {
                System.IO.Directory.CreateDirectory(screenshotDirectory);
            }

            // Générer un nom de fichier unique pour éviter les conflits
            string screenshotFileName = "screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

            // Combiner le répertoire et le nom du fichier pour obtenir le chemin complet
            string screenshotPath = System.IO.Path.Combine(screenshotDirectory, screenshotFileName);

            // Prendre la capture d'écran
            ScreenCapture.CaptureScreenshot(screenshotPath);

            // Retourner le chemin de la capture d'écran
            return screenshotPath;
        }

        

        /// <summary>
        /// Gère la soumission d'un feedback.
        /// </summary>
        public void HandleFeedbackSubmission(string feedbackType, string subject, string description,
                                              string severity, string screenshotPath)
        {
            // ensure feedback is correct on UI side
        }

        /// <summary>
        /// Affiche un message de confirmation.
        /// </summary>
        public void ShowConfirmation(string message)
        {
            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(true);

                if (confirmationText != null)
                {
                    confirmationText.text = message;
                }
            }

            // Cacher le panneau de feedback
            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(false);
            }
        }



        /// <summary>
        /// Affiche un message d'erreur.
        /// </summary>
        public void ShowError(string message)
        {
            // Dans une implémentation réelle, cela afficherait un message d'erreur dans l'UI
            Debug.LogWarning($"Feedback Error: {message}");
        }

        /// <summary>
        /// Ferme automatiquement le panneau de confirmation après un délai.
        /// </summary>
        private IEnumerator AutoCloseConfirmationCoroutine()
        {
            yield return new WaitForSeconds(autoCloseConfirmationDelay);

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            // Réinitialiser le formulaire si nécessaire
            if (clearFormAfterSubmission)
            {
                ClearForm();
            }
        }

        /// <summary>
        /// Réinitialise le formulaire.
        /// </summary>
        private void ClearForm()
        {
            if (subjectInputField != null)
                subjectInputField.text = "";

            if (descriptionInputField != null)
                descriptionInputField.text = "";

            if (feedbackTypeDropdown != null)
                feedbackTypeDropdown.value = 0;

            if (severityDropdown != null)
                severityDropdown.value = 0;

            if (includeScreenshotToggle != null)
                includeScreenshotToggle.isOn = false;

            if (includeSystemInfoToggle != null)
                includeSystemInfoToggle.isOn = true;
        }

        /// <summary>
        /// Obtient le type de feedback sélectionné.
        /// </summary>
        private string GetFeedbackType()
        {
            if (feedbackTypeDropdown == null)
                return "general";

            switch (feedbackTypeDropdown.value)
            {
                case 0: return "bug";
                case 1: return "feature";
                case 2: return "general";
                case 3: return "performance";
                case 4: return "other";
                default: return "general";
            }
        }

        /// <summary>
        /// Obtient la sévérité sélectionnée.
        /// </summary>
        private string GetSeverity()
        {
            if (severityDropdown == null)
                return "medium";

            switch (severityDropdown.value)
            {
                case 0: return "low";
                case 1: return "medium";
                case 2: return "high";
                case 3: return "critical";
                default: return "medium";
            }
        }

        /// <summary>
        /// Définit la visibilité du bouton de feedback.
        /// </summary>
        /// <param name="visible">True pour l'afficher, false pour le masquer.</param>
        public void SetFeedbackButtonVisible(bool visible)
        {
            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(visible);
            }
            else
            {
                Debug.LogWarning("Feedback panel reference is not assigned.");
            }
        }

        /// <summary>
        /// Simulation de la soumission du feedback.
        /// </summary>
        private void SubmitFeedback()
        {
            string subject = subjectInputField?.text ?? "";
            string description = descriptionInputField?.text ?? "";
            string type = GetFeedbackType();
            string severity = GetSeverity();
            bool includeScreenshot = includeScreenshotToggle != null && includeScreenshotToggle.isOn;
            bool includeSystemInfo = includeSystemInfoToggle != null && includeSystemInfoToggle.isOn;

            Debug.Log($"[Feedback Submitted]\nSubject: {subject}\nType: {type}\nSeverity: {severity}\nInclude Screenshot: {includeScreenshot}\nInclude System Info: {includeSystemInfo}\nDescription: {description}");

            // TODO: Send feedback to server or save it locally
        }


    }
}
