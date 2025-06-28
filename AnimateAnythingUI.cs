using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrawlAnything.Core;
using BrawlAnything.Character;

namespace BrawlAnything.UI
{
    /// <summary>
    /// Interface utilisateur pour la recherche et la génération de modèles avec Animate Anything World
    /// </summary>
    public class AnimateAnythingUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private GameObject searchPanel;
        [SerializeField] private GameObject generationPanel;
        [SerializeField] private GameObject voicePanel;
        
        [Header("Champ de recherche")]
        [SerializeField] private TMPro.TMP_InputField searchInputField;
        [SerializeField] private UnityEngine.UI.Button searchButton;
        [SerializeField] private Transform searchResultsContainer;
        [SerializeField] private GameObject searchResultPrefab;
        
        [Header("Génération de modèle")]
        [SerializeField] private TMPro.TMP_InputField textPromptInputField;
        [SerializeField] private UnityEngine.UI.Button generateFromTextButton;
        [SerializeField] private UnityEngine.UI.Button generateFromVoiceButton;
        [SerializeField] private UnityEngine.UI.Button uploadImageButton;
        [SerializeField] private UnityEngine.UI.RawImage previewImage;
        
        [Header("Génération vocale")]
        [SerializeField] private TMPro.TMP_InputField soundEffectInputField;
        [SerializeField] private UnityEngine.UI.Button generateSoundButton;
        [SerializeField] private UnityEngine.UI.Button playSoundButton;
        [SerializeField] private TMPro.TMP_Dropdown soundPresetDropdown;
        
        [Header("Statut")]
        [SerializeField] private TMPro.TextMeshProUGUI statusText;
        [SerializeField] private UnityEngine.UI.Image loadingSpinner;
        
        // Références aux services
        private AnimateAnythingService animateAnythingService;
        private VoiceGenerationManager voiceGenerationManager;
        
        // État
        private List<ModelSearchResult> currentSearchResults = new List<ModelSearchResult>();
        private string lastGeneratedSoundEffect = "";
        private AudioClip lastGeneratedClip;
        private Texture2D uploadedImage;
        
        private void Awake()
        {
            // Obtenir les références
            animateAnythingService = AnimateAnythingService.Instance;
            voiceGenerationManager = VoiceGenerationManager.Instance;
            
            // Configurer les boutons
            if (searchButton != null)
                searchButton.onClick.AddListener(OnSearchButtonClicked);
                
            if (generateFromTextButton != null)
                generateFromTextButton.onClick.AddListener(OnGenerateFromTextButtonClicked);
                
            if (generateFromVoiceButton != null)
                generateFromVoiceButton.onClick.AddListener(OnGenerateFromVoiceButtonClicked);
                
            if (uploadImageButton != null)
                uploadImageButton.onClick.AddListener(OnUploadImageButtonClicked);
                
            if (generateSoundButton != null)
                generateSoundButton.onClick.AddListener(OnGenerateSoundButtonClicked);
                
            if (playSoundButton != null)
                playSoundButton.onClick.AddListener(OnPlaySoundButtonClicked);
                
            // Désactiver le bouton de lecture du son au démarrage
            if (playSoundButton != null)
                playSoundButton.interactable = false;
        }
        
        private void Start()
        {
            // S'abonner aux événements
            if (animateAnythingService != null)
            {
                animateAnythingService.OnSearchCompleted += OnSearchCompleted;
                animateAnythingService.OnModelGenerated += OnModelGenerated;
                animateAnythingService.OnError += OnAnimateAnythingError;
            }
            
            if (voiceGenerationManager != null)
            {
                voiceGenerationManager.OnSoundEffectGenerated += OnSoundEffectGenerated;
                voiceGenerationManager.OnError += OnVoiceGenerationError;
            }
            
            // Initialiser l'interface
            UpdateSoundPresetDropdown();
            SetStatus("");
            ShowSearchPanel();
        }
        
        private void OnDestroy()
        {
            // Se désabonner des événements
            if (animateAnythingService != null)
            {
                animateAnythingService.OnSearchCompleted -= OnSearchCompleted;
                animateAnythingService.OnModelGenerated -= OnModelGenerated;
                animateAnythingService.OnError -= OnAnimateAnythingError;
            }
            
            if (voiceGenerationManager != null)
            {
                voiceGenerationManager.OnSoundEffectGenerated -= OnSoundEffectGenerated;
                voiceGenerationManager.OnError -= OnVoiceGenerationError;
            }
        }
        
        #region UI Navigation
        
        public void ShowSearchPanel()
        {
            searchPanel.SetActive(true);
            generationPanel.SetActive(false);
            voicePanel.SetActive(false);
        }
        
        public void ShowGenerationPanel()
        {
            searchPanel.SetActive(false);
            generationPanel.SetActive(true);
            voicePanel.SetActive(false);
        }
        
