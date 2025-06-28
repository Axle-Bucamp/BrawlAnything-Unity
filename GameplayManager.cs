using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrawlAnything.Network;
using BrawlAnything.Models;
using BrawlAnything.AR;
using TMPro;

namespace BrawlAnything.Managers
{
    /// <summary>
    /// Manages the core gameplay loop, coordinating between UI, AR, and network components.
    /// </summary>
    public class GameplayManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject characterSelectionPanel;
        [SerializeField] private GameObject battleSetupPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject resultPanel;
        
        [Header("Character Selection")]
        [SerializeField] private Transform characterContainer;
        [SerializeField] private GameObject characterCardPrefab;
        [SerializeField] private Button createCharacterButton;
        [SerializeField] private Button refreshCharactersButton;
        
        [Header("Battle Setup")]
        [SerializeField] private TMP_Dropdown battleTypeDropdown;
        [SerializeField] private TMP_Dropdown opponentDropdown;
        [SerializeField] private Button startBattleButton;
        [SerializeField] private TextMeshProUGUI arStatusText;
        
        [Header("Battle UI")]
        [SerializeField] private Slider playerHealthBar;
        [SerializeField] private Slider opponentHealthBar;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button specialButton;
        [SerializeField] private TextMeshProUGUI battleTimerText;
        [SerializeField] private TextMeshProUGUI battleStatusText;
        
        [Header("Result UI")]
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI experienceGainedText;
        [SerializeField] private Button returnToMenuButton;
        
        [Header("Components")]
        [SerializeField] private ARBattleEnvironment arBattleEnvironment;
        
        // Private variables
        private BackendCommunicator backendCommunicator;
        private List<CharacterData> userCharacters = new List<CharacterData>();
        private CharacterData selectedCharacter;
        private BattleData currentBattle;
        private float battleTimer;
        private bool battleInProgress;
        private int playerHealth = 100;
        private int opponentHealth = 100;
        private int playerScore = 0;
        private int opponentScore = 0;
        private List<int> usedAbilities = new List<int>();
        private int damageDealt = 0;
        private int damageTaken = 0;
        private int healingDone = 0;
        
        private void Awake()
        {
            backendCommunicator = BackendCommunicator.Instance;
            
            // Register event handlers
            backendCommunicator.OnLoginComplete += HandleLoginComplete;
            backendCommunicator.OnCharactersLoaded += HandleCharactersLoaded;
            backendCommunicator.OnCharacterCreated += HandleCharacterCreated;
            backendCommunicator.OnBattleCreated += HandleBattleCreated;
            backendCommunicator.OnBattleCompleted += HandleBattleCompleted;
            
            if (arBattleEnvironment != null)
            {
                arBattleEnvironment.OnSuitablePlaneFound += HandleSuitablePlaneFound;
                arBattleEnvironment.OnArenaPlaced += HandleArenaPlaced;
            }
            
            // Set up UI button listeners
            if (createCharacterButton != null)
                createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
            
            if (refreshCharactersButton != null)
                refreshCharactersButton.onClick.AddListener(OnRefreshCharactersClicked);
            
            if (startBattleButton != null)
                startBattleButton.onClick.AddListener(OnStartBattleClicked);
            
            if (attackButton != null)
                attackButton.onClick.AddListener(OnAttackClicked);
            
            if (defendButton != null)
                defendButton.onClick.AddListener(OnDefendClicked);
            
            if (specialButton != null)
                specialButton.onClick.AddListener(OnSpecialClicked);
            
            if (returnToMenuButton != null)
                returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            
            // Initialize UI
            ShowPanel(loginPanel);
        }
        
        private void OnDestroy()
        {
            // Unregister event handlers
            if (backendCommunicator != null)
            {
                backendCommunicator.OnLoginComplete -= HandleLoginComplete;
                backendCommunicator.OnCharactersLoaded -= HandleCharactersLoaded;
                backendCommunicator.OnCharacterCreated -= HandleCharacterCreated;
                backendCommunicator.OnBattleCreated -= HandleBattleCreated;
                backendCommunicator.OnBattleCompleted -= HandleBattleCompleted;
            }
            
            if (arBattleEnvironment != null)
            {
                arBattleEnvironment.OnSuitablePlaneFound -= HandleSuitablePlaneFound;
                arBattleEnvironment.OnArenaPlaced -= HandleArenaPlaced;
            }
        }
        
