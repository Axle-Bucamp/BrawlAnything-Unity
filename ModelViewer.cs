using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.IO;

namespace BrawlAnything.Models
{
    /// <summary>
    /// Handles loading, displaying, and manipulating 3D models.
    /// </summary>
    public class ModelViewer : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private Transform modelContainer;
        [SerializeField] private Light mainLight;
        [SerializeField] private UnityEngine.Camera viewerCamera;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float minZoomDistance = 0.5f;
        [SerializeField] private float maxZoomDistance = 5f;
        [SerializeField] private Vector2 panSpeed = new Vector2(0.01f, 0.01f);

        [Header("UI References")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject modelControls;
        [SerializeField] private GameObject errorMessage;

        // Events
        public event Action<GameObject> OnModelLoaded;
        public event Action<int> OnVariationChanged;
        public event Action OnLoadFailed;

        // Internal State
        private GameObject currentModel;
        private List<GameObject> modelVariations = new();
        private int currentVariationIndex = 0;

        private Vector3 initialModelPosition;
        private Quaternion initialModelRotation;
        private Vector3 initialModelScale;

        private void Start()
        {
            loadingIndicator?.SetActive(false);
            modelControls?.SetActive(false);
            errorMessage?.SetActive(false);
        }

        public void LoadModel(string modelPath) => StartCoroutine(LoadModelCoroutine(modelPath));
        public void LoadModelVariations(List<string> modelPaths) => StartCoroutine(LoadModelVariationsCoroutine(modelPaths));

        private IEnumerator LoadModelCoroutine(string modelPath)
        {
            loadingIndicator?.SetActive(true);
            modelControls?.SetActive(false);
            errorMessage?.SetActive(false);
            ClearCurrentModel();

            UnityWebRequest request = null;
            AssetBundle bundle = null;

            try
            {
                GameObject modelPrefab = null;

                if (modelPath.StartsWith("http"))
                {
                    request = UnityWebRequestAssetBundle.GetAssetBundle(modelPath);
                    request.SendWebRequest(); // yield return ...

                    if (request.result != UnityWebRequest.Result.Success)
                        throw new Exception($"Failed to load model: {request.error}");

                    bundle = DownloadHandlerAssetBundle.GetContent(request);
                    if (bundle == null || bundle.GetAllAssetNames().Length == 0)
                        throw new Exception("Asset bundle is empty or invalid");

                    modelPrefab = bundle.LoadAsset<GameObject>(bundle.GetAllAssetNames()[0]);
                }
                else if (modelPath.StartsWith("/") || modelPath.Contains(":/"))
                {
                    if (!File.Exists(modelPath))
                        throw new FileNotFoundException("Model file not found");

                    Debug.LogWarning("GLTF loading requires an external importer. Using placeholder.");
                    modelPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                }
                else
                {
                    modelPrefab = Resources.Load<GameObject>(modelPath);
                    if (modelPrefab == null)
                        throw new Exception("Failed to load model from Resources.");
                }

                currentModel = Instantiate(modelPrefab, modelContainer);
                CenterAndScaleModel(currentModel);

                initialModelPosition = currentModel.transform.localPosition;
                initialModelRotation = currentModel.transform.localRotation;
                initialModelScale = currentModel.transform.localScale;

                modelControls?.SetActive(true);
                OnModelLoaded?.Invoke(currentModel);

                Debug.Log($"Model loaded: {modelPath}");
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Model loading error: {ex.Message}");
                errorMessage?.SetActive(true);
                OnLoadFailed?.Invoke();
            }
            finally
            {
                loadingIndicator?.SetActive(false);
                bundle?.Unload(false);
                request?.Dispose();
            }
            yield return request.result;
        }


        private IEnumerator LoadModelVariationsCoroutine(List<string> modelPaths)
        {
            loadingIndicator?.SetActive(true);
            modelControls?.SetActive(false);
            errorMessage?.SetActive(false);
            ClearAllModels();

            bool success = false;

            foreach (var path in modelPaths)
            {
                GameObject variation = null;

                try
                {
                    if (path.StartsWith("http") || path.StartsWith("/") || path.Contains(":/"))
                    {
                        variation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    }
                    else
                    {
                        GameObject prefab = Resources.Load<GameObject>(path);
                        if (prefab == null) continue;

                        variation = Instantiate(prefab);
                    }

                    if (variation != null)
                    {
                        variation.transform.SetParent(modelContainer, false);
                        CenterAndScaleModel(variation);
                        variation.SetActive(modelVariations.Count == 0);
                        modelVariations.Add(variation);
                        success = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading variation: {e.Message}");
                }

                yield return null;
            }

            if (success && modelVariations.Count > 0)
            {
                currentModel = modelVariations[0];
                currentVariationIndex = 0;

                initialModelPosition = currentModel.transform.localPosition;
                initialModelRotation = currentModel.transform.localRotation;
                initialModelScale = currentModel.transform.localScale;

                modelControls?.SetActive(true);
                OnModelLoaded?.Invoke(currentModel);
                OnVariationChanged?.Invoke(currentVariationIndex);

                Debug.Log($"Loaded {modelVariations.Count} variations.");
            }
            else
            {
                errorMessage?.SetActive(true);
                OnLoadFailed?.Invoke();
                Debug.LogError("No model variations could be loaded.");
            }

            loadingIndicator?.SetActive(false);
        }

        private void CenterAndScaleModel(GameObject model)
        {
            if (!model) return;

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            var bounds = CalculateBounds(model);
            model.transform.localPosition = -bounds.center;

            float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDim > 0)
            {
                float scale = 1f / maxDim;
                model.transform.localScale = Vector3.one * scale;
            }
        }

        private Bounds CalculateBounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            bounds.center = model.transform.InverseTransformPoint(bounds.center);
            return bounds;
        }

        private void ClearCurrentModel()
        {
            if (currentModel != null)
            {
                Destroy(currentModel);
                currentModel = null;
            }
        }

        private void ClearAllModels()
        {
            ClearCurrentModel();

            foreach (var variation in modelVariations)
            {
                if (variation != null)
                    Destroy(variation);
            }

            modelVariations.Clear();
            currentVariationIndex = 0;
        }

        public void NextVariation()
        {
            if (modelVariations.Count < 2) return;

            modelVariations[currentVariationIndex].SetActive(false);
            currentVariationIndex = (currentVariationIndex + 1) % modelVariations.Count;
            modelVariations[currentVariationIndex].SetActive(true);
            currentModel = modelVariations[currentVariationIndex];

            UpdateInitialTransform();
            OnVariationChanged?.Invoke(currentVariationIndex);
        }

        public void PreviousVariation()
        {
            if (modelVariations.Count < 2) return;

            modelVariations[currentVariationIndex].SetActive(false);
            currentVariationIndex = (currentVariationIndex - 1 + modelVariations.Count) % modelVariations.Count;
            modelVariations[currentVariationIndex].SetActive(true);
            currentModel = modelVariations[currentVariationIndex];

            UpdateInitialTransform();
            OnVariationChanged?.Invoke(currentVariationIndex);
        }

        public void ResetModelTransform()
        {
            if (!currentModel) return;

            currentModel.transform.localPosition = initialModelPosition;
            currentModel.transform.localRotation = initialModelRotation;
            currentModel.transform.localScale = initialModelScale;
        }

        private void UpdateInitialTransform()
        {
            if (currentModel == null) return;

            initialModelPosition = currentModel.transform.localPosition;
            initialModelRotation = currentModel.transform.localRotation;
            initialModelScale = currentModel.transform.localScale;
        }
    }
}
