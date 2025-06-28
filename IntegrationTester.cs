using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrawlAnything.Core;
using BrawlAnything.Character;
using BrawlAnything.Network;

namespace BrawlAnything.Tests
{
    /// <summary>
    /// Classe de test pour vérifier l'intégration et la fonctionnalité des différents composants
    /// </summary>
    public class IntegrationTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool logTestResults = true;
        
        [Header("API Test Settings")]
        [SerializeField] private string testUsername = "testuser";
        [SerializeField] private string testPassword = "testpassword";
        
        [Header("Animate Anything Test Settings")]
        [SerializeField] private string testPrompt = "a cartoon cat with blue fur";
        [SerializeField] private string testModelName = "BlueCat";
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button runTestsButton;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private TextMeshProUGUI logText;
        
        // Références aux composants à tester
        private APIClient apiClient;
        private SocketClient socketClient;
        private VaultManager vaultManager;
        private AnimateAnythingService animateAnythingService;
        private GameManager gameManager;
        private CharacterManager characterManager;
        private AR.ARManager arManager;
        
        // État des tests
        private bool isRunningTests = false;
        private List<string> testResults = new List<string>();
        private int totalTests = 0;
        private int passedTests = 0;
        
        private void Awake()
        {
            // Obtenir les références
            apiClient = APIClient.Instance;
            socketClient = SocketClient.Instance;
            vaultManager = GetComponent<VaultManager>();
            animateAnythingService = AnimateAnythingService.Instance;
            gameManager = GameManager.Instance;
            characterManager = CharacterManager.Instance;
            arManager = AR.ARManager.Instance;
            
            // Initialiser le gestionnaire Vault si nécessaire
            if (vaultManager == null)
            {
                vaultManager = gameObject.AddComponent<VaultManager>();
                vaultManager.Initialize("https://vault.guidry-cloud.com");
            }
            
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
            
            UpdateStatus("Exécution des tests d'intégration...");
            ClearLog();
            
            StartCoroutine(RunTestsCoroutine());
        }
        
        private IEnumerator RunTestsCoroutine()
        {
            // Test 1: Vérifier les références
            yield return StartCoroutine(TestReferences());
            
            // Test 2: Tester le gestionnaire Vault
            yield return StartCoroutine(TestVaultManager());
            
            // Test 3: Tester l'API Client
            yield return StartCoroutine(TestAPIClient());
            
            // Test 4: Tester le Socket Client
            yield return StartCoroutine(TestSocketClient());
            
            // Test 5: Tester le service Animate Anything
            yield return StartCoroutine(TestAnimateAnythingService());
            
            // Test 6: Tester l'intégration AR
            yield return StartCoroutine(TestARIntegration());
            
            // Test 7: Tester l'intégration des personnages
            yield return StartCoroutine(TestCharacterIntegration());
            
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
            
            // Test 1.1: Vérifier APIClient
            TestResult("APIClient", apiClient != null);
            
            // Test 1.2: Vérifier SocketClient
            TestResult("SocketClient", socketClient != null);
            
            // Test 1.3: Vérifier VaultManager
            TestResult("VaultManager", vaultManager != null);
            
            // Test 1.4: Vérifier AnimateAnythingService
            TestResult("AnimateAnythingService", animateAnythingService != null);
            
            // Test 1.5: Vérifier GameManager
            TestResult("GameManager", gameManager != null);
            
            // Test 1.6: Vérifier CharacterManager
            TestResult("CharacterManager", characterManager != null);
            
            // Test 1.7: Vérifier ARManager
            TestResult("ARManager", arManager != null);
            
            yield return null;
        }
        
        private IEnumerator TestVaultManager()
        {
            LogTestHeader("Test du gestionnaire Vault");
            
            // Test 2.1: Vérifier l'initialisation
            TestResult("VaultManager.Initialize", vaultManager.IsInitialized());
            
            // Test 2.2: Stocker un secret
            string testKey = "test_key";
            string testValue = "test_value_" + DateTime.Now.Ticks;
            bool storeSuccess = false;
            
            vaultManager.StoreSecret(testKey, testValue, (success, error) => {
                storeSuccess = success;
            });
            
            yield return new WaitForSeconds(0.5f);
            
            TestResult("VaultManager.StoreSecret", storeSuccess);
            
            // Test 2.3: Récupérer un secret
            string retrievedValue = null;
            
            vaultManager.GetSecretAsync(testKey, (value, error) => {
                retrievedValue = value;
            });
            
            yield return new WaitForSeconds(0.5f);
            
            TestResult("VaultManager.GetSecret", retrievedValue == testValue);
            
            // Test 2.4: Supprimer un secret
            bool deleteSuccess = false;
            
            vaultManager.DeleteSecret(testKey, (success, error) => {
                deleteSuccess = success;
            });
            
            yield return new WaitForSeconds(0.5f);
            
            TestResult("VaultManager.DeleteSecret", deleteSuccess);
        }
        
        private IEnumerator TestAPIClient()
        {
            LogTestHeader("Test de l'API Client");
            
            // Test 3.1: Vérifier l'initialisation
            TestResult("APIClient.Initialize", apiClient != null);
            
            // Test 3.2: Tester la connexion
            bool loginSuccess = false;
            
            yield return StartCoroutine(apiClient.Login(testUsername, testPassword, (success, error) => {
                loginSuccess = success;
            }));
            
            TestResult("APIClient.Login", loginSuccess);
            
            // Si la connexion a échoué, ne pas continuer les tests d'API
            if (!loginSuccess)
            {
                LogMessage("Impossible de se connecter à l'API, tests suivants ignorés", Color.yellow);
                yield break;
            }
            
            // Test 3.3: Récupérer l'utilisateur courant
            UserData userData = null;
            
            yield return StartCoroutine(apiClient.GetCurrentUser((user, error) => {
                userData = user;
            }));
            
            TestResult("APIClient.GetCurrentUser", userData != null);
            
            // Test 3.4: Récupérer les personnages
            List<CharacterData> characters = null;
            
            yield return StartCoroutine(apiClient.GetCharacters((chars, error) => {
                characters = chars;
            }));
            
            TestResult("APIClient.GetCharacters", characters != null);
            
            // Test 3.5: Déconnexion
            apiClient.Logout();
            TestResult("APIClient.Logout", !apiClient.IsAuthenticated());
        }
        
