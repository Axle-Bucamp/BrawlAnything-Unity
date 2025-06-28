using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrawlAnything.Core
{
    /// <summary>
    /// Système d'événements pour la communication entre les différents modules du jeu
    /// </summary>
    public class EventSystem : MonoBehaviour
    {
        // Singleton instance
        private static EventSystem _instance;
        public static EventSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EventSystem");
                        _instance = go.AddComponent<EventSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Dictionary to store event callbacks
        private Dictionary<string, List<Action<object>>> eventCallbacks = new Dictionary<string, List<Action<object>>>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// S'abonne à un événement
        /// </summary>
        /// <param name="eventName">Nom de l'événement</param>
        /// <param name="callback">Fonction de rappel à appeler lorsque l'événement est déclenché</param>
        public void Subscribe(string eventName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("Event name cannot be null or empty");
                return;
            }

            if (callback == null)
            {
                Debug.LogError("Callback cannot be null");
                return;
            }

            if (!eventCallbacks.ContainsKey(eventName))
            {
                eventCallbacks[eventName] = new List<Action<object>>();
            }

            eventCallbacks[eventName].Add(callback);
        }

        /// <summary>
        /// Se désabonne d'un événement
        /// </summary>
        /// <param name="eventName">Nom de l'événement</param>
        /// <param name="callback">Fonction de rappel à désabonner</param>
        public void Unsubscribe(string eventName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null)
            {
                return;
            }

            if (eventCallbacks.ContainsKey(eventName))
            {
                eventCallbacks[eventName].Remove(callback);

                // Remove the event from the dictionary if there are no more callbacks
                if (eventCallbacks[eventName].Count == 0)
                {
                    eventCallbacks.Remove(eventName);
                }
            }
        }

        /// <summary>
        /// Déclenche un événement
        /// </summary>
        /// <param name="eventName">Nom de l'événement</param>
        /// <param name="data">Données à passer aux abonnés</param>
        public void TriggerEvent(string eventName, object data = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("Event name cannot be null or empty");
                return;
            }

            if (eventCallbacks.ContainsKey(eventName))
            {
                // Create a copy of the callbacks list to avoid issues if callbacks modify the list
                List<Action<object>> callbacksCopy = new List<Action<object>>(eventCallbacks[eventName]);

                foreach (var callback in callbacksCopy)
                {
                    try
                    {
                        callback(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in event callback for event '{eventName}': {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Vérifie si un événement a des abonnés
        /// </summary>
        /// <param name="eventName">Nom de l'événement</param>
        /// <returns>True si l'événement a des abonnés, false sinon</returns>
        public bool HasSubscribers(string eventName)
        {
            return !string.IsNullOrEmpty(eventName) && 
                   eventCallbacks.ContainsKey(eventName) && 
                   eventCallbacks[eventName].Count > 0;
        }

        /// <summary>
        /// Supprime tous les abonnements pour un événement spécifique
        /// </summary>
        /// <param name="eventName">Nom de l'événement</param>
        public void ClearEvent(string eventName)
        {
            if (!string.IsNullOrEmpty(eventName) && eventCallbacks.ContainsKey(eventName))
            {
                eventCallbacks.Remove(eventName);
            }
        }

        /// <summary>
        /// Supprime tous les abonnements à tous les événements
        /// </summary>
        public void ClearAllEvents()
        {
            eventCallbacks.Clear();
        }
    }
}
