using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrawlAnything.Core
{
    /// <summary>
    /// Gère les transitions entre les scènes et le chargement asynchrone
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        [Header("Loading Settings")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private UnityEngine.UI.Slider progressBar;
        [SerializeField] private TMPro.TextMeshProUGUI loadingText;
        [SerializeField] private float minimumLoadingTime = 0.5f;
        [SerializeField] private float fadeTime = 0.5f;
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        // Singleton instance
        private static SceneController _instance;
        public static SceneController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneController>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SceneController");
                        _instance = go.AddComponent<SceneController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Events
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize loading screen if not assigned
            if (loadingScreen == null)
            {
                Debug.LogWarning("Loading screen not assigned to SceneController. Loading transitions will not be visible.");
            }
            else
            {
                loadingScreen.SetActive(false);
            }
        }

        /// <summary>
        /// Charge une scène de manière asynchrone avec écran de chargement
        /// </summary>
        /// <param name="sceneName">Nom de la scène à charger</param>
        /// <param name="loadingMessage">Message à afficher pendant le chargement</param>
        public void LoadScene(string sceneName, string loadingMessage = "Chargement...")
        {
            StartCoroutine(LoadSceneAsync(sceneName, loadingMessage));
        }

        private IEnumerator LoadSceneAsync(string sceneName, string loadingMessage)
        {
            // Notify listeners that scene load has started
            OnSceneLoadStarted?.Invoke(sceneName);

            // Show loading screen
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
                
                if (loadingText != null)
                    loadingText.text = loadingMessage;
                
                if (progressBar != null)
                    progressBar.value = 0f;
                
                // Fade in
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = 0f;
                    while (fadeCanvasGroup.alpha < 1f)
                    {
                        fadeCanvasGroup.alpha += Time.deltaTime / fadeTime;
                        yield return null;
                    }
                }
            }

            // Start loading the scene
            float startTime = Time.time;
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = false;

            // Update progress bar
            while (!asyncOperation.isDone)
            {
                float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                
                if (progressBar != null)
                    progressBar.value = progress;

                // Check if loading is almost done and minimum time has passed
                if (asyncOperation.progress >= 0.9f && Time.time - startTime >= minimumLoadingTime)
                {
                    // Allow scene activation
                    asyncOperation.allowSceneActivation = true;
                }

                yield return null;
            }

            // Fade out
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;
                while (fadeCanvasGroup.alpha > 0f)
                {
                    fadeCanvasGroup.alpha -= Time.deltaTime / fadeTime;
                    yield return null;
                }
            }

            // Hide loading screen
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }

            // Notify listeners that scene load has completed
            OnSceneLoadCompleted?.Invoke(sceneName);
        }

        /// <summary>
        /// Recharge la scène actuelle
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Charge la scène suivante dans l'ordre de build
        /// </summary>
        public void LoadNextScene()
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = (currentSceneIndex + 1) % SceneManager.sceneCountInBuildSettings;
            LoadScene(SceneManager.GetSceneByBuildIndex(nextSceneIndex).name);
        }
    }
}
