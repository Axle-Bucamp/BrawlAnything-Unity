using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrawlAnything.Character;
using BrawlAnything.Network;

namespace BrawlAnything.Tests
{
    /// <summary>
    /// Classe de test pour vérifier l'intégration du SDK Animate Anything World et la génération vocale
    /// </summary>
    public class AnimateAnythingTester : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool logTestResults = true;
        [SerializeField] private Transform modelDisplayArea;
        
        [Header("Test Animate Anything")]
        [SerializeField] private string[] testModelNames = new string[] { "cat", "dog", "dragon" };
        [SerializeField] private string testTextPrompt = "a blue cartoon robot";
        
        [Header("Test Voice Generation")]
        [SerializeField] private string[] testSoundEffects = new string[] { 
            "laser blast", 
            "explosion", 
            "victory fanfare", 
            "punch impact" 
        };
        
        [Header("UI References")]
        [SerializeField] private TMPro.TextMeshProUGUI statusText;
        [SerializeField] private UnityEngine.UI.Button runTestsButton;
        [SerializeField] private UnityEngine.UI.ScrollRect logScrollRect;
        [SerializeField] private TMPro.TextMeshProUGUI logText;
        
        // Références aux composants à tester
        private AnimateAnythingService animateAnythingService;
        private VoiceGenerationManager voiceGenerationManager;
        
        // État des tests
        private bool isRunningTests = false;
        private List<string> testResults = new List<string>();
        private int totalTests = 0;
        private int passedTests = 0;
        