        private void Update()
        {
            if (battleInProgress)
            {
                // Update battle timer
                battleTimer += Time.deltaTime;
                UpdateBattleTimerUI();
                
                // Check for battle end conditions
                if (playerHealth <= 0 || opponentHealth <= 0)
                {
                    EndBattle();
                }
            }
        }
        
        #region UI Management
        
        private void ShowPanel(GameObject panel)
        {
            // Hide all panels
            if (loginPanel != null) loginPanel.SetActive(false);
            if (characterSelectionPanel != null) characterSelectionPanel.SetActive(false);
            if (battleSetupPanel != null) battleSetupPanel.SetActive(false);
            if (battlePanel != null) battlePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            
            // Show the specified panel
            if (panel != null) panel.SetActive(true);
        }
        
        private void PopulateCharacterSelection()
        {
            // Clear existing character cards
            foreach (Transform child in characterContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create character cards
            foreach (CharacterData character in userCharacters)
            {
                GameObject cardObject = Instantiate(characterCardPrefab, characterContainer);
                CharacterCard card = cardObject.GetComponent<CharacterCard>();
                if (card != null)
                {
                    card.Initialize(character);
                    card.OnSelected += HandleCharacterSelected;
                }
            }
        }
        
        private void UpdateBattleTimerUI()
        {
            if (battleTimerText != null)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(battleTimer);
                battleTimerText.text = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
            }
        }
        
        private void UpdateHealthBars()
        {
            if (playerHealthBar != null)
                playerHealthBar.value = playerHealth / 100f;
            
            if (opponentHealthBar != null)
                opponentHealthBar.value = opponentHealth / 100f;
        }
        