        public void ShowVoicePanel()
        {
            searchPanel.SetActive(false);
            generationPanel.SetActive(false);
            voicePanel.SetActive(true);
        }
        
        #endregion
        
        #region Search Functionality
        
        private void OnSearchButtonClicked()
        {
            if (string.IsNullOrEmpty(searchInputField.text))
                return;
                
            string searchTerm = searchInputField.text.Trim();
            
            // Effacer les résultats précédents
            ClearSearchResults();
            
            // Afficher l'état de chargement
            SetStatus($"Recherche de modèles pour '{searchTerm}'...");
            SetLoading(true);
            
            // Lancer la recherche
            animateAnythingService.SearchModels(searchTerm);
        }
        
        private void OnSearchCompleted(ModelSearchResult[] results)
        {
            // Mettre à jour l'état
            SetLoading(false);
            
            if (results == null || results.Length == 0)
            {
                SetStatus("Aucun résultat trouvé");
                return;
            }
            
            SetStatus($"{results.Length} modèles trouvés");
            
            // Stocker les résultats
            currentSearchResults.Clear();
            currentSearchResults.AddRange(results);
            
            // Afficher les résultats
            DisplaySearchResults(results);
        }
        
        private void DisplaySearchResults(ModelSearchResult[] results)
        {
            // Effacer les résultats précédents
            ClearSearchResults();
            
            // Créer un élément d'interface pour chaque résultat
            foreach (var result in results)
            {
                GameObject resultObject = Instantiate(searchResultPrefab, searchResultsContainer);
                SearchResultUI resultUI = resultObject.GetComponent<SearchResultUI>();
                
                if (resultUI != null)
                {
                    resultUI.Initialize(result);
                    resultUI.OnSelected += OnSearchResultSelected;
                }
            }
        }
        
