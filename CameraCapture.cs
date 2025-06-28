using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using System;

namespace BrawlAnything.Camera
{
    /// <summary>
    /// Manages camera capture functionality for creating character models
    /// </summary>
    public class CameraCapture : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private bool useFrontCamera = true;
        [SerializeField] private int requestedWidth = 1280;
        [SerializeField] private int requestedHeight = 720;
        [SerializeField] private int frameRate = 30;

        [Header("UI References")]
        [SerializeField] private RawImage previewDisplay;
        [SerializeField] private Button captureButton;
        [SerializeField] private Button switchCameraButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject captureGuideOverlay;
        [SerializeField] private GameObject permissionDeniedPanel;

        [Header("Capture Settings")]
        [SerializeField] private string captureFolder = "Captures";
        [SerializeField] private float captureDelay = 0.5f;

        // Private variables
        private WebCamTexture webCamTexture;
        private bool isCameraInitialized = false;
        private bool isCapturing = false;
        private string lastCapturedImagePath;

        // Events
        public event Action<string> OnImageCaptured;
        public event Action OnCameraInitialized;
        public event Action OnCameraFailed;

        private void Start()
        {
            // Initialize UI elements
            if (captureButton != null)
                captureButton.onClick.AddListener(CaptureImage);
            
            if (switchCameraButton != null)
                switchCameraButton.onClick.AddListener(SwitchCamera);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);

            // Hide loading indicator initially
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            // Hide permission denied panel initially
            if (permissionDeniedPanel != null)
                permissionDeniedPanel.SetActive(false);

            // Initialize camera
            StartCoroutine(InitializeCamera());
        }

        private IEnumerator InitializeCamera()
        {
            // Request camera permission
            yield return RequestCameraPermission();

            // Check if we have permission
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.LogError("Camera permission denied");
                if (permissionDeniedPanel != null)
                    permissionDeniedPanel.SetActive(true);
                
                if (OnCameraFailed != null)
                    OnCameraFailed.Invoke();
                
                yield break;
            }

            // Get available devices
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("No camera devices found");
                if (OnCameraFailed != null)
                    OnCameraFailed.Invoke();
                
                yield break;
            }

            // Find appropriate camera (front or back)
            WebCamDevice selectedDevice = devices[0];
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].isFrontFacing == useFrontCamera)
                {
                    selectedDevice = devices[i];
                    break;
                }
            }

            // Create WebCamTexture
            webCamTexture = new WebCamTexture(selectedDevice.name, requestedWidth, requestedHeight, frameRate);
            
            // Start the camera
            webCamTexture.Play();

            // Wait for the camera to initialize
            yield return new WaitUntil(() => webCamTexture.width > 100);

            // Set the texture to the preview display
            if (previewDisplay != null)
            {
                previewDisplay.texture = webCamTexture;
                
                // Adjust aspect ratio
                float aspectRatio = (float)webCamTexture.width / (float)webCamTexture.height;
                previewDisplay.GetComponent<AspectRatioFitter>().aspectRatio = aspectRatio;
            }

            isCameraInitialized = true;
            
            if (OnCameraInitialized != null)
                OnCameraInitialized.Invoke();
            
            Debug.Log("Camera initialized: " + selectedDevice.name);
        }

        private IEnumerator RequestCameraPermission()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        public void CaptureImage()
        {
            if (!isCameraInitialized || isCapturing)
                return;

            StartCoroutine(CaptureImageCoroutine());
        }

        private IEnumerator CaptureImageCoroutine()
        {
            isCapturing = true;
            
            // Show loading indicator
            if (loadingIndicator != null)
                loadingIndicator.SetActive(true);
            
            // Hide capture guide
            if (captureGuideOverlay != null)
                captureGuideOverlay.SetActive(false);
            
            // Wait for a frame to ensure UI updates
            yield return null;
            
            // Add a small delay to allow user to prepare
            yield return new WaitForSeconds(captureDelay);
            
            // Capture the image
            Texture2D snapshot = new Texture2D(webCamTexture.width, webCamTexture.height);
            snapshot.SetPixels(webCamTexture.GetPixels());
            snapshot.Apply();
            
            // Convert to PNG
            byte[] bytes = snapshot.EncodeToPNG();
            
            // Create directory if it doesn't exist
            string directory = Path.Combine(Application.persistentDataPath, captureFolder);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            // Save the image
            string filename = "capture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            lastCapturedImagePath = Path.Combine(directory, filename);
            File.WriteAllBytes(lastCapturedImagePath, bytes);
            
            Debug.Log("Image captured: " + lastCapturedImagePath);
            
            // Hide loading indicator
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
            
            // Notify listeners
            if (OnImageCaptured != null)
                OnImageCaptured.Invoke(lastCapturedImagePath);
            
            isCapturing = false;
        }

        public void SwitchCamera()
        {
            if (isCapturing)
                return;
            
            // Toggle front/back camera
            useFrontCamera = !useFrontCamera;
            
            // Stop current camera
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
            }
            
            // Reinitialize camera
            isCameraInitialized = false;
            StartCoroutine(InitializeCamera());
        }

        public void Cancel()
        {
            // Stop camera
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
            }
            
            // Return to previous screen (implementation depends on navigation system)
            // For example:
            // SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            // Clean up
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
            }
        }

        // Public method to get the last captured image path
        public string GetLastCapturedImagePath()
        {
            return lastCapturedImagePath;
        }
    }
}
