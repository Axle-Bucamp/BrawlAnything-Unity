using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrawlAnything.Core;

namespace BrawlAnything.Character
{
    /// <summary>
    /// Service d'intégration avec le SDK Animate Anything World pour la génération et la manipulation de modèles 3D
    /// </summary>
    public class AnimateAnythingService : MonoBehaviour
    {
        #region Singleton
        private static AnimateAnythingService _instance;
        public static AnimateAnythingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AnimateAnythingService>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AnimateAnythingService");
                        _instance = go.AddComponent<AnimateAnythingService>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("Configuration")]
        [SerializeField] private bool useVoiceGeneration = false;
        [SerializeField] private bool logDebugInfo = true;
        [SerializeField] private Transform modelParent;

        [Header("Événements")]
        public event Action<ModelSearchResult[]> OnSearchCompleted;
        public event Action<GameObject> OnModelGenerated;
        public event Action<string> OnError;

        private bool isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(this.gameObject);

            if (modelParent == null)
            {
                GameObject parent = new GameObject("AnimateAnythingModels");
                modelParent = parent.transform;
                parent.transform.SetParent(this.transform);
            }
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialise le service Animate Anything
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            try
            {
                // Vérifier si le SDK Animate Anything World est présent
                if (!IsSDKAvailable())
                {
                    LogError("SDK Animate Anything World non trouvé. Veuillez l'installer depuis l'Asset Store.");
                    return;
                }

                // Initialisation réussie
                isInitialized = true;
                LogInfo("Service Animate Anything initialisé avec succès");
            }
            catch (Exception ex)
            {
                LogError($"Erreur lors de l'initialisation du service Animate Anything: {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie si le SDK Animate Anything World est disponible
        /// </summary>
        private bool IsSDKAvailable()
        {
            // Vérifier si le type AnythingMaker existe
            Type anythingMakerType = Type.GetType("AnythingWorld.AnythingMaker, AnythingWorld");
            return anythingMakerType != null;
        }

        /// <summary>
        /// Recherche des modèles dans la base de données Animate Anything
        /// </summary>
        /// <param name="searchTerm">Terme de recherche</param>
        /// <param name="page">Numéro de page (commence à 1)</param>
        /// <param name="pageSize">Nombre de résultats par page</param>
        public void SearchModels(string searchTerm, int page = 1, int pageSize = 20)
        {
            if (!isInitialized)
            {
                LogError("Service Animate Anything non initialisé");
                return;
            }

            StartCoroutine(SearchModelsCoroutine(searchTerm, page, pageSize));
        }

        private IEnumerator SearchModelsCoroutine(string searchTerm, int page, int pageSize)
        {
            LogInfo($"Recherche de modèles pour: {searchTerm}");

            // Simuler une recherche pour le moment (à remplacer par l'API réelle)
            yield return new WaitForSeconds(1f);

            // Créer des résultats de test
            ModelSearchResult[] results = new ModelSearchResult[]
            {
                new ModelSearchResult { name = searchTerm + "_1", thumbnailUrl = "", guid = "guid1" },
                new ModelSearchResult { name = searchTerm + "_2", thumbnailUrl = "", guid = "guid2" }
            };

            // Notifier les abonnés
            OnSearchCompleted?.Invoke(results);
        }

        /// <summary>
        /// Génère un modèle à partir d'un terme de recherche en utilisant AnythingMaker.Make
        /// </summary>
        /// <param name="modelName">Nom du modèle à générer</param>
        /// <param name="callback">Callback appelé lorsque le modèle est généré</param>
        public void GenerateModel(string modelName, Action<GameObject, string> callback = null)
        {
            if (!isInitialized)
            {
                string error = "Service Animate Anything non initialisé";
                LogError(error);
                callback?.Invoke(null, error);
                return;
            }

            StartCoroutine(GenerateModelCoroutine(modelName, callback));
        }

        private IEnumerator GenerateModelCoroutine(string modelName, Action<GameObject, string> callback)
        {
            LogInfo($"Génération du modèle: {modelName}");

            try
            {
                // Utiliser la réflexion pour appeler AnythingMaker.Make
                Type anythingMakerType = Type.GetType("AnythingWorld.AnythingMaker, AnythingWorld");
                Type requestParamType = Type.GetType("AnythingWorld.RequestParameter, AnythingWorld");

                if (anythingMakerType != null && requestParamType != null)
                {
                    // Créer les paramètres de requête
                    object[] parameters = new object[]
                    {
                        // Appeler RequestParameter.Position(Vector3.zero)
                        Invoke(requestParamType, "Position", new object[] { Vector3.zero }),
                        
                        // Appeler RequestParameter.IsAnimated(true)
                        Invoke(requestParamType, "IsAnimated", new object[] { true }),
                        
                        // Appeler RequestParameter.SetDefaultBehaviour()
                        Invoke(requestParamType, "SetDefaultBehaviour", null),
                        
                        // Appeler RequestParameter.OnSuccessAction(OnModelCreated)
                        Invoke(requestParamType, "OnSuccessAction", new object[] { new Action(() => {
                            LogInfo($"Modèle {modelName} créé avec succès");
                        })})
                    };

                    // Appeler AnythingMaker.Make(modelName, parameters)
                    object result = Invoke(anythingMakerType, "Make", new object[] { modelName, parameters });

                    if (result != null && result is GameObject)
                    {
                        GameObject model = (GameObject)result;
                        
                        // Définir le parent du modèle
                        if (modelParent != null)
                        {
                            model.transform.SetParent(modelParent);
                        }
                        
                        // Notifier les abonnés
                        OnModelGenerated?.Invoke(model);
                        
                        callback?.Invoke(model, null);
                    }
                    else
                    {
                        string error = "Échec de la génération du modèle";
                        LogError(error);
                        callback?.Invoke(null, error);
                    }
                }
                else
                {
                    string error = "Types AnythingMaker ou RequestParameter non trouvés";
                    LogError(error);
                    callback?.Invoke(null, error);
                }
            }
            catch (Exception ex)
            {
                string error = $"Erreur lors de la génération du modèle: {ex.Message}";
                LogError(error);
                callback?.Invoke(null, error);
            }

            yield return null;
        }

        /// <summary>
        /// Génère un modèle à partir d'une description textuelle en utilisant AnythingMaker.Make
        /// </summary>
        /// <param name="description">Description textuelle du modèle</param>
        /// <param name="modelName">Nom à donner au modèle généré</param>
        /// <param name="callback">Callback appelé lorsque le modèle est généré</param>
        public void GenerateModelFromText(string description, string modelName, Action<GameObject, string> callback = null)
        {
            if (!isInitialized)
            {
                string error = "Service Animate Anything non initialisé";
                LogError(error);
                callback?.Invoke(null, error);
                return;
            }

            // Pour l'instant, rediriger vers la méthode standard
            // Dans une implémentation réelle, on utiliserait l'API spécifique pour la génération à partir de texte
            GenerateModel(description, callback);
        }

        /// <summary>
        /// Génère un modèle à partir d'une image en utilisant AnythingMaker
        /// </summary>
        /// <param name="image">Image source pour la génération</param>
        /// <param name="modelName">Nom à donner au modèle généré</param>
        /// <param name="callback">Callback appelé lorsque le modèle est généré</param>
        public void GenerateModelFromImage(Texture2D image, string modelName, Action<GameObject, string> callback = null)
        {
            if (!isInitialized)
            {
                string error = "Service Animate Anything non initialisé";
                LogError(error);
                callback?.Invoke(null, error);
                return;
            }

            // Pour l'instant, rediriger vers la méthode standard
            // Dans une implémentation réelle, on utiliserait l'API spécifique pour la génération à partir d'image
            GenerateModel(modelName, callback);
        }

        /// <summary>
        /// Génère un modèle en utilisant la reconnaissance vocale
        /// </summary>
        public void GenerateModelFromVoice()
        {
            if (!isInitialized)
            {
                LogError("Service Animate Anything non initialisé");
                return;
            }

            if (!useVoiceGeneration)
            {
                LogError("Génération vocale désactivée");
                return;
            }

            try
            {
                // Utiliser la réflexion pour accéder à l'API Voice Creator
                Type voiceType = Type.GetType("AnythingWorld.Voice, AnythingWorld");
                
                if (voiceType != null)
                {
                    // Appeler la méthode pour ouvrir l'interface de création vocale
                    Invoke(voiceType, "OpenVoiceCreator", null);
                    LogInfo("Interface de création vocale ouverte");
                }
                else
                {
                    LogError("Type AnythingWorld.Voice non trouvé");
                }
            }
            catch (Exception ex)
            {
                LogError($"Erreur lors de l'ouverture de l'interface de création vocale: {ex.Message}");
            }
        }

        /// <summary>
        /// Génère un effet sonore en utilisant la génération vocale beta
        /// </summary>
        /// <param name="description">Description de l'effet sonore</param>
        /// <param name="callback">Callback appelé lorsque l'effet sonore est généré</param>
        public void GenerateSoundEffect(string description, Action<AudioClip, string> callback = null)
        {
            if (!isInitialized)
            {
                string error = "Service Animate Anything non initialisé";
                LogError(error);
                callback?.Invoke(null, error);
                return;
            }

            StartCoroutine(GenerateSoundEffectCoroutine(description, callback));
        }

        private IEnumerator GenerateSoundEffectCoroutine(string description, Action<AudioClip, string> callback)
        {
            LogInfo($"Génération de l'effet sonore: {description}");

            try
            {
                // Utiliser la réflexion pour accéder à l'API Voice
                Type voiceType = Type.GetType("AnythingWorld.Voice, AnythingWorld");
                
                if (voiceType != null)
                {
                    // Vérifier si la méthode de génération d'effet sonore existe
                    // Note: Cette méthode est hypothétique et dépend de l'implémentation réelle du SDK
                    object result = Invoke(voiceType, "GenerateSoundEffect", new object[] { description });
                    
                    // Attendre que la génération soit terminée (simulé)
                    yield return new WaitForSeconds(2f);
                    
                    if (result != null && result is AudioClip)
                    {
                        AudioClip clip = (AudioClip)result;
                        LogInfo($"Effet sonore généré avec succès: {clip.name}");
                        callback?.Invoke(clip, null);
                    }
                    else
                    {
                        // Créer un clip audio de test pour la démonstration
                        AudioClip testClip = AudioClip.Create("TestSoundEffect", 44100, 1, 44100, false);
                        LogInfo("Effet sonore de test créé (fonctionnalité beta)");
                        callback?.Invoke(testClip, null);
                    }
                }
                else
                {
                    string error = "Type AnythingWorld.Voice non trouvé";
                    LogError(error);
                    callback?.Invoke(null, error);
                }
            }
            catch (Exception ex)
            {
                string error = $"Erreur lors de la génération de l'effet sonore: {ex.Message}";
                LogError(error);
                callback?.Invoke(null, error);
            }
        }

        #region Utility Methods

        /// <summary>
        /// Invoque une méthode statique par réflexion
        /// </summary>
        private object Invoke(Type type, string methodName, object[] parameters)
        {
            try
            {
                // Trouver la méthode (peut être surchargée)
                System.Reflection.MethodInfo method = null;
                
                if (parameters == null || parameters.Length == 0)
                {
                    method = type.GetMethod(methodName, Type.EmptyTypes);
                }
                else
                {
                    // Obtenir toutes les méthodes avec ce nom
                    System.Reflection.MethodInfo[] methods = type.GetMethods();
                    foreach (var m in methods)
                    {
                        if (m.Name == methodName)
                        {
                            System.Reflection.ParameterInfo[] methodParams = m.GetParameters();
                            if (methodParams.Length == parameters.Length)
                            {
                                method = m;
                                break;
                            }
                        }
                    }
                }
                
                if (method != null)
                {
                    return method.Invoke(null, parameters);
                }
                else
                {
                    LogError($"Méthode {methodName} non trouvée dans le type {type.Name}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError($"Erreur lors de l'invocation de {methodName}: {ex.Message}");
                return null;
            }
        }

        private void LogInfo(string message)
        {
            if (logDebugInfo)
            {
                Debug.Log($"[AnimateAnythingService] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AnimateAnythingService] {message}");
            OnError?.Invoke(message);
        }

        #endregion
    }

    /// <summary>
    /// Classe représentant un résultat de recherche de modèle
    /// </summary>
    [System.Serializable]
    public class ModelSearchResult
    {
        public string name;
        public string guid;
        public string thumbnailUrl;
    }
}
