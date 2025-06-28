using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrawlAnything.Core;
using BrawlAnything.Network;

namespace BrawlAnything.Character
{
    /// <summary>
    /// Classe qui représente une carte de personnage dans l'interface utilisateur
    /// </summary>
    public class CharacterCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image characterImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI characterLevelText;
        [SerializeField] private Image rarityIndicator;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button editButton;
        [SerializeField] private Button deleteButton;
        
        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = Color.gray;
        [SerializeField] private Color uncommonColor = Color.green;
        [SerializeField] private Color rareColor = Color.blue;
        [SerializeField] private Color epicColor = Color.purple;
        [SerializeField] private Color legendaryColor = Color.yellow;
        
        // Données du personnage
        private CharacterData characterData;
        private bool isSelected = false;
        
        // Événements
        public event Action<CharacterData> OnCharacterSelected;
        public event Action<CharacterData> OnCharacterEdit;
        public event Action<CharacterData> OnCharacterDelete;
        
        private void Awake()
        {
            // Configurer les boutons
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            }
            
            if (editButton != null)
            {
                editButton.onClick.AddListener(OnEditButtonClicked);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            }
        }
        
        private void OnDestroy()
        {
            // Nettoyer les écouteurs
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectButtonClicked);
            }
            
            if (editButton != null)
            {
                editButton.onClick.RemoveListener(OnEditButtonClicked);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(OnDeleteButtonClicked);
            }
        }
        
        /// <summary>
        /// Configure la carte avec les données du personnage
        /// </summary>
        /// <param name="data">Données du personnage</param>
        public void SetCharacterData(CharacterData data)
        {
            characterData = data;
            
            // Mettre à jour l'UI
            if (characterNameText != null)
            {
                characterNameText.text = data.name;
            }
            
            if (characterLevelText != null)
            {
                characterLevelText.text = $"Niv. {data.level}";
            }
            
            if (rarityIndicator != null)
            {
                rarityIndicator.color = GetRarityColor(data.rarity);
            }
            
            // Charger l'image du personnage
            if (characterImage != null && !string.IsNullOrEmpty(data.imageUrl))
            {
                StartCoroutine(LoadCharacterImage(data.imageUrl));
            }
            
            // Mettre à jour l'état de sélection
            UpdateSelectionState(false);
        }
        
        /// <summary>
        /// Charge l'image du personnage depuis l'URL
        /// </summary>
        private IEnumerator LoadCharacterImage(string imageUrl)
        {
            // Vérifier si l'URL est valide
            if (string.IsNullOrEmpty(imageUrl))
                yield break;
                
            // Utiliser le client API pour télécharger l'image
            APIClient apiClient = APIClient.Instance;
            if (apiClient == null)
            {
                Debug.LogError("APIClient not found. Cannot load character image.");
                yield break;
            }
            
            yield return apiClient.DownloadAsset(imageUrl, (imageData, error) => {
                if (imageData != null && string.IsNullOrEmpty(error))
                {
                    // Créer une texture à partir des données
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(imageData);
                    
                    // Créer un sprite à partir de la texture
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    
                    // Assigner le sprite à l'image
                    if (characterImage != null)
                    {
                        characterImage.sprite = sprite;
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load character image: {error}");
                }
            });
        }
        
        /// <summary>
        /// Obtient la couleur correspondant à la rareté
        /// </summary>
        private Color GetRarityColor(string rarity)
        {
            switch (rarity.ToLower())
            {
                case "common":
                    return commonColor;
                case "uncommon":
                    return uncommonColor;
                case "rare":
                    return rareColor;
                case "epic":
                    return epicColor;
                case "legendary":
                    return legendaryColor;
                default:
                    return commonColor;
            }
        }
        
        /// <summary>
        /// Met à jour l'état de sélection de la carte
        /// </summary>
        public void UpdateSelectionState(bool selected)
        {
            isSelected = selected;
            
            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(selected);
            }
        }
        
        /// <summary>
        /// Appelé lorsque le bouton de sélection est cliqué
        /// </summary>
        private void OnSelectButtonClicked()
        {
            // Notifier les écouteurs
            OnCharacterSelected?.Invoke(characterData);
            
            // Notifier le système d'événements
            EventSystem.Instance.TriggerEvent("character_selected", characterData);
        }
        
        /// <summary>
        /// Appelé lorsque le bouton d'édition est cliqué
        /// </summary>
        private void OnEditButtonClicked()
        {
            // Notifier les écouteurs
            OnCharacterEdit?.Invoke(characterData);
            
            // Notifier le système d'événements
            EventSystem.Instance.TriggerEvent("character_edit", characterData);
        }
        
        /// <summary>
        /// Appelé lorsque le bouton de suppression est cliqué
        /// </summary>
        private void OnDeleteButtonClicked()
        {
            // Notifier les écouteurs
            OnCharacterDelete?.Invoke(characterData);
            
            // Notifier le système d'événements
            EventSystem.Instance.TriggerEvent("character_delete", characterData);
        }
        
        /// <summary>
        /// Obtient les données du personnage
        /// </summary>
        public CharacterData GetCharacterData()
        {
            return characterData;
        }
        
        /// <summary>
        /// Vérifie si la carte est sélectionnée
        /// </summary>
        public bool IsSelected()
        {
            return isSelected;
        }
    }
}
