using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.IO;

namespace BrawlAnything.Camera
{
    /// <summary>
    /// Handles image processing operations like cropping, filtering, and masking
    /// </summary>
    public class ImageProcessor : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage imageDisplay;
        [SerializeField] private RectTransform cropFrame;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Slider contrastSlider;
        [SerializeField] private Slider saturationSlider;
        [SerializeField] private Toggle maskModeToggle;
        [SerializeField] private Image brushCursor;
        [SerializeField] private Slider brushSizeSlider;

        [Header("Processing Settings")]
        [SerializeField] private Material imageFxMaterial;
        [SerializeField] private Material maskingMaterial;
        [SerializeField] private Color maskColor = Color.red;
        [SerializeField] private float defaultBrushSize = 50f;

        // Private variables
        private Texture2D sourceTexture;
        private Texture2D processedTexture;
        private Texture2D maskTexture;
        private bool isMaskMode = false;
        private Vector2 lastTouchPosition;
        private float currentBrushSize;
        private RenderTexture renderTexture;

        // Events
        public event Action<Texture2D> OnImageProcessed;
        public event Action<Texture2D> OnMaskUpdated;

        private void Start()
        {
            // Initialize UI controls
            if (brightnessSlider != null)
                brightnessSlider.onValueChanged.AddListener(UpdateImageEffects);
            
            if (contrastSlider != null)
                contrastSlider.onValueChanged.AddListener(UpdateImageEffects);
            
            if (saturationSlider != null)
                saturationSlider.onValueChanged.AddListener(UpdateImageEffects);
            
            if (maskModeToggle != null)
                maskModeToggle.onValueChanged.AddListener(SetMaskMode);
            
            if (brushSizeSlider != null)
            {
                brushSizeSlider.onValueChanged.AddListener(UpdateBrushSize);
                currentBrushSize = defaultBrushSize;
                brushSizeSlider.value = defaultBrushSize;
            }

            // Hide brush cursor initially
            if (brushCursor != null)
                brushCursor.gameObject.SetActive(false);
        }

        public void LoadImage(string imagePath)
        {
            StartCoroutine(LoadImageCoroutine(imagePath));
        }

        private IEnumerator LoadImageCoroutine(string imagePath)
        {
            // Check if file exists
            if (!File.Exists(imagePath))
            {
                Debug.LogError("Image file not found: " + imagePath);
                yield break;
            }

            // Load image data
            byte[] imageData = File.ReadAllBytes(imagePath);
            sourceTexture = new Texture2D(2, 2);
            sourceTexture.LoadImage(imageData);

            // Create processed texture
            processedTexture = new Texture2D(sourceTexture.width, sourceTexture.height);
            Graphics.CopyTexture(sourceTexture, processedTexture);

            // Create mask texture (transparent by default)
            maskTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[sourceTexture.width * sourceTexture.height];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = new Color(0, 0, 0, 0);
            maskTexture.SetPixels(colors);
            maskTexture.Apply();

            // Create render texture for processing
            renderTexture = new RenderTexture(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            // Display the image
            if (imageDisplay != null)
            {
                imageDisplay.texture = processedTexture;
                
                // Adjust aspect ratio
                float aspectRatio = (float)processedTexture.width / (float)processedTexture.height;
                imageDisplay.GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
            }

            // Apply initial effects
            UpdateImageEffects(0);
        }

        private void UpdateImageEffects(float unused)
        {
            if (sourceTexture == null || imageFxMaterial == null)
                return;

            // Set shader parameters
            imageFxMaterial.SetFloat("_Brightness", brightnessSlider != null ? brightnessSlider.value : 0);
            imageFxMaterial.SetFloat("_Contrast", contrastSlider != null ? contrastSlider.value : 0);
            imageFxMaterial.SetFloat("_Saturation", saturationSlider != null ? saturationSlider.value : 1);

            // Apply effects using the shader
            Graphics.Blit(sourceTexture, renderTexture, imageFxMaterial);
            
            // Copy result back to processed texture
            RenderTexture.active = renderTexture;
            processedTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            processedTexture.Apply();
            RenderTexture.active = null;

            // Update display
            if (imageDisplay != null)
                imageDisplay.texture = processedTexture;

            // Notify listeners
            if (OnImageProcessed != null)
                OnImageProcessed.Invoke(processedTexture);
        }

        private void SetMaskMode(bool isOn)
        {
            isMaskMode = isOn;
            
            // Show/hide brush cursor
            if (brushCursor != null)
                brushCursor.gameObject.SetActive(isOn);
            
            // Show/hide brush size slider
            if (brushSizeSlider != null)
                brushSizeSlider.gameObject.SetActive(isOn);
        }

        private void UpdateBrushSize(float size)
        {
            currentBrushSize = size;
            
            // Update brush cursor size
            if (brushCursor != null)
            {
                RectTransform rt = brushCursor.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(size, size);
            }
        }

        private void Update()
        {
            // Handle masking input
            if (isMaskMode && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                // Convert touch position to canvas space
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    imageDisplay.rectTransform,
                    touch.position,
                    null,
                    out Vector2 localPoint);
                
                // Update brush cursor position
                if (brushCursor != null)
                {
                    brushCursor.rectTransform.localPosition = localPoint;
                }
                
                // Draw mask when touching
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began)
                {
                    // Convert local point to texture coordinates
                    Vector2 texCoord = new Vector2(
                        (localPoint.x + imageDisplay.rectTransform.rect.width / 2) / imageDisplay.rectTransform.rect.width,
                        (localPoint.y + imageDisplay.rectTransform.rect.height / 2) / imageDisplay.rectTransform.rect.height
                    );
                    
                    // Draw on mask texture
                    DrawOnMask(texCoord, currentBrushSize / imageDisplay.rectTransform.rect.width);
                    
                    // Save last position for interpolation
                    lastTouchPosition = texCoord;
                }
            }
            