        private IEnumerator TestSocketClient()
        {
            LogTestHeader("Test du Socket Client");
            
            // Test 4.1: Vérifier l'initialisation
            TestResult("SocketClient.Initialize", socketClient != null);
            
            // Test 4.2: Connexion au socket
            bool wasConnected = socketClient.IsConnected();
            bool connectionEventReceived = false;
            
            // S'abonner à l'événement de connexion
            Action onConnected = () => { connectionEventReceived = true; };
            socketClient.OnConnected += onConnected;
            
            // Se connecter si nécessaire
            if (!wasConnected)
            {
                // Se connecter d'abord à l'API pour obtenir un token
                yield return StartCoroutine(apiClient.Login(testUsername, testPassword, (success, error) => {}));
                
                if (apiClient.IsAuthenticated())
                {
                    UserData userData = null;
                    yield return StartCoroutine(apiClient.GetCurrentUser((user, error) => {
                        userData = user;
                    }));
                    
                    if (userData != null)
                    {
                        socketClient.Connect(userData.id.ToString(), apiClient.GetAuthToken());
                        
                        // Attendre la connexion
                        float timeout = 5f;
                        float elapsed = 0f;
                        
                        while (!connectionEventReceived && elapsed < timeout)
                        {
                            yield return new WaitForSeconds(0.1f);
                            elapsed += 0.1f;
                        }
                    }
                }
            }
            
            // Se désabonner de l'événement
            socketClient.OnConnected -= onConnected;
            
            TestResult("SocketClient.Connect", wasConnected || connectionEventReceived);
            
            // Test 4.3: Vérifier le sérialiseur FEN
            FENSerializer fenSerializer = new FENSerializer();
            
            // Créer des données de test
            BattleUpdateData testData = new BattleUpdateData
            {
                battleId = 12345,
                status = "active",
                timeRemaining = 120.5f,
                characters = new List<CharacterStateData>
                {
                    new CharacterStateData
                    {
                        characterId = 1,
                        currentHealth = 100,
                        position = new Vector3(1, 2, 3),
                        rotation = Quaternion.Euler(10, 20, 30),
                        currentAnimation = "idle",
                        statusEffects = new List<StatusEffectData>
                        {
                            new StatusEffectData { type = "stun", duration = 5f, intensity = 1f }
                        }
                    }
                },
                customData = new Dictionary<string, object>
                {
                    { "test", "value" }
                }
            };
            
            // Sérialiser
            string fenData = fenSerializer.SerializeBattleUpdate(testData);
            
            // Désérialiser
            BattleUpdateData deserializedData = fenSerializer.DeserializeBattleUpdate(fenData);
            
            TestResult("FENSerializer.Serialize/Deserialize", 
                deserializedData != null && 
                deserializedData.battleId == testData.battleId &&
                deserializedData.status == testData.status &&
                deserializedData.characters.Count == testData.characters.Count);
        }
        
        private IEnumerator TestAnimateAnythingService()
        {
            LogTestHeader("Test du service Animate Anything");
            
            // Test 5.1: Vérifier l'initialisation
            TestResult("AnimateAnythingService.Initialize", animateAnythingService != null);
            
            // Test 5.2: Recherche de modèles
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
            float timeout = 10f;
            float elapsed = 0f;
            
            while (!searchCompleted && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
            
            // Se désabonner de l'événement
            animateAnythingService.OnSearchCompleted -= onSearchCompleted;
            
            TestResult("AnimateAnythingService.SearchModels", searchCompleted && searchResults != null);
            
            // Test 5.3: Génération de modèle (simulée)
            // Note: La génération réelle prendrait trop de temps pour un test
            LogMessage("Test de génération de modèle ignoré (trop long pour un test)", Color.yellow);
        }
        
        private IEnumerator TestARIntegration()
        {
            LogTestHeader("Test de l'intégration AR");
            
            // Test 6.1: Vérifier l'initialisation
            TestResult("ARManager.Initialize", arManager != null);
            
            // Test 6.2: Vérifier la disponibilité AR
            bool arAvailable = arManager.IsARAvailable();
            LogMessage($"AR disponible: {arAvailable}", arAvailable ? Color.green : Color.yellow);
            
            // Test 6.3: Vérifier l'expérience partagée
            AR.SharedARExperience sharedARExperience = FindObjectOfType<AR.SharedARExperience>();
            TestResult("SharedARExperience.Initialize", sharedARExperience != null);
            
            yield return null;
        }
        
        private IEnumerator TestCharacterIntegration()
        {
            LogTestHeader("Test de l'intégration des personnages");
            
            // Test 7.1: Vérifier l'initialisation
            TestResult("CharacterManager.Initialize", characterManager != null);
            
            // Test 7.2: Vérifier le ModelViewer
            ModelViewer modelViewer = FindObjectOfType<ModelViewer>();
            TestResult("ModelViewer.Initialize", modelViewer != null);
            
            yield return null;
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
                Debug.Log($"[IntegrationTester] {message}");
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
