            
            if (characterPreviewImage.texture == null)
            {
                Debug.LogError("Character image is required");
                return;
            }
            
            // In a real implementation, we would create a character and generate models
            // For the prototype, we'll simulate this process
            ShowLoadingPanel("Generating 3D models...");
            
            // Simulate model generation with a delay
            StartCoroutine(SimulateModelGeneration());
        }
        
        private IEnumerator SimulateModelGeneration()
        {
            // Simulate progress updates
            for (float progress = 0f; progress <= 1f; progress += 0.1f)
            {
                UpdateLoadingProgress(progress, $"Generating 3D models... {progress * 100:0}%");
                yield return new WaitForSeconds(0.5f);
            }
            
            // Show model selection panel with simulated models
            ShowModelSelectionPanel();
            
            // Clear existing models
            foreach (Transform child in modelListContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Add simulated models
            for (int i = 0; i < 4; i++)
            {
                GameObject modelItem = Instantiate(modelItemPrefab, modelListContainer);
                // In a real implementation, we would set the model preview image
                // For the prototype, we'll use a placeholder
                
                // Add click handler to select this model
                Button button = modelItem.GetComponent<Button>();
                if (button != null)
                {
                    int modelIndex = i;
                    button.onClick.AddListener(() => OnModelItemClicked(modelIndex));
                }
            }
        }
        
        private void OnModelItemClicked(int modelIndex)
        {
            // In a real implementation, we would select this model
            Debug.Log($"Selected model {modelIndex}");
            
            // Enable the select button
            selectModelButton.interactable = true;
        }
        
        private void OnSelectModelButtonClicked()
        {
            // In a real implementation, we would save the selected model
            // For the prototype, we'll simulate this process
            ShowLoadingPanel("Saving character...");
            
            // Simulate saving with a delay
            StartCoroutine(SimulateSavingCharacter());
        }
        
        private IEnumerator SimulateSavingCharacter()
        {
            // Simulate progress updates
            for (float progress = 0f; progress <= 1f; progress += 0.2f)
            {
                UpdateLoadingProgress(progress, $"Saving character... {progress * 100:0}%");
                yield return new WaitForSeconds(0.3f);
            }
            
            // Return to character selection
            ShowCharacterSelectionPanel();
        }
        
        private void OnAttackButtonClicked()
        {
            // In a real implementation, this would trigger an attack animation
            Debug.Log("Attack button clicked");

            CharacterData activeCharacter = CharacterManager.Instance.GetActiveCharacter();
            if (activeCharacter != null)
            {
                // Find the character instance and play attack animation
                foreach (CharacterBehaviour characterBehaviour in FindObjectsOfType<CharacterBehaviour>())
                {
                    if (characterBehaviour.CharacterData.id == activeCharacter.id)
                    {
                        characterBehaviour.Attack();
                        break;
                    }
                }
            }
        }
        
        private void OnDefendButtonClicked()
        {
            // In a real implementation, this would trigger a defend animation
            Debug.Log("Defend button clicked");

            CharacterData activeCharacter = CharacterManager.Instance.GetActiveCharacter();
            if (activeCharacter != null)
            {
                // Find the character instance and play defend animation
                foreach (CharacterBehaviour characterBehaviour in FindObjectsOfType<CharacterBehaviour>())
                {
                    if (characterBehaviour.CharacterData.id == activeCharacter.id)
                    {
                        characterBehaviour.Defend();
                        break;
                    }
                }
            }
        }
        
        private void OnSpecialButtonClicked()
        {
            // In a real implementation, this would trigger a special attack animation
            Debug.Log("Special button clicked");

            CharacterData activeCharacter = CharacterManager.Instance.GetActiveCharacter();
            if (activeCharacter != null)
            {
                // Find the character instance and play special animation
                foreach (CharacterBehaviour characterBehaviour in FindObjectsOfType<CharacterBehaviour>())
                {
                    if (characterBehaviour.CharacterData.id == activeCharacter.id)
                    {
                        // For prototype, we'll use victory animation as special
                        characterBehaviour.Victory();
                        break;
                    }
                }
            }
        }

        #endregion

        #region CharacterData Management
        private void LoadCharacters()
        {
            // Clear existing character items
            foreach (Transform child in characterListContainer)
            {
                Destroy(child.gameObject);
            }

            // Load characters from CharacterManager
            CharacterManager.Instance.LoadUserCharacters((characters) =>
            {
                if (characters == null || characters.Count == 0)
                {
                    Debug.LogWarning("No characters found or failed to load.");
                    return;
                }

                foreach (var character in characters)
                {
                    GameObject item = Instantiate(characterItemPrefab, characterListContainer);
                    CharacterCard card = item.GetComponent<CharacterCard>();

                    if (card != null)
                    {
                        StartCoroutine(LoadPreviewAndInitializeCard(card, character));
                    }
                    else
                    {
                        Debug.LogWarning("CharacterCard component not found on prefab.");
                    }
                }
            });
        }


        private IEnumerator LoadPreviewAndInitializeCard(CharacterCard card, CharacterData character)
        {
            card.Initialize(character); // Only pass the character, let the card handle image loading and selection callback

            card.OnSelected += selected =>
            {
                CharacterManager.Instance.SetActiveCharacter(selected);
                ShowBattlePanel();
            };

            yield return null; // Ensure coroutine is still used properly if you're calling this via StartCoroutine
        }


        #endregion
    }
}