        private void Awake()
        {
            // Obtenir les références
            animateAnythingService = AnimateAnythingService.Instance;
            voiceGenerationManager = VoiceGenerationManager.Instance;
            
            // Configurer l'UI
            if (runTestsButton != null)
            {
                runTestsButton.onClick.AddListener(RunAllTests);
            }
        }
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(DelayedRunTests());
            }
        }
        
        private IEnumerator DelayedRunTests()
        {
            // Attendre que tous les composants soient initialisés
            yield return new WaitForSeconds(1f);
            
            RunAllTests();
        }
        
        /// <summary>
        /// Exécute tous les tests d'intégration
        /// </summary>
        public void RunAllTests()
        {
            if (isRunningTests)
                return;
                
            isRunningTests = true;
            testResults.Clear();
            totalTests = 0;
            passedTests = 0;
            
            UpdateStatus("Exécution des tests d'intégration Animate Anything...");
            ClearLog();
            
            StartCoroutine(RunTestsCoroutine());
        }
        
        private IEnumerator RunTestsCoroutine()
        {
            // Test 1: Vérifier les références
            yield return StartCoroutine(TestReferences());
            
            // Test 2: Tester la recherche de modèles
            yield return StartCoroutine(TestModelSearch());
            
            // Test 3: Tester la génération de modèles
            yield return StartCoroutine(TestModelGeneration());
            
            // Test 4: Tester la génération vocale
            yield return StartCoroutine(TestVoiceGeneration());
            
            // Afficher le résumé des tests
            string summary = $"Tests terminés: {passedTests}/{totalTests} réussis";
            LogMessage(summary, Color.white);
            UpdateStatus(summary);
            
            isRunningTests = false;
        }
        
        #region Test Methods
        
        private IEnumerator TestReferences()
        {
            LogTestHeader("Test des références");
            
            // Test 1.1: Vérifier AnimateAnythingService
            TestResult("AnimateAnythingService", animateAnythingService != null);
            
            // Test 1.2: Vérifier VoiceGenerationManager
            TestResult("VoiceGenerationManager", voiceGenerationManager != null);
            
            yield return null;
        }
        
        private IEnumerator TestModelSearch()
        {
            LogTestHeader("Test de recherche de modèles");
            
            if (animateAnythingService == null)
            {
                LogMessage("AnimateAnythingService non disponible, tests ignorés", Color.yellow);
                yield break;
            }
            
            // Test 2.1: Recherche de modèles
            bool searchCompleted = false;
            ModelSearchResult[] searchResults = null;
            
            // S'abonner à l'événement de recherche
            Action<ModelSearchResult[]> onSearchCompleted = (results) => { 
                searchCompleted = true;
                searchResults = results;
            };
            
            animateAnythingService.OnSearchCompleted += onSearchCompleted;
            
            // Lancer la recherche
            animateAnythingService.SearchModels("cat");
            
            // Attendre la fin de la recherche
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!searchCompleted && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
            
            // Se désabonner de l'événement
            animateAnythingService.OnSearchCompleted -= onSearchCompleted;
            
            TestResult("AnimateAnythingService.SearchModels", searchCompleted && searchResults != null);
            
            if (searchResults != null)
            {
                LogMessage($"Résultats trouvés: {searchResults.Length}", Color.green);
                foreach (var result in searchResults)
                {
                    LogMessage($"- {result.name}", Color.green);
                }
            }
        }
        
        private IEnumerator TestModelGeneration()
        {
            LogTestHeader("Test de génération de modèles");
            
            if (animateAnythingService == null)
            {
                LogMessage("AnimateAnythingService non disponible, tests ignorés", Color.yellow);
                yield break;
            }
            
            // Test 3.1: Génération de modèle à partir d'un nom
            if (testModelNames.Length > 0)
            {
                string modelName = testModelNames[0];
                bool generationCompleted = false;
                GameObject generatedModel = null;
                
                animateAnythingService.GenerateModel(modelName, (model, error) => {
                    generationCompleted = true;
                    generatedModel = model;
                });
                
                // Attendre la fin de la génération
                float timeout = 10f;
                float elapsed = 0f;
                
                while (!generationCompleted && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                }
                
                TestResult($"AnimateAnythingService.GenerateModel({modelName})", generationCompleted && generatedModel != null);
                
                if (generatedModel != null && modelDisplayArea != null)
                {
                    generatedModel.transform.SetParent(modelDisplayArea);
                    generatedModel.transform.localPosition = Vector3.zero;
                }
            }
            
            // Test 3.2: Génération de modèle à partir d'une description textuelle
            bool textGenerationCompleted = false;
            GameObject textGeneratedModel = null;
            
            animateAnythingService.GenerateModelFromText(testTextPrompt, "TextGeneratedModel", (model, error) => {
                textGenerationCompleted = true;
                textGeneratedModel = model;
            });
            
            // Attendre la fin de la génération
            float textTimeout = 15f;
            float textElapsed = 0f;
            
            while (!textGenerationCompleted && textElapsed < textTimeout)
            {
                yield return new WaitForSeconds(0.5f);
                textElapsed += 0.5f;
            }
            
            TestResult($"AnimateAnythingService.GenerateModelFromText", textGenerationCompleted && textGeneratedModel != null);
            
            if (textGeneratedModel != null && modelDisplayArea != null)
            {
                textGeneratedModel.transform.SetParent(modelDisplayArea);
                textGeneratedModel.transform.localPosition = new Vector3(2, 0, 0);
            }
        }
        
        private IEnumerator TestVoiceGeneration()
        {
            LogTestHeader("Test de génération vocale");
            
            if (voiceGenerationManager == null)
            {
                LogMessage("VoiceGenerationManager non disponible, tests ignorés", Color.yellow);
                yield break;
            }
            
            // Test 4.1: Génération d'effets sonores
            if (testSoundEffects.Length > 0)
            {
                for (int i = 0; i < Mathf.Min(2, testSoundEffects.Length); i++)
                {
                    string effectDescription = testSoundEffects[i];
                    bool generationCompleted = false;
                    AudioClip generatedClip = null;
                    
                    voiceGenerationManager.GenerateSoundEffect(effectDescription, (clip, error) => {
                        generationCompleted = true;
                        generatedClip = clip;
                    });
                    
                    // Attendre la fin de la génération
                    float timeout = 5f;
                    float elapsed = 0f;
                    
                    while (!generationCompleted && elapsed < timeout)
                    {
                        yield return new WaitForSeconds(0.5f);
                        elapsed += 0.5f;
                    }
                    
                    TestResult($"VoiceGenerationManager.GenerateSoundEffect({effectDescription})", generationCompleted && generatedClip != null);
                    
                    // Jouer l'effet sonore si généré avec succès
                    if (generatedClip != null)
                    {
                        AudioSource.PlayClipAtPoint(generatedClip, Camera.main.transform.position, 0.5f);
                        yield return new WaitForSeconds(1f);
                    }
                }
            }
            
            // Test 4.2: Ajout et lecture d'un preset d'effet sonore
            string presetId = "test_effect";
            string presetDescription = "magical sparkle sound";
            
            bool presetAdded = voiceGenerationManager.AddSoundEffectPreset(presetId, presetDescription);
            TestResult("VoiceGenerationManager.AddSoundEffectPreset", presetAdded);
            
            // Attendre que le preset soit généré
            yield return new WaitForSeconds(2f);
            
            // Tester la lecture du preset
            bool presetPlayed = voiceGenerationManager.PlaySoundEffect(presetId);
            TestResult("VoiceGenerationManager.PlaySoundEffect", presetPlayed);
            
            yield return new WaitForSeconds(1f);
        }
        
        #endregion
        
        #region Helper Methods
        
        private void TestResult(string testName, bool success)
        {
            totalTests++;
            
            if (success)
            {
                passedTests++;
                LogMessage($"✓ {testName}: Réussi", Color.green);
            }
            else
            {
                LogMessage($"✗ {testName}: Échoué", Color.red);
            }
            
            testResults.Add($"{testName}: {(success ? "Réussi" : "Échoué")}");
        }
        
        private void LogTestHeader(string header)
        {
            LogMessage($"\n=== {header} ===", Color.cyan);
        }
        
        private void LogMessage(string message, Color color)
        {
            if (logTestResults)
            {
                Debug.Log($"[AnimateAnythingTester] {message}");
            }
            
            if (logText != null)
            {
                logText.text += $"\n<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>";
                
                // Faire défiler vers le bas
                if (logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
        
        private void ClearLog()
        {
            if (logText != null)
            {
                logText.text = "";
            }
        }
        
        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }
        
        #endregion
    }
}