        private void ClearSearchResults()
        {
            // Détruire tous les enfants du conteneur de résultats
            foreach (Transform child in searchResultsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        private void OnSearchResultSelected(ModelSearchResult result)
        {
            if (result == null)
                return;
                
            // Afficher l'état de chargement
            SetStatus($"Génération du modèle '{result.name}'...");
            SetLoading(true);
            
            // Générer le modèle
            animateAnythingService.GenerateModel(result.name, (model, error) => {
                SetLoading(false);
                
                if (model != null)
                {
                    SetStatus($"Modèle '{result.name}' généré avec succès");
                }
                else
                {
                    SetStatus($"Erreur: {error}");
                }
            });
        }
        
        #endregion
        
        #region Model Generation
        
        private void OnGenerateFromTextButtonClicked()
        {
            if (string.IsNullOrEmpty(textPromptInputField.text))
                return;
                
            string textPrompt = textPromptInputField.text.Trim();
            
            // Afficher l'état de chargement
            SetStatus($"Génération du modèle à partir de la description...");
            SetLoading(true);
            
            // Générer le modèle
            animateAnythingService.GenerateModelFromText(textPrompt, "TextGeneratedModel", (model, error) => {
                SetLoading(false);
                
                if (model != null)
                {
                    SetStatus($"Modèle généré avec succès");
                }
                else
                {
                    SetStatus($"Erreur: {error}");
                }
            });
        }
        
        private void OnGenerateFromVoiceButtonClicked()
        {
            // Afficher l'état
            SetStatus("Activation de la reconnaissance vocale...");
            
            // Ouvrir l'interface de création vocale
            animateAnythingService.GenerateModelFromVoice();
        }
        
        private void OnUploadImageButtonClicked()
        {
            // Ouvrir une boîte de dialogue pour sélectionner une image
            // Note: Dans une application réelle, cela utiliserait NativeGallery ou un plugin similaire
            // Pour cette démonstration, nous simulons le téléchargement d'une image
            
            SetStatus("Sélection d'une image...");
            
            // Simuler le téléchargement d'une image
            StartCoroutine(SimulateImageUpload());
        }
        
        private IEnumerator SimulateImageUpload()
        {
            yield return new WaitForSeconds(1f);
            
            // Créer une texture de test
            uploadedImage = new Texture2D(256, 256);
            for (int y = 0; y < uploadedImage.height; y++)
            {
                for (int x = 0; x < uploadedImage.width; x++)
                {
                    Color color = new Color(
                        UnityEngine.Random.value,
                        UnityEngine.Random.value,
                        UnityEngine.Random.value
                    );
                    uploadedImage.SetPixel(x, y, color);
                }
            }
            uploadedImage.Apply();
            
            // Afficher l'image dans l'aperçu
            if (previewImage != null)
            {
                previewImage.texture = uploadedImage;
                previewImage.gameObject.SetActive(true);
            }
            
            SetStatus("Image téléchargée. Cliquez sur Générer pour créer un modèle.");
        }
        
        private void OnModelGenerated(GameObject model)
        {
            SetLoading(false);
            SetStatus("Modèle généré avec succès");
        }
        
        #endregion
        
        #region Voice Generation
        
        private void UpdateSoundPresetDropdown()
        {
            if (soundPresetDropdown == null || voiceGenerationManager == null)
                return;
                
            // Effacer les options existantes
            soundPresetDropdown.ClearOptions();
            
            // Ajouter une option par défaut
            List<string> options = new List<string>
            {
                "-- Sélectionner un effet --"
            };
            
            // Ajouter les presets prédéfinis
            options.Add("explosion");
            options.Add("laser_blast");
            options.Add("victory_fanfare");
            options.Add("punch_impact");
            options.Add("magical_sparkle");
            
            // Mettre à jour le dropdown
            soundPresetDropdown.AddOptions(options);
            
            // Ajouter un écouteur d'événements
            soundPresetDropdown.onValueChanged.AddListener(OnSoundPresetSelected);
        }
        
        private void OnSoundPresetSelected(int index)
        {
            if (index <= 0 || voiceGenerationManager == null)
                return;
                
            // Obtenir le preset sélectionné
            string presetId = soundPresetDropdown.options[index].text;
            
            // Jouer l'effet sonore
            voiceGenerationManager.PlaySoundEffect(presetId);
        }
        
        private void OnGenerateSoundButtonClicked()
        {
            if (string.IsNullOrEmpty(soundEffectInputField.text))
                return;
                
            string description = soundEffectInputField.text.Trim();
            lastGeneratedSoundEffect = description;
            
            // Afficher l'état de chargement
            SetStatus($"Génération de l'effet sonore...");
            SetLoading(true);
            
            // Générer l'effet sonore
            voiceGenerationManager.GenerateSoundEffect(description, (clip, error) => {
                SetLoading(false);
                
                if (clip != null)
                {
                    lastGeneratedClip = clip;
                    playSoundButton.interactable = true;
                    SetStatus($"Effet sonore généré avec succès");
                }
                else
                {
                    playSoundButton.interactable = false;
                    SetStatus($"Erreur: {error}");
                }
            });
        }
        
        private void OnPlaySoundButtonClicked()
        {
            if (lastGeneratedClip == null)
                return;
                
            // Jouer le dernier effet sonore généré
            AudioSource.PlayClipAtPoint(lastGeneratedClip, Camera.main.transform.position, 0.7f);
            SetStatus($"Lecture de l'effet sonore: {lastGeneratedSoundEffect}");
        }
        
        private void OnSoundEffectGenerated(AudioClip clip)
        {
            // Cet événement est déclenché lorsqu'un effet sonore est généré
            // Nous pouvons l'utiliser pour mettre à jour l'interface utilisateur
        }
        
        #endregion
        
        #region Error Handling
        
        private void OnAnimateAnythingError(string errorMessage)
        {
            SetLoading(false);
            SetStatus($"Erreur: {errorMessage}");
            Debug.LogError($"[AnimateAnythingUI] {errorMessage}");
        }
        
        private void OnVoiceGenerationError(string errorMessage)
        {
            SetLoading(false);
            SetStatus($"Erreur: {errorMessage}");
            Debug.LogError($"[AnimateAnythingUI] {errorMessage}");
        }
        
        #endregion
        
        #region Helper Methods
        
        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
        
        private void SetLoading(bool isLoading)
        {
            if (loadingSpinner != null)
            {
                loadingSpinner.gameObject.SetActive(isLoading);
                
                if (isLoading)
                {
                    // Animer le spinner
                    loadingSpinner.transform.rotation = Quaternion.identity;
                    loadingSpinner.transform.DORotate(new Vector3(0, 0, 360), 1f, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1);
                }
                else
                {
                    // Arrêter l'animation
                    DOTween.Kill(loadingSpinner.transform);
                }
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Classe pour l'interface utilisateur d'un résultat de recherche
    /// </summary>
    public class SearchResultUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI nameText;
        [SerializeField] private UnityEngine.UI.RawImage thumbnailImage;
        [SerializeField] private UnityEngine.UI.Button selectButton;
        
        private ModelSearchResult result;
        
        public event Action<ModelSearchResult> OnSelected;
        
        public void Initialize(ModelSearchResult result)
        {
            this.result = result;
            
            if (nameText != null)
                nameText.text = result.name;
                
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectButtonClicked);
                
            // Charger la miniature si disponible
            if (thumbnailImage != null && !string.IsNullOrEmpty(result.thumbnailUrl))
            {
                StartCoroutine(LoadThumbnail(result.thumbnailUrl));
            }
        }
        
        private IEnumerator LoadThumbnail(string url)
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
                    thumbnailImage.texture = texture;
                }
            }
        }
        
        private void OnSelectButtonClicked()
        {
            OnSelected?.Invoke(result);
        }
    }
}