            // Handle mouse input for editor testing
            #if UNITY_EDITOR
            if (isMaskMode && Input.GetMouseButton(0))
            {
                // Convert mouse position to canvas space
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    imageDisplay.rectTransform,
                    Input.mousePosition,
                    null,
                    out Vector2 localPoint);
                
                // Update brush cursor position
                if (brushCursor != null)
                {
                    brushCursor.rectTransform.localPosition = localPoint;
                }
                
                // Convert local point to texture coordinates
                Vector2 texCoord = new Vector2(
                    (localPoint.x + imageDisplay.rectTransform.rect.width / 2) / imageDisplay.rectTransform.rect.width,
                    (localPoint.y + imageDisplay.rectTransform.rect.height / 2) / imageDisplay.rectTransform.rect.height
                );
                
                // Draw on mask texture
                DrawOnMask(texCoord, currentBrushSize / imageDisplay.rectTransform.rect.width);
                
                // Save last position for interpolation
                lastTouchPosition = texCoord;
            }
            #endif
        }

        private void DrawOnMask(Vector2 texCoord, float brushSizeNormalized)
        {
            if (maskTexture == null)
                return;
            
            // Calculate pixel coordinates
            int x = Mathf.RoundToInt(texCoord.x * maskTexture.width);
            int y = Mathf.RoundToInt(texCoord.y * maskTexture.height);
            int radius = Mathf.RoundToInt(brushSizeNormalized * maskTexture.width / 2);
            
            // Draw circle on mask texture
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (i * i + j * j <= radius * radius)
                    {
                        int pixelX = x + i;
                        int pixelY = y + j;
                        
                        // Check bounds
                        if (pixelX >= 0 && pixelX < maskTexture.width && pixelY >= 0 && pixelY < maskTexture.height)
                        {
                            maskTexture.SetPixel(pixelX, pixelY, maskColor);
                        }
                    }
                }
            }
            
            // Apply changes
            maskTexture.Apply();
            
            // Notify listeners
            if (OnMaskUpdated != null)
                OnMaskUpdated.Invoke(maskTexture);
        }

        public void ClearMask()
        {
            if (maskTexture == null)
                return;
            
            // Reset mask to transparent
            Color[] colors = new Color[maskTexture.width * maskTexture.height];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = new Color(0, 0, 0, 0);
            maskTexture.SetPixels(colors);
            maskTexture.Apply();
            
            // Notify listeners
            if (OnMaskUpdated != null)
                OnMaskUpdated.Invoke(maskTexture);
        }

        public Texture2D GetProcessedImage()
        {
            return processedTexture;
        }

        public Texture2D GetMaskTexture()
        {
            return maskTexture;
        }

        public Texture2D GetCroppedImage()
        {
            if (processedTexture == null || cropFrame == null)
                return processedTexture;
            
            // Calculate crop rect in texture space
            Rect cropRect = GetNormalizedCropRect();
            
            // Create cropped texture
            int width = Mathf.RoundToInt(cropRect.width * processedTexture.width);
            int height = Mathf.RoundToInt(cropRect.height * processedTexture.height);
            Texture2D croppedTexture = new Texture2D(width, height);
            
            // Copy pixels from the processed texture
            Color[] pixels = processedTexture.GetPixels(
                Mathf.RoundToInt(cropRect.x * processedTexture.width),
                Mathf.RoundToInt(cropRect.y * processedTexture.height),
                width,
                height
            );
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            
            return croppedTexture;
        }

        private Rect GetNormalizedCropRect()
        {
            if (cropFrame == null || imageDisplay == null)
                return new Rect(0, 0, 1, 1);
            
            // Get crop frame rect in canvas space
            Rect cropFrameRect = cropFrame.rect;
            Vector2 cropFramePos = cropFrame.localPosition;
            
            // Get image display rect in canvas space
            Rect imageRect = imageDisplay.rectTransform.rect;
            Vector2 imagePos = imageDisplay.rectTransform.localPosition;
            
            // Calculate crop rect relative to image
            float x = (cropFramePos.x - cropFrameRect.width / 2 - imagePos.x + imageRect.width / 2) / imageRect.width;
            float y = (cropFramePos.y - cropFrameRect.height / 2 - imagePos.y + imageRect.height / 2) / imageRect.height;
            float width = cropFrameRect.width / imageRect.width;
            float height = cropFrameRect.height / imageRect.height;
            
            // Clamp to image bounds
            x = Mathf.Clamp(x, 0, 1 - width);
            y = Mathf.Clamp(y, 0, 1 - height);
            
            return new Rect(x, y, width, height);
        }

        private void OnDestroy()
        {
            // Clean up
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }
}
