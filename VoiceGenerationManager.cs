using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrawlAnything.Core;

namespace BrawlAnything.Character
{
    /// <summary>
    /// Gestionnaire pour la génération vocale et les effets sonores utilisant la fonctionnalité beta du SDK Animate Anything World
    /// </summary>
    public class VoiceGenerationManager : MonoBehaviour
    {
        #region Singleton
        private static VoiceGenerationManager _instance;
        public static VoiceGenerationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VoiceGenerationManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("VoiceGenerationManager");
                        _instance = go.AddComponent<VoiceGenerationManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("Configuration")]
        [SerializeField] private bool enableVoiceGeneration = true;
        [SerializeField] private bool logDebugInfo = true;
        [SerializeField] private AudioSource effectsAudioSource;
        [SerializeField] private float defaultVolume = 0.7f;

        [Header("Effets sonores prédéfinis")]
        [SerializeField] private List<SoundEffectPreset> soundEffectPresets = new List<SoundEffectPreset>();

        // Dictionnaire pour stocker les clips audio générés
        private Dictionary<string, AudioClip> generatedClips = new Dictionary<string, AudioClip>();
        private bool isInitialized = false;

        // Événements
        public event Action<AudioClip> OnSoundEffectGenerated;
        public event Action<string> OnError;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(this.gameObject);

            if (effectsAudioSource == null)
            {
                effectsAudioSource = gameObject.AddComponent<AudioSource>();
                effectsAudioSource.playOnAwake = false;
                effectsAudioSource.volume = defaultVolume;
            }
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialise le gestionnaire de génération vocale
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

                // Précharger les effets sonores prédéfinis
                StartCoroutine(PreloadSoundEffects());

