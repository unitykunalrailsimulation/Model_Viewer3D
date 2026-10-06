using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using ModelViewerApp;

namespace ModelViewerApp.Android
{
    public class AndroidIntentHandler : MonoBehaviour
    {
        private static AndroidIntentHandler _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ProcessIntent();
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (hasFocus)
            {
                ProcessIntent();
            }
#endif
        }

        private void ProcessIntent()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            if (intent != null)
                            {
                                string action = intent.Call<string>("getAction");
                                if (action == "android.intent.action.VIEW")
                                {
                                    using (AndroidJavaObject uri = intent.Call<AndroidJavaObject>("getData"))
                                    {
                                        if (uri != null)
                                        {
                                            string uriString = uri.Call<string>("toString");
                                            
                                            // Clear intent to prevent reloading on every focus
                                            intent.Call<AndroidJavaObject>("setData", (AndroidJavaObject)null);
                                            
                                            StartCoroutine(CopyAndLoadContentUri(uriString));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AndroidIntentHandler] Error processing intent: " + e.Message);
            }
#endif
        }

        private IEnumerator CopyAndLoadContentUri(string uriString)
        {
            if (ModelViewerUI.Instance != null && ModelViewerUI.Instance.uploadProgressModal != null)
            {
                ModelViewerUI.Instance.uploadProgressModal.SetActive(true);
                if (ModelViewerUI.Instance.uploadStatusText != null) 
                    ModelViewerUI.Instance.uploadStatusText.text = "Importing file from device...";
                if (ModelViewerUI.Instance.uploadProgressBar != null) 
                    ModelViewerUI.Instance.uploadProgressBar.fillAmount = 0.2f;
            }

            string ext = ".fbx";
            // Check if there is an extension in the URI (sometimes present)
            if (uriString.ToLower().Contains(".obj")) ext = ".obj";
            else if (uriString.ToLower().Contains(".gltf")) ext = ".gltf";
            else if (uriString.ToLower().Contains(".glb")) ext = ".glb";

            string localPath = Path.Combine(Application.persistentDataPath, "imported_model" + ext);

            using (UnityWebRequest uwr = UnityWebRequest.Get(uriString))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(localPath, uwr.downloadHandler.data);
                    
                    if (ModelViewerUI.Instance != null && ModelViewerUI.Instance.uploadProgressBar != null) 
                        ModelViewerUI.Instance.uploadProgressBar.fillAmount = 1.0f;
                        
                    yield return new WaitForSeconds(0.2f);
                    
                    if (ModelViewerUI.Instance != null && ModelViewerUI.Instance.uploadProgressModal != null)
                        ModelViewerUI.Instance.uploadProgressModal.SetActive(false);

                    if (ModelDataManager.Instance != null)
                    {
                        ModelDataManager.Instance.UploadModelFromFile(localPath);
                    }
                }
                else
                {
                    Debug.LogError("[AndroidIntentHandler] Failed to read content URI: " + uwr.error);
                    if (ModelViewerUI.Instance != null && ModelViewerUI.Instance.uploadProgressModal != null)
                        ModelViewerUI.Instance.uploadProgressModal.SetActive(false);
                }
            }
        }
    }
}
