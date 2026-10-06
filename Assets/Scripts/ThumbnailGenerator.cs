using System;
using UnityEngine;

namespace ModelViewerApp
{
    public class ThumbnailGenerator : MonoBehaviour
    {
        public static ThumbnailGenerator Instance { get; private set; }

        private Camera thumbCamera;
        private Light thumbLight;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                DestroySafe(gameObject);
                return;
            }

            SetupThumbnailCamera();
        }

        public void SetupThumbnailCamera()
        {
            Transform existingCam = transform.Find("ThumbnailCamera");
            GameObject camObj = existingCam != null ? existingCam.gameObject : null;
            if (camObj == null)
            {
                camObj = new GameObject("ThumbnailCamera");
                camObj.transform.SetParent(transform);
            }
            camObj.transform.position = new Vector3(0, 1000, 0);

            thumbCamera = camObj.GetComponent<Camera>();
            if (thumbCamera == null)
            {
                thumbCamera = camObj.AddComponent<Camera>();
            }
            thumbCamera.clearFlags = CameraClearFlags.SolidColor;
            thumbCamera.backgroundColor = new Color(0.08f, 0.12f, 0.18f, 1f);
            thumbCamera.fieldOfView = 35f;
            thumbCamera.nearClipPlane = 0.05f;
            thumbCamera.farClipPlane = 100f;
            thumbCamera.enabled = false;

            Transform existingLight = camObj.transform.Find("ThumbnailLight");
            GameObject lightObj = existingLight != null ? existingLight.gameObject : null;
            if (lightObj == null)
            {
                lightObj = new GameObject("ThumbnailLight");
                lightObj.transform.SetParent(camObj.transform);
            }
            lightObj.transform.localRotation = Quaternion.Euler(45, -30, 0);
            thumbLight = lightObj.GetComponent<Light>();
            if (thumbLight == null)
            {
                thumbLight = lightObj.AddComponent<Light>();
            }
            thumbLight.type = LightType.Directional;
            thumbLight.color = Color.white;
            thumbLight.intensity = 1.3f;
        }

        public Sprite GenerateThumbnail(GameObject modelPrefabOrInstance, int width = 256, int height = 256)
        {
            if (modelPrefabOrInstance == null) return null;

            if (thumbCamera == null)
            {
                SetupThumbnailCamera();
            }

            if (thumbCamera == null)
            {
                return UIProceduralTheme.GetRoundedRectSprite(width, height, 12, new Color(0.12f, 0.16f, 0.24f));
            }

            GameObject clone = null;
            RenderTexture rt = null;

            try
            {
                // Instantiate a clone temporarily at offscreen position
                Vector3 offscreenPos = new Vector3(0, 1000, 5);
                clone = Instantiate(modelPrefabOrInstance, offscreenPos, Quaternion.Euler(15, -35, 0));
                clone.SetActive(true);

                // Compute bounds
                Bounds bounds = new Bounds(offscreenPos, Vector3.zero);
                bool hasBounds = false;
                Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r is ParticleSystemRenderer) continue;
                    if (!hasBounds)
                    {
                        bounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }

                // Position camera to frame the clone
                float radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) * 1.5f;
                if (radius < 0.1f) radius = 1f;

                float fov = thumbCamera != null ? thumbCamera.fieldOfView : 35f;
                float distance = radius / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
                thumbCamera.transform.position = bounds.center + new Vector3(0, radius * 0.2f, -distance);
                thumbCamera.transform.LookAt(bounds.center);

                // Render to texture
                rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                thumbCamera.targetTexture = rt;
                thumbCamera.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                thumbCamera.targetTexture = null;
                RenderTexture.active = null;

                // Create sprite
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                return sprite;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ThumbnailGenerator] Failed to generate thumbnail: " + ex.Message);
                return UIProceduralTheme.GetRoundedRectSprite(width, height, 12, new Color(0.12f, 0.16f, 0.24f));
            }
            finally
            {
                if (rt != null)
                {
                    RenderTexture.ReleaseTemporary(rt);
                }

                if (clone != null)
                {
                    DestroySafe(clone);
                }
            }
        }

        private void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }
    }
}
