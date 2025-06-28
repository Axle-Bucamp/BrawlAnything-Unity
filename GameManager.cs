using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrawlAnything.Managers
{
    /// <summary>
    /// Main game manager that coordinates all aspects of the game
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private AR.ARManager arManager;
        [SerializeField] private UI.UIManager uiManager;
        
        // Game state
        private bool isInBattle = false;
        private int currentRound = 0;
        private int playerScore = 0;
        private int opponentScore = 0;
        
        // Singleton instance
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
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
            
            // Find components if not assigned
            if (arManager == null) arManager = FindObjectOfType<AR.ARManager>();
            if (uiManager == null) uiManager = FindObjectOfType<UI.UIManager>();
        }
        
        private void Start()
        {
            // Subscribe to events
            if (arManager != null)
            {
                arManager.OnCharacterPlaced += OnCharacterPlaced;
            }
        }
        
        private void OnCharacterPlaced(GameObject character)
        {
            // When a character is placed in AR, start the battle
            StartBattle(character);
        }
        
        public void StartBattle(GameObject playerCharacter)
        {
            if (isInBattle) return;
            
            isInBattle = true;
            currentRound = 1;
            playerScore = 0;
            opponentScore = 0;
            
            // Show battle UI
            if (uiManager != null)
            {
                uiManager.ShowBattlePanel();
            }
            
            // In a real implementation, we would spawn an opponent character
            // For the prototype, we'll just use the player character
            Debug.Log("Battle started!");
        }
        
        public void EndBattle(bool playerWon)
        {
            if (!isInBattle) return;
            
            isInBattle = false;
            
            // Update scores
            if (playerWon)
            {
                playerScore++;
            }
            else
            {
                opponentScore++;
            }
            
            // Show results
            Debug.Log($"Battle ended! Player: {playerScore}, Opponent: {opponentScore}");
            
            // Return to character selection
            if (uiManager != null)
            {
                uiManager.ShowCharacterSelectionPanel();
            }
        }
        
        public void NextRound()
        {
            if (!isInBattle) return;
            
            currentRound++;
            Debug.Log($"Round {currentRound} started!");
        }
        
        // Methods for game state management
        public bool IsInBattle() => isInBattle;
        public int GetCurrentRound() => currentRound;
        public int GetPlayerScore() => playerScore;
        public int GetOpponentScore() => opponentScore;
    }
}