                // Initialisation réussie
                isInitialized = true;
                LogInfo("Gestionnaire de génération vocale initialisé avec succès");
            }
            catch (Exception ex)
            {
                LogError($"Erreur lors de l'initialisation du gestionnaire de génération vocale: {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie si le SDK Animate Anything World est disponible
        /// </summary>
        private bool IsSDKAvailable()
        {
            // Vérifier si le type AnythingWorld.Voice existe
            Type voiceType = Type.GetType("AnythingWorld.Voice, AnythingWorld");
            return voiceType != null;
        }

        /// <summary>
        /// Précharge les effets sonores prédéfinis
        /// </summary>
        private IEnumerator PreloadSoundEffects()
        {
            LogInfo("Préchargement des effets sonores prédéfinis...");

            foreach (var preset in soundEffectPresets)
            {
                if (!string.IsNullOrEmpty(preset.description) && !generatedClips.ContainsKey(preset.id))
                {
                    yield return StartCoroutine(GenerateSoundEffectCoroutine(preset.description, (clip, error) =>
                    {
                        if (clip != null)
                        {
                            generatedClips[preset.id] = clip;
                            LogInfo($"Effet sonore préchargé: {preset.id}");
                        }
                    }));

                    // Attendre un peu entre chaque génération pour éviter de surcharger l'API
                    yield return new WaitForSeconds(0.5f);
                }
            }

            LogInfo($"Préchargement terminé. {generatedClips.Count} effets sonores disponibles.");
        }

        /// <summary>
        /// Génère un effet sonore à partir d'une description
        /// </summary>
        /// <param name="description">Description de l'effet sonore à générer</param>
        /// <param name="callback">Callback appelé lorsque l'effet sonore est généré</param>
        public void GenerateSoundEffect(string description, Action<AudioClip, string> callback = null)
        {
            if (!isInitialized)
            {
                string error = "Gestionnaire de génération vocale non initialisé";
                LogError(error);
                callback?.Invoke(null, error);
                return;
            }

            if (!enableVoiceGeneration)
            {
                string error = "Génération vocale désactivée";
                LogError(error);
                callback?.Invoke(null, error);
                return;
            }

            StartCoroutine(GenerateSoundEffectCoroutine(description, callback));
        }

        /// <summary>
        /// Coroutine pour générer un effet sonore
        /// </summary>
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
                    yield return new WaitForSeconds(1.5f);
                    
                    if (result != null && result is AudioClip)
                    {
                        AudioClip clip = (AudioClip)result;
                        LogInfo($"Effet sonore généré avec succès: {clip.name}");
                        
                        // Notifier les abonnés
                        OnSoundEffectGenerated?.Invoke(clip);
                        
                        callback?.Invoke(clip, null);
                    }
                    else
                    {
                        // Créer un clip audio de test pour la démonstration
                        AudioClip testClip = AudioClip.Create("TestSoundEffect_" + description.GetHashCode(), 44100, 1, 44100, false);
                        LogInfo("Effet sonore de test créé (fonctionnalité beta)");
                        
                        // Notifier les abonnés
                        OnSoundEffectGenerated?.Invoke(testClip);
                        
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

        /// <summary>
        /// Joue un effet sonore prédéfini
        /// </summary>
        /// <param name="effectId">Identifiant de l'effet sonore</param>
        /// <param name="volume">Volume de lecture (0.0 à 1.0)</param>
        /// <returns>True si l'effet a été joué, False sinon</returns>
        public bool PlaySoundEffect(string effectId, float volume = -1)
        {
            if (!isInitialized)
            {
                LogError("Gestionnaire de génération vocale non initialisé");
                return false;
            }

            if (generatedClips.TryGetValue(effectId, out AudioClip clip))
            {
                if (clip != null)
                {
                    float actualVolume = volume >= 0 ? volume : defaultVolume;
                    effectsAudioSource.PlayOneShot(clip, actualVolume);
                    LogInfo($"Lecture de l'effet sonore: {effectId}");
                    return true;
                }
            }

            // Effet non trouvé, essayer de le générer à partir des présets
            SoundEffectPreset preset = soundEffectPresets.Find(p => p.id == effectId);
            if (preset != null)
            {
                LogInfo($"Effet sonore {effectId} non préchargé, génération à la demande...");
                GenerateSoundEffect(preset.description, (generatedClip, error) =>
                {
                    if (generatedClip != null)
                    {
                        generatedClips[effectId] = generatedClip;
                        float actualVolume = volume >= 0 ? volume : defaultVolume;
                        effectsAudioSource.PlayOneShot(generatedClip, actualVolume);
                        LogInfo($"Lecture de l'effet sonore généré à la demande: {effectId}");
                    }
                });
                return true;
            }

            LogError($"Effet sonore non trouvé: {effectId}");
            return false;
        }

        /// <summary>
        /// Joue un effet sonore généré à partir d'une description
        /// </summary>
        /// <param name="description">Description de l'effet sonore</param>
        /// <param name="volume">Volume de lecture (0.0 à 1.0)</param>
        public void PlaySoundEffectFromDescription(string description, float volume = -1)
        {
            if (!isInitialized)
            {
                LogError("Gestionnaire de génération vocale non initialisé");
                return;
            }

            GenerateSoundEffect(description, (clip, error) =>
            {
                if (clip != null)
                {
                    float actualVolume = volume >= 0 ? volume : defaultVolume;
                    effectsAudioSource.PlayOneShot(clip, actualVolume);
                    LogInfo($"Lecture de l'effet sonore généré: {description}");
                }
            });
        }

        /// <summary>
        /// Ajoute un effet sonore prédéfini
        /// </summary>
        /// <param name="id">Identifiant unique de l'effet</param>
        /// <param name="description">Description pour la génération</param>
        /// <returns>True si l'effet a été ajouté, False sinon</returns>
        public bool AddSoundEffectPreset(string id, string description)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(description))
            {
                LogError("ID et description ne peuvent pas être vides");
                return false;
            }

            // Vérifier si l'ID existe déjà
            if (soundEffectPresets.Exists(p => p.id == id))
            {
                LogError($"Un effet sonore avec l'ID {id} existe déjà");
                return false;
            }

            // Ajouter le preset
            SoundEffectPreset preset = new SoundEffectPreset
            {
                id = id,
                description = description
            };
            soundEffectPresets.Add(preset);

            // Générer l'effet sonore
            GenerateSoundEffect(description, (clip, error) =>
            {
                if (clip != null)
                {
                    generatedClips[id] = clip;
                    LogInfo($"Effet sonore ajouté et généré: {id}");
                }
            });

            return true;
        }

        /// <summary>
        /// Ouvre l'interface de création vocale d'Animate Anything World
        /// </summary>
        public void OpenVoiceCreator()
        {
            if (!isInitialized)
            {
                LogError("Gestionnaire de génération vocale non initialisé");
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
                Debug.Log($"[VoiceGenerationManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[VoiceGenerationManager] {message}");
            OnError?.Invoke(message);
        }

        #endregion
    }

    /// <summary>
    /// Classe représentant un preset d'effet sonore
    /// </summary>
    [System.Serializable]
    public class SoundEffectPreset
    {
        public string id;
        public string description;
    }
}