        private void UpdateBattleStatus(string status)
        {
            if (battleStatusText != null)
                battleStatusText.text = status;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleLoginComplete(bool success, string message)
        {
            if (success)
            {
                Debug.Log("Login successful");
                
                // Load user characters
                backendCommunicator.GetUserCharacters();
                
                // Show character selection panel
                ShowPanel(characterSelectionPanel);
            }
            else
            {
                Debug.LogError($"Login failed: {message}");
                // Show error message to user
            }
        }
        
        private void HandleCharactersLoaded(bool success, List<CharacterData> characters)
        {
            if (success && characters != null)
            {
                Debug.Log($"Loaded {characters.Count} characters");
                userCharacters = characters;
                PopulateCharacterSelection();
            }
            else
            {
                Debug.LogError("Failed to load characters");
                // Show error message to user
            }
        }
        
        private void HandleCharacterCreated(bool success, CharacterData character)
        {
            if (success && character != null)
            {
                Debug.Log($"Created character: {character.name}");
                userCharacters.Add(character);
                PopulateCharacterSelection();
            }
            else
            {
                Debug.LogError("Failed to create character");
                // Show error message to user
            }
        }
        
        private void HandleCharacterSelected(CharacterData character)
        {
            selectedCharacter = character;
            Debug.Log($"Selected character: {character.name}");
            
            // Show battle setup panel
            ShowPanel(battleSetupPanel);
        }
        
        private void HandleBattleCreated(bool success, BattleData battle)
        {
            if (success && battle != null)
            {
                Debug.Log($"Created battle: {battle.id}");
                currentBattle = battle;
                
                // Add selected character to battle
                backendCommunicator.AddCharacterToBattle(battle.id, selectedCharacter.id);
                
                // Show battle panel
                ShowPanel(battlePanel);
                
                // Start battle
                StartBattle();
            }
            else
            {
                Debug.LogError("Failed to create battle");
                // Show error message to user
            }
        }
        
        private void HandleBattleCompleted(bool success, BattleData battle)
        {
            if (success && battle != null)
            {
                Debug.Log($"Completed battle: {battle.id}, Result: {battle.result}");
                
                // Show result panel
                ShowPanel(resultPanel);
                
                // Update result UI
                if (resultText != null)
                    resultText.text = battle.result.ToUpper();
                
                if (experienceGainedText != null)
                    experienceGainedText.text = $"+{battle.experience_gained} XP";
            }
            else
            {
                Debug.LogError("Failed to complete battle");
                // Show error message to user
            }
        }
        
        private void HandleSuitablePlaneFound(bool found)
        {
            if (arStatusText != null)
            {
                arStatusText.text = found ? 
                    "Suitable surface found. Tap to place arena." : 
                    "Looking for suitable surface...";
            }
            
            if (startBattleButton != null)
                startBattleButton.interactable = found;
        }
        
        private void HandleArenaPlaced(Vector3 position)
        {
            Debug.Log($"Arena placed at {position}");
            
            // Place characters in arena
            if (selectedCharacter != null && selectedCharacter.model_url != null)
            {
                // In a real implementation, we would load the model from the URL
                // For this prototype, we'll use a placeholder
                GameObject characterPrefab = Resources.Load<GameObject>("CharacterPlaceholder");
                if (characterPrefab != null)
                {
                    arBattleEnvironment.PlaceCharacter(selectedCharacter.id, characterPrefab, true);
                    
                    // Place opponent character
                    arBattleEnvironment.PlaceCharacter(-1, characterPrefab, false);
                }
            }
            
            // Update UI
            if (arStatusText != null)
                arStatusText.text = "Arena placed. Battle starting...";
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnCreateCharacterClicked()
        {
            // In a real implementation, this would open a character creation UI
            // For this prototype, we'll create a character with default values
            
            // Generate a random texture
            Texture2D texture = new Texture2D(256, 256);
            Color[] colors = new Color[256 * 256];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = UnityEngine.Random.ColorHSV();
            }
            texture.SetPixels(colors);
            texture.Apply();
            
            backendCommunicator.CreateCharacter(
                $"Character {UnityEngine.Random.Range(1000, 9999)}",
                "Auto-generated character",
                false,
                texture,
                "#FF0000",
                "#00FF00",
                "#0000FF"
            );
        }
        
        private void OnRefreshCharactersClicked()
        {
            backendCommunicator.GetUserCharacters();
        }
        
        private void OnStartBattleClicked()
        {
            string battleType = battleTypeDropdown != null ? 
                battleTypeDropdown.options[battleTypeDropdown.value].text.ToLower() : 
                "training";
            
            int? opponentId = null;
            if (opponentDropdown != null && opponentDropdown.value > 0)
            {
                // In a real implementation, we would get the opponent ID from the dropdown
                // For this prototype, we'll use a placeholder
                opponentId = 2;
            }
            
            backendCommunicator.CreateBattle(battleType, opponentId);
        }
        
        private void OnAttackClicked()
        {
            if (!battleInProgress)
                return;
            
            // Calculate damage
            int damage = UnityEngine.Random.Range(5, 15);
            opponentHealth -= damage;
            damageDealt += damage;
            playerScore += damage;
            
            // Update UI
            UpdateHealthBars();
            UpdateBattleStatus($"You dealt {damage} damage!");
            
            // Play attack animation and effect
            if (arBattleEnvironment != null)
            {
                GameObject playerCharacter = arBattleEnvironment.GetCharacter(selectedCharacter.id);
                if (playerCharacter != null)
                {
                    // In a real implementation, we would play the attack animation
                    // For this prototype, we'll just play an effect
                    arBattleEnvironment.PlayBattleEffect(0, playerCharacter.transform.position + Vector3.forward);
                }
            }
            
            // Add ability to used list
            usedAbilities.Add(1); // Assuming 1 is the ID of 

            // Simulate opponent response
            StartCoroutine(SimulateOpponentAction());
        }

        private void OnDefendClicked()
        {
            if (!battleInProgress)
                return;

            int reducedDamage = UnityEngine.Random.Range(2, 5);
            damageTaken += reducedDamage;
            playerHealth -= reducedDamage;
            opponentScore += reducedDamage;

            UpdateHealthBars();
            UpdateBattleStatus($"You defended and reduced damage to {reducedDamage}!");

            usedAbilities.Add(2); // Defend ability
        }

        private void OnSpecialClicked()
        {
            if (!battleInProgress)
                return;

            int specialDamage = UnityEngine.Random.Range(10, 25);
            opponentHealth -= specialDamage;
            damageDealt += specialDamage;
            playerScore += specialDamage;

            UpdateHealthBars();
            UpdateBattleStatus($"Special attack! You dealt {specialDamage} damage!");

            usedAbilities.Add(3); // Special ability

            if (arBattleEnvironment != null)
            {
                GameObject playerCharacter = arBattleEnvironment.GetCharacter(selectedCharacter.id);
                if (playerCharacter != null)
                {
                    arBattleEnvironment.PlayBattleEffect(1, playerCharacter.transform.position + Vector3.forward * 2f);
                }
            }

            // Simulate opponent response
            StartCoroutine(SimulateOpponentAction());
        }

        private void OnReturnToMenuClicked()
        {
            ShowPanel(characterSelectionPanel);
        }

        #endregion

        #region Battle Management

        private void StartBattle()
        {
            battleTimer = 0f;
            battleInProgress = true;
            playerHealth = 100;
            opponentHealth = 100;
            playerScore = 0;
            opponentScore = 0;
            damageDealt = 0;
            damageTaken = 0;
            healingDone = 0;
            usedAbilities.Clear();

            UpdateHealthBars();
            UpdateBattleStatus("Battle started!");
        }

        private void EndBattle()
        {
            battleInProgress = false;

            string result = playerHealth > 0 ? "Victory" : "Defeat";
            int experience = playerScore + damageDealt - damageTaken;

            currentBattle.result = result.ToLower();
            currentBattle.experience_gained = Mathf.Max(0, experience);

            backendCommunicator.CompleteBattle(currentBattle);
        }

        private IEnumerator SimulateOpponentAction()
        {
            yield return new WaitForSeconds(1f);

            if (!battleInProgress) yield break;

            int actionType = UnityEngine.Random.Range(0, 3);
            int value;

            switch (actionType)
            {
                case 0: // Attack
                    value = UnityEngine.Random.Range(5, 10);
                    playerHealth -= value;
                    damageTaken += value;
                    opponentScore += value;
                    UpdateBattleStatus($"Opponent attacked and dealt {value} damage.");
                    break;

                case 1: // Defend
                    value = UnityEngine.Random.Range(2, 4);
                    UpdateBattleStatus($"Opponent defended and blocked {value} damage.");
                    break;

                case 2: // Special
                    value = UnityEngine.Random.Range(8, 20);
                    playerHealth -= value;
                    damageTaken += value;
                    opponentScore += value;
                    UpdateBattleStatus($"Opponent used a special and dealt {value} damage!");
                    break;
            }

            UpdateHealthBars();
        }

        public class BattleStep
        {
            public bool isPlayer;     // true if action performed by player, false if by opponent
            public int abilityId;     // ID of the ability used
            public int damage;        // Damage dealt (if any)
            public int heal;          // Healing done (if any)
        }

        public void HandleBattleStepUpdate(BattleStep step)
        {
            if (!battleInProgress)
            {
                Debug.LogWarning("Received battle step update but battle is not in progress.");
                return;
            }

            string performerName = step.isPlayer ? "You" : "Opponent";
            int damage = step.damage;
            int heal = step.heal;
            int abilityId = step.abilityId;

            // Apply damage or healing
            if (step.isPlayer)
            {
                if (damage > 0)
                {
                    opponentHealth = Mathf.Max(0, opponentHealth - damage);
                    damageDealt += damage;
                    playerScore += damage;
                }

                if (heal > 0)
                {
                    playerHealth = Mathf.Min(100, playerHealth + heal);
                    healingDone += heal;
                }
            }
            else
            {
                if (damage > 0)
                {
                    playerHealth = Mathf.Max(0, playerHealth - damage);
                    damageTaken += damage;
                    opponentScore += damage;
                }

                if (heal > 0)
                {
                    opponentHealth = Mathf.Min(100, opponentHealth + heal);
                }
            }

            // Track used ability
            usedAbilities.Add(abilityId);

            // Update UI
            UpdateHealthBars();
            UpdateBattleStatus($"{performerName} used ability {abilityId}. " +
                               (damage > 0 ? $"Dealt {damage} damage. " : "") +
                               (heal > 0 ? $"Healed {heal} HP." : ""));

            // Trigger effects
            if (arBattleEnvironment != null)
            {
                int characterId = step.isPlayer ? selectedCharacter.id : -1;
                GameObject performer = arBattleEnvironment.GetCharacter(characterId);

                if (performer != null)
                {
                    Vector3 effectPosition = performer.transform.position + Vector3.forward;
                    arBattleEnvironment.PlayBattleEffect(abilityId, effectPosition);
                }
            }

            // Check for battle end
            if (playerHealth <= 0 || opponentHealth <= 0)
            {
                EndBattle();
            }
        }

        #endregion
    }
}
