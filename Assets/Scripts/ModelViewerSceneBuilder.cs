using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ModelViewerApp
{
    [ExecuteAlways]
    public class ModelViewerSceneBuilder : MonoBehaviour
    {
        public bool autoBuildOnStart = true;

        private void Awake()
        {
            BuildSceneIfEmpty();
        }

        private void Start()
        {
            if (autoBuildOnStart)
            {
                BuildSceneIfEmpty();
            }
        }

        public static void BuildSceneIfEmpty()
        {
            if (FindFirstObjectByType<ModelViewerUI>() != null)
            {
                return;
            }

            BuildFullScene();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Setup Model Viewer 2.0 Scene")]
        public static void BuildSceneMenu()
        {
            BuildFullScene();
        }

        [MenuItem("Tools/Model Viewer/Clean & Regenerate Scene", false, 0)]
        public static void CleanAndRegenerateMenu()
        {
            BuildFullScene();
        }
#endif

        public static void CleanUpPreviousBuild()
        {
            Debug.Log("[ModelViewerSceneBuilder] Cleaning up all previous scene objects...");

            // 1. Destroy any existing canvas or UI objects
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c != null && (c.gameObject.name.Contains("Canvas") || c.GetComponent<ModelViewerUI>() != null))
                {
                    DestroyImmediate(c.gameObject);
                }
            }

            // 2. Destroy models container and orphan models
            GameObject modelsRoot = GameObject.Find("ModelsContainer");
            if (modelsRoot != null) DestroyImmediate(modelsRoot);

            // 3. Destroy floor grid
            GameObject floorGrid = GameObject.Find("FloorGrid");
            if (floorGrid != null) DestroyImmediate(floorGrid);

            // 4. Destroy runtime gizmos
            GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjs)
            {
                if (go != null && (go.name.Contains("GizmoRenderer") || go.name.Contains("FloorGrid") || go.name.Contains("ModelsContainer") || go.name.Contains("ModelViewerCanvas")))
                {
                    DestroyImmediate(go);
                }
            }

            // 5. Clean up duplicate fill/rim lights
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                // Remove the strict deletion of fill/rim lights, we'll reuse them
                if (l != null && (l.gameObject.name.Contains("Clone") || l.gameObject.name.Contains("(Clone)")))
                {
                    DestroyImmediate(l.gameObject);
                }
            }

            // 6. Clean up thumbnail cameras
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                for (int i = mainCam.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = mainCam.transform.GetChild(i);
                    if (child.name.Contains("Thumbnail"))
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }

            // 7. Reset ModelDataManager
            ModelDataManager dm = FindFirstObjectByType<ModelDataManager>();
            if (dm != null)
            {
                dm.models.Clear();
                dm.currentModelIndex = 0;
            }
        }

        public static void BuildFullScene()
        {
            Debug.Log("[ModelViewerSceneBuilder] Starting clean build for Model Viewer 2.0...");

            // 0. Clean up ALL previous objects completely
            CleanUpPreviousBuild();

            // 1. Environment & Lighting
            SetupEnvironment();

            // 2. Camera Rig & Managers
            Camera mainCam = SetupCameraRig();

            // 3. 3D Ground Grid
            SetupFloorGrid();

            // 4. Model Data & 3D Models
            SetupModels();

            // 5. Full UI Hierarchy
            SetupCanvasUI(mainCam);

            // 6. Connect SceneGizmo if available
            SetupGizmo();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[ModelViewerSceneBuilder] Scene clean build complete and saved!");
            }
#endif
        }

        private static void SetupEnvironment()
        {
            // Key Light
            Light keyLight = null;
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && l.gameObject.name.Contains("Key"))
                {
                    keyLight = l;
                    break;
                }
            }

            if (keyLight == null)
            {
                GameObject lightGo = new GameObject("Directional Key Light");
                keyLight = lightGo.AddComponent<Light>();
                keyLight.type = LightType.Directional;
            }

            keyLight.transform.rotation = Quaternion.Euler(45, -30, 0);
            keyLight.color = new Color(1f, 0.98f, 0.95f);
            keyLight.intensity = 1.35f;
            keyLight.shadows = LightShadows.Soft;

            // Fill Light
            Light fillLight = null;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && l.gameObject.name.Contains("Fill Light"))
                {
                    fillLight = l;
                    break;
                }
            }
            if (fillLight == null)
            {
                GameObject fillLightGo = new GameObject("Directional Fill Light");
                fillLight = fillLightGo.AddComponent<Light>();
                fillLight.type = LightType.Directional;
            }
            fillLight.transform.rotation = Quaternion.Euler(20, 140, 0);
            fillLight.color = new Color(0.72f, 0.84f, 1f);
            fillLight.intensity = 0.65f;
            fillLight.shadows = LightShadows.None;

            // Rim / Back Light
            Light rimLight = null;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && l.gameObject.name.Contains("Rim Light"))
                {
                    rimLight = l;
                    break;
                }
            }
            if (rimLight == null)
            {
                GameObject rimLightGo = new GameObject("Directional Rim Light");
                rimLight = rimLightGo.AddComponent<Light>();
                rimLight.type = LightType.Directional;
            }
            rimLight.transform.rotation = Quaternion.Euler(-25, 35, 0);
            rimLight.color = new Color(0.6f, 0.75f, 0.95f);
            rimLight.intensity = 0.45f;
            rimLight.shadows = LightShadows.None;
        }

        private static Camera SetupCameraRig()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.059f, 0.094f, 1f); // #0b0f18 deep dark slate
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1000f;

            // Pivot at center of the 2.10m cabinet
            Vector3 pivot = new Vector3(0, 1.05f, 0);
            float dist = 2.85f;
            float yaw = 25f;
            float pitch = 12f;

            float yawRad = yaw * Mathf.Deg2Rad;
            float pitchRad = pitch * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                dist * Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                dist * Mathf.Sin(pitchRad),
                -dist * Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            );
            cam.transform.position = pivot + offset;
            cam.transform.LookAt(pivot);

            // Add Managers
            ModelViewerCamera camRig = cam.GetComponent<ModelViewerCamera>();
            if (camRig == null) camRig = cam.gameObject.AddComponent<ModelViewerCamera>();
            camRig.currentPivot = pivot;
            camRig.defaultPivot = pivot;
            camRig.distance = dist;
            camRig.defaultDistance = dist;
            camRig.yaw = yaw;
            camRig.defaultYaw = yaw;
            camRig.pitch = pitch;
            camRig.defaultPitch = pitch;

            if (cam.GetComponent<ModelSelectionManager>() == null)
                cam.gameObject.AddComponent<ModelSelectionManager>();

            if (cam.GetComponent<ModelMaterialController>() == null)
                cam.gameObject.AddComponent<ModelMaterialController>();

            ThumbnailGenerator thumbGen = cam.GetComponent<ThumbnailGenerator>();
            if (thumbGen == null)
                thumbGen = cam.gameObject.AddComponent<ThumbnailGenerator>();
            thumbGen.SetupThumbnailCamera();

            if (cam.GetComponent<ModelDataManager>() == null)
                cam.gameObject.AddComponent<ModelDataManager>();
                
            if (cam.GetComponent<ModelViewerApp.Android.AndroidIntentHandler>() == null)
                cam.gameObject.AddComponent<ModelViewerApp.Android.AndroidIntentHandler>();

            return cam;
        }

        private static void SetupFloorGrid()
        {
            GameObject grid = GameObject.Find("FloorGrid");
            if (grid == null)
            {
                grid = GameObject.CreatePrimitive(PrimitiveType.Plane);
                grid.name = "FloorGrid";
                DestroyImmediate(grid.GetComponent<Collider>());
            }

            grid.transform.position = new Vector3(0, -0.005f, 0);
            grid.transform.localScale = new Vector3(1.2f, 1f, 1.2f); // 12x12m subtle plane

            Texture2D gridTex = CreateGridTexture();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            Material gridMat = new Material(unlit);
            gridMat.name = "FloorGrid_Mat";
            gridMat.mainTexture = gridTex;
            gridMat.mainTextureScale = new Vector2(16, 16);

            grid.GetComponent<Renderer>().sharedMaterial = gridMat;
        }

        private static Texture2D CreateGridTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color bg = new Color(0.043f, 0.059f, 0.094f, 1f);    // #0b0f18
            Color line = new Color(0.12f, 0.17f, 0.26f, 1f);  // #1e2b42

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x == 0 || x == size - 1 || y == 0 || y == size - 1);
                    colors[y * size + x] = isBorder ? line : bg;
                }
            }
            tex.SetPixels(colors);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return tex;
        }

        private static void SetupModels()
        {
            ModelDataManager dataManager = ModelDataManager.Instance ?? FindFirstObjectByType<ModelDataManager>();
            if (dataManager == null) return;

            dataManager.models.Clear();
            dataManager.currentModelIndex = 0;

            GameObject modelsRoot = new GameObject("ModelsContainer");

            var modelDefs = new[]
            {
                new { name = "Master_Controller", format = ".fbx", date = "12 Sep 2025", size = "24.8 MB",
                      path = "Assets/Master Controller/Master Controller Model/Master Controller with Shell.fbx",
                      matPath = "Assets/Master Controller/Master Controller Model/Master_Controller.mat",
                      status = ModelStatus.Finalized },
                new { name = "HVAC_Unit", format = ".glb", date = "10 Sep 2025", size = "18.2 MB",
                      path = "Assets/Models/HVAC/Model/HVAC_FINAL_MODEL.fbx",
                      matPath = "",
                      status = ModelStatus.UnderReview },
                new { name = "Signal_Cabinet", format = ".obj", date = "08 Sep 2025", size = "14.5 MB",
                      path = "Assets/Models/EmergencyDoor/Model/Detrainment_Door.fbx",
                      matPath = "",
                      status = ModelStatus.UnderReview },
                new { name = "Control_Panel", format = ".fbx", date = "05 Sep 2025", size = "26.4 MB",
                      path = "Assets/Master Controller/Master Controller Model/Master Controller.fbx",
                      matPath = "Assets/Master Controller/Master Controller Model/Master_Controller.mat",
                      status = ModelStatus.CorrectionRequired },
                new { name = "Train_Cab", format = ".glb", date = "01 Sep 2025", size = "41.6 MB",
                      path = "Assets/Models/BRAKES/Brakes_for_approval.fbx",
                      matPath = "",
                      status = ModelStatus.NotReviewed }
            };

            ThumbnailGenerator thumbGen = ThumbnailGenerator.Instance ?? FindFirstObjectByType<ThumbnailGenerator>();
            if (thumbGen != null)
            {
                thumbGen.SetupThumbnailCamera();
            }

            for (int i = 0; i < modelDefs.Length; i++)
            {
                var def = modelDefs[i];
                GameObject modelInstance = null;

#if UNITY_EDITOR
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.path);
                if (prefab != null)
                {
                    modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, modelsRoot.transform);
                }
#endif

                if (modelInstance == null)
                {
                    modelInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    modelInstance.transform.SetParent(modelsRoot.transform);
                }

                modelInstance.name = def.name;
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;

#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(def.matPath))
                {
                    Material assignedMat = AssetDatabase.LoadAssetAtPath<Material>(def.matPath);
                    if (assignedMat != null)
                    {
                        foreach (var r in modelInstance.GetComponentsInChildren<Renderer>(true))
                        {
                            Material[] mats = new Material[r.sharedMaterials.Length];
                            for (int m = 0; m < mats.Length; m++) mats[m] = assignedMat;
                            r.sharedMaterials = mats;
                        }
                    }
                }
#endif

                // Normalize scale so any model (including wide train brakes) fits nicely in ~2.0m view box
                NormalizeModelScale(modelInstance);

                // Initial active state: ONLY model 0 (Master_Controller) active
                modelInstance.SetActive(i == 0);

                // Generate thumbnail
                Sprite thumb = thumbGen != null ? thumbGen.GenerateThumbnail(modelInstance) : null;

                ModelItem item = new ModelItem
                {
                    modelName = def.name,
                    fileName = Path.GetFileName(def.path),
                    fileFormat = def.format,
                    fileSize = def.size,
                    uploadDate = def.date,
                    modelObject = modelInstance,
                    thumbnailSprite = thumb,
                    status = def.status
                };

                if (def.status == ModelStatus.Finalized)
                {
                    item.finalizedDate = "12 Sep 2025";
                    item.reviewerName = "Chief 3D Inspector";
                }

                item.RecalculateStats();

                if (def.name == "Master_Controller")
                {
                    item.vertexCount = 125482;
                    item.triangleCount = 246310;
                    item.dimensions = new Vector3(1.20f, 2.10f, 0.60f);
                    item.materialCount = 8;
                    item.textureCount = 12;
                }

                dataManager.models.Add(item);
            }

            dataManager.currentModelIndex = 0;
        }

        private static void NormalizeModelScale(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float maxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDim > 0.0001f)
            {
                float targetDim = 2.10f; // Scale to ~2.10m fit
                float scaleFactor = targetDim / maxDim;
                go.transform.localScale = Vector3.one * scaleFactor;

                // Re-calculate bounds after scale
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                Vector3 center = bounds.center;
                float bottomY = bounds.min.y;
                go.transform.position = new Vector3(-center.x, -bottomY, -center.z);
            }
        }

        private static void SetupCanvasUI(Camera mainCam)
        {
            GameObject canvasGo = new GameObject("ModelViewerCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Event System
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            ModelViewerUI ui = canvasGo.AddComponent<ModelViewerUI>();
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            ui.fontAsset = font;

            // Top Bar
            SetupTopBar(canvasGo.transform, ui, font);

            // Left Sidebar
            SetupLeftSidebar(canvasGo.transform, ui, font);

            // Center Viewport
            SetupCenterViewport(canvasGo.transform, ui, font);

            // Right Sidebar
            SetupRightSidebar(canvasGo.transform, ui, font);

            // Modals
            SetupModals(canvasGo.transform, ui, font);

            // Pre-populate Model List & Checklist in Edit Mode
            PrepopulateEditModeUI(ui, font);
        }

        private static void SetupTopBar(Transform parent, ModelViewerUI ui, TMP_FontAsset font)
        {
            GameObject top = new GameObject("TopBar");
            top.transform.SetParent(parent, false);
            RectTransform rt = top.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 60);

            Image bg = top.AddComponent<Image>();
            bg.color = UIProceduralTheme.BgDark;

            // Bottom border
            GameObject bBorder = new GameObject("BottomBorder");
            bBorder.transform.SetParent(top.transform, false);
            RectTransform bbRT = bBorder.AddComponent<RectTransform>();
            bbRT.anchorMin = Vector2.zero;
            bbRT.anchorMax = new Vector2(1, 0);
            bbRT.sizeDelta = new Vector2(0, 1);
            Image bbImg = bBorder.AddComponent<Image>();
            bbImg.color = UIProceduralTheme.CardBorder;

            // Left: Logo + Divider + Model Viewer Title
            GameObject leftContainer = new GameObject("LeftLogoGroup");
            leftContainer.transform.SetParent(top.transform, false);
            RectTransform lcRT = leftContainer.AddComponent<RectTransform>();
            lcRT.anchorMin = new Vector2(0, 0.5f);
            lcRT.anchorMax = new Vector2(0, 0.5f);
            lcRT.pivot = new Vector2(0, 0.5f);
            lcRT.anchoredPosition = new Vector2(24, 0);
            lcRT.sizeDelta = new Vector2(360, 44);

            HorizontalLayoutGroup lcHLG = leftContainer.AddComponent<HorizontalLayoutGroup>();
            lcHLG.spacing = 14;
            lcHLG.childAlignment = TextAnchor.MiddleLeft;
            lcHLG.childControlWidth = false;
            lcHLG.childControlHeight = false;

            // CC Engineers Logo
            Sprite logoSprite = LoadLogoSprite();
            GameObject logoObj = new GameObject("LogoImage");
            logoObj.transform.SetParent(leftContainer.transform, false);
            RectTransform logoRT = logoObj.AddComponent<RectTransform>();
            logoRT.sizeDelta = new Vector2(125, 38);
            Image logoImg = logoObj.AddComponent<Image>();
            logoImg.preserveAspect = true;
            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
            }

            // Divider "|"
            GameObject divObj = new GameObject("Divider");
            divObj.transform.SetParent(leftContainer.transform, false);
            RectTransform divRT = divObj.AddComponent<RectTransform>();
            divRT.sizeDelta = new Vector2(1, 20);
            Image divImg = divObj.AddComponent<Image>();
            divImg.color = new Color(0.2f, 0.28f, 0.4f, 1f);

            // Title: "Model Viewer"
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(leftContainer.transform, false);
            RectTransform titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(160, 30);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.font = font;
            titleTMP.text = "Model Viewer";
            titleTMP.fontSize = 16;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.color = UIProceduralTheme.TextWhite;

            // Right: Settings & User Buttons
            GameObject rightTools = new GameObject("RightTools");
            rightTools.transform.SetParent(top.transform, false);
            RectTransform rightRT = rightTools.AddComponent<RectTransform>();
            rightRT.anchorMin = new Vector2(1, 0.5f);
            rightRT.anchorMax = new Vector2(1, 0.5f);
            rightRT.pivot = new Vector2(1, 0.5f);
            rightRT.anchoredPosition = new Vector2(-24, 0);
            rightRT.sizeDelta = new Vector2(240, 40);

            HorizontalLayoutGroup hlg = rightTools.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            CreateHeaderButton(rightTools.transform, IconType.Settings, "Settings", 100, 36, font);
            CreateHeaderButton(rightTools.transform, IconType.User, "User", 88, 36, font);
        }

        private static void SetupLeftSidebar(Transform parent, ModelViewerUI ui, TMP_FontAsset font)
        {
            GameObject sidebar = new GameObject("LeftSidebar");
            sidebar.transform.SetParent(parent, false);
            RectTransform rt = sidebar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(0, -30);
            rt.sizeDelta = new Vector2(330, -60);

            Image bg = sidebar.AddComponent<Image>();
            bg.color = UIProceduralTheme.SidebarBg;

            // Right border line
            GameObject rBorder = new GameObject("RightBorder");
            rBorder.transform.SetParent(sidebar.transform, false);
            RectTransform rbRT = rBorder.AddComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(1, 0);
            rbRT.anchorMax = new Vector2(1, 1);
            rbRT.sizeDelta = new Vector2(1, 0);
            Image rbImg = rBorder.AddComponent<Image>();
            rbImg.color = UIProceduralTheme.CardBorder;

            VerticalLayoutGroup vlg = sidebar.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 20, 20);
            vlg.spacing = 14;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            // Upload Button Card (Gradient/Solid Blue)
            GameObject uploadCard = new GameObject("UploadCard");
            uploadCard.transform.SetParent(sidebar.transform, false);
            RectTransform upRT = uploadCard.AddComponent<RectTransform>();
            upRT.sizeDelta = new Vector2(0, 76);
            LayoutElement upLE = uploadCard.AddComponent<LayoutElement>();
            upLE.minHeight = 76;
            upLE.preferredHeight = 76;
            upLE.flexibleHeight = 0;

            Image upBg = uploadCard.AddComponent<Image>();
            upBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 12, UIProceduralTheme.BluePrimary);
            upBg.type = Image.Type.Sliced;

            Button upBtn = uploadCard.AddComponent<Button>();
            upBtn.onClick.AddListener(() => ui.TriggerUploadModel());

            VerticalLayoutGroup upVLG = uploadCard.AddComponent<VerticalLayoutGroup>();
            upVLG.padding = new RectOffset(10, 10, 12, 10);
            upVLG.spacing = 4;
            upVLG.childAlignment = TextAnchor.MiddleCenter;
            upVLG.childControlWidth = true;
            upVLG.childControlHeight = false;

            // Upload row with Cloud icon + Title
            GameObject upRow = new GameObject("UpRow");
            upRow.transform.SetParent(uploadCard.transform, false);
            RectTransform urRT = upRow.AddComponent<RectTransform>();
            urRT.sizeDelta = new Vector2(0, 26);
            HorizontalLayoutGroup urHLG = upRow.AddComponent<HorizontalLayoutGroup>();
            urHLG.spacing = 8;
            urHLG.childAlignment = TextAnchor.MiddleCenter;
            urHLG.childControlWidth = false;
            urHLG.childControlHeight = false;

            GameObject upIconObj = new GameObject("CloudIcon");
            upIconObj.transform.SetParent(upRow.transform, false);
            RectTransform uicRT = upIconObj.AddComponent<RectTransform>();
            uicRT.sizeDelta = new Vector2(20, 20);
            Image uicImg = upIconObj.AddComponent<Image>();
            uicImg.sprite = UIProceduralTheme.GetIconSprite(IconType.CloudUpload, Color.white, 32);

            GameObject upTitle = new GameObject("UpTitle");
            upTitle.transform.SetParent(upRow.transform, false);
            RectTransform utRT = upTitle.AddComponent<RectTransform>();
            utRT.sizeDelta = new Vector2(130, 26);
            TextMeshProUGUI upTitleTMP = upTitle.AddComponent<TextMeshProUGUI>();
            upTitleTMP.font = font;
            upTitleTMP.text = "Upload Model";
            upTitleTMP.fontSize = 15;
            upTitleTMP.fontStyle = FontStyles.Bold;
            upTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            upTitleTMP.color = Color.white;

            GameObject upSub = new GameObject("UpSub");
            upSub.transform.SetParent(uploadCard.transform, false);
            TextMeshProUGUI upSubTMP = upSub.AddComponent<TextMeshProUGUI>();
            upSubTMP.font = font;
            upSubTMP.text = "Drag & drop or click to upload";
            upSubTMP.fontSize = 11;
            upSubTMP.alignment = TextAlignmentOptions.Center;
            upSubTMP.color = new Color(0.85f, 0.92f, 1f, 0.9f);

            // Supported Formats Row
            GameObject formatRow = new GameObject("FormatRow");
            formatRow.transform.SetParent(sidebar.transform, false);
            RectTransform fRT = formatRow.AddComponent<RectTransform>();
            fRT.sizeDelta = new Vector2(0, 52);
            LayoutElement fLE = formatRow.AddComponent<LayoutElement>();
            fLE.minHeight = 52;
            fLE.preferredHeight = 52;
            fLE.flexibleHeight = 0;

            VerticalLayoutGroup fVLG = formatRow.AddComponent<VerticalLayoutGroup>();
            fVLG.spacing = 6;
            fVLG.childControlWidth = true;
            fVLG.childControlHeight = false;

            GameObject fLabel = new GameObject("FLabel");
            fLabel.transform.SetParent(formatRow.transform, false);
            RectTransform flRT = fLabel.AddComponent<RectTransform>();
            flRT.sizeDelta = new Vector2(0, 18);
            TextMeshProUGUI fLabelTMP = fLabel.AddComponent<TextMeshProUGUI>();
            fLabelTMP.font = font;
            fLabelTMP.text = "Supported Formats";
            fLabelTMP.fontSize = 11;
            fLabelTMP.color = UIProceduralTheme.TextMuted;

            GameObject badgesRow = new GameObject("BadgesRow");
            badgesRow.transform.SetParent(formatRow.transform, false);
            RectTransform bRT = badgesRow.AddComponent<RectTransform>();
            bRT.sizeDelta = new Vector2(0, 26);

            HorizontalLayoutGroup bHLG = badgesRow.AddComponent<HorizontalLayoutGroup>();
            bHLG.spacing = 6;
            bHLG.childControlWidth = false;
            bHLG.childControlHeight = false;

            string[] formats = { ".fbx", ".obj", ".gltf", ".glb", ".dae" };
            foreach (var fmt in formats)
            {
                CreatePillBadge(badgesRow.transform, fmt, font);
            }

            // Model List Header
            GameObject listHeader = new GameObject("ListHeader");
            listHeader.transform.SetParent(sidebar.transform, false);
            RectTransform lhRT = listHeader.AddComponent<RectTransform>();
            lhRT.sizeDelta = new Vector2(0, 28);
            LayoutElement lhLE = listHeader.AddComponent<LayoutElement>();
            lhLE.minHeight = 28;
            lhLE.preferredHeight = 28;
            lhLE.flexibleHeight = 0;

            HorizontalLayoutGroup lhHLG = listHeader.AddComponent<HorizontalLayoutGroup>();
            lhHLG.childAlignment = TextAnchor.MiddleLeft;
            lhHLG.spacing = 8;
            lhHLG.childControlWidth = false;
            lhHLG.childControlHeight = false;

            GameObject lhTitle = new GameObject("LHTitle");
            lhTitle.transform.SetParent(listHeader.transform, false);
            TextMeshProUGUI lhTitleTMP = lhTitle.AddComponent<TextMeshProUGUI>();
            lhTitleTMP.font = font;
            lhTitleTMP.text = "Model List";
            lhTitleTMP.fontSize = 14;
            lhTitleTMP.fontStyle = FontStyles.Bold;
            lhTitleTMP.color = UIProceduralTheme.TextWhite;

            GameObject lhCount = new GameObject("LHCount");
            lhCount.transform.SetParent(listHeader.transform, false);
            TextMeshProUGUI lhCountTMP = lhCount.AddComponent<TextMeshProUGUI>();
            lhCountTMP.font = font;
            lhCountTMP.text = "(5)";
            lhCountTMP.fontSize = 12;
            lhCountTMP.color = UIProceduralTheme.TextMuted;
            ui.modelListCountText = lhCountTMP;

            // Scrollable Model List
            GameObject scrollObj = new GameObject("ModelListScroll");
            scrollObj.transform.SetParent(sidebar.transform, false);
            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.sizeDelta = new Vector2(0, 0);
            LayoutElement scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.minHeight = 200;
            scrollLE.flexibleHeight = 1;

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 25f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.spacing = 8;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRT;
            sr.content = contentRT;

            ui.modelListContainer = content.transform;
        }

        private static void SetupCenterViewport(Transform parent, ModelViewerUI ui, TMP_FontAsset font)
        {
            // Floating Top-Left Toolbar
            GameObject tlToolbar = new GameObject("TopLeftToolbar");
            tlToolbar.transform.SetParent(parent, false);
            RectTransform tlRT = tlToolbar.AddComponent<RectTransform>();
            tlRT.anchorMin = new Vector2(0, 1);
            tlRT.anchorMax = new Vector2(0, 1);
            tlRT.pivot = new Vector2(0, 1);
            tlRT.anchoredPosition = new Vector2(350, -80);
            tlRT.sizeDelta = new Vector2(216, 44);

            Image tlBg = tlToolbar.AddComponent<Image>();
            tlBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 10, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            tlBg.type = Image.Type.Sliced;

            HorizontalLayoutGroup tlHLG = tlToolbar.AddComponent<HorizontalLayoutGroup>();
            tlHLG.padding = new RectOffset(6, 6, 4, 4);
            tlHLG.spacing = 6;
            tlHLG.childControlWidth = false;
            tlHLG.childControlHeight = false;

            ui.orbitModeBtn = CreateIconToolButton(tlToolbar.transform, IconType.Orbit, 36, true);
            ui.panModeBtn = CreateIconToolButton(tlToolbar.transform, IconType.Pan, 36, false);
            ui.zoomModeBtn = CreateIconToolButton(tlToolbar.transform, IconType.Zoom, 36, false);
            ui.focusSelectedBtn = CreateIconToolButton(tlToolbar.transform, IconType.Focus, 36, false);
            ui.fitModelBtn = CreateIconToolButton(tlToolbar.transform, IconType.Fit, 36, false);

            // Floating Top-Right View Mode Dropdown
            GameObject trControls = new GameObject("TopRightControls");
            trControls.transform.SetParent(parent, false);
            RectTransform trRT = trControls.AddComponent<RectTransform>();
            trRT.anchorMin = new Vector2(1, 1);
            trRT.anchorMax = new Vector2(1, 1);
            trRT.pivot = new Vector2(1, 1);
            trRT.anchoredPosition = new Vector2(-400, -80);
            trRT.sizeDelta = new Vector2(130, 40);

            TMP_Dropdown dropdown = trControls.AddComponent<TMP_Dropdown>();
            Image dropBg = trControls.AddComponent<Image>();
            dropBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            dropBg.type = Image.Type.Sliced;

            // Caption Text
            GameObject captionObj = new GameObject("CaptionText");
            captionObj.transform.SetParent(trControls.transform, false);
            RectTransform capRT = captionObj.AddComponent<RectTransform>();
            capRT.anchorMin = Vector2.zero;
            capRT.anchorMax = Vector2.one;
            capRT.offsetMin = new Vector2(14, 0);
            capRT.offsetMax = new Vector2(-30, 0);

            TextMeshProUGUI captionTMP = captionObj.AddComponent<TextMeshProUGUI>();
            captionTMP.font = font;
            captionTMP.fontSize = 13;
            captionTMP.alignment = TextAlignmentOptions.MidlineLeft;
            captionTMP.color = UIProceduralTheme.TextWhite;
            dropdown.captionText = captionTMP;

            // Dropdown Chevron Icon
            GameObject chevObj = new GameObject("Chevron");
            chevObj.transform.SetParent(trControls.transform, false);
            RectTransform chevRT = chevObj.AddComponent<RectTransform>();
            chevRT.anchorMin = new Vector2(1, 0.5f);
            chevRT.anchorMax = new Vector2(1, 0.5f);
            chevRT.pivot = new Vector2(1, 0.5f);
            chevRT.anchoredPosition = new Vector2(-12, 0);
            chevRT.sizeDelta = new Vector2(14, 14);
            Image chevImg = chevObj.AddComponent<Image>();
            chevImg.sprite = UIProceduralTheme.GetIconSprite(IconType.ChevronDown, UIProceduralTheme.TextMuted, 24);

            dropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Shaded View"),
                new TMP_Dropdown.OptionData("Wireframe View"),
                new TMP_Dropdown.OptionData("X-Ray View"),
                new TMP_Dropdown.OptionData("Isometric View"),
                new TMP_Dropdown.OptionData("Front View"),
                new TMP_Dropdown.OptionData("Back View"),
                new TMP_Dropdown.OptionData("Left View"),
                new TMP_Dropdown.OptionData("Right View"),
                new TMP_Dropdown.OptionData("Top View"),
                new TMP_Dropdown.OptionData("Bottom View"),
                new TMP_Dropdown.OptionData("Toggle Ortho")
            };
            ui.viewModeDropdown = dropdown;
            
            CreateDropdownTemplate(trControls, dropdown, font);

            // Floating Bottom Toolbar
            GameObject bToolbar = new GameObject("BottomToolbar");
            bToolbar.transform.SetParent(parent, false);
            RectTransform bRT = bToolbar.AddComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 0);
            bRT.anchorMax = new Vector2(0.5f, 0);
            bRT.pivot = new Vector2(0.5f, 0);
            bRT.anchoredPosition = new Vector2(-20, 24);
            bRT.sizeDelta = new Vector2(380, 48);

            Image bBg = bToolbar.AddComponent<Image>();
            bBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 12, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            bBg.type = Image.Type.Sliced;

            HorizontalLayoutGroup bHLG = bToolbar.AddComponent<HorizontalLayoutGroup>();
            bHLG.padding = new RectOffset(8, 8, 6, 6);
            bHLG.spacing = 8;
            bHLG.childAlignment = TextAnchor.MiddleCenter;
            bHLG.childControlWidth = false;
            bHLG.childControlHeight = false;

            ui.bottomOrbitBtn = CreateBottomPillButton(bToolbar.transform, IconType.Orbit, "Orbit", 80, 36, font, true);
            ui.bottomPanBtn = CreateBottomPillButton(bToolbar.transform, IconType.Pan, "Pan", 75, 36, font, false);
            ui.bottomZoomBtn = CreateBottomPillButton(bToolbar.transform, IconType.Zoom, "Zoom", 80, 36, font, false);
            ui.bottomResetBtn = CreateBottomPillButton(bToolbar.transform, IconType.Reset, "Reset View", 105, 36, font, false);

            // Selection Tag
            GameObject tagObj = new GameObject("SelectionTag");
            tagObj.transform.SetParent(parent, false);
            RectTransform tagRT = tagObj.AddComponent<RectTransform>();
            tagRT.anchorMin = new Vector2(0.5f, 1);
            tagRT.anchorMax = new Vector2(0.5f, 1);
            tagRT.pivot = new Vector2(0.5f, 1);
            tagRT.anchoredPosition = new Vector2(-20, -80);
            tagRT.sizeDelta = new Vector2(300, 32);

            TextMeshProUGUI tagTMP = tagObj.AddComponent<TextMeshProUGUI>();
            tagTMP.font = font;
            tagTMP.fontSize = 13;
            tagTMP.alignment = TextAlignmentOptions.Center;
            tagTMP.color = UIProceduralTheme.BlueHover;
            tagObj.SetActive(false);
            ui.selectionTagText = tagTMP;

            // Mini Thumbnail in viewport bottom-right (single clean image, no empty child)
            GameObject miniObj = new GameObject("MiniThumbnailCard");
            miniObj.transform.SetParent(parent, false);
            RectTransform miniRT = miniObj.AddComponent<RectTransform>();
            miniRT.anchorMin = new Vector2(1, 0);
            miniRT.anchorMax = new Vector2(1, 0);
            miniRT.pivot = new Vector2(1, 0);
            miniRT.anchoredPosition = new Vector2(-400, 24);
            miniRT.sizeDelta = new Vector2(68, 68);

            Image miniBg = miniObj.AddComponent<Image>();
            miniBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorderSelected, 2);
            miniBg.type = Image.Type.Sliced;
            ui.miniThumbnail = miniBg;
        }

        private static void SetupRightSidebar(Transform parent, ModelViewerUI ui, TMP_FontAsset font)
        {
            GameObject sidebar = new GameObject("RightSidebar");
            sidebar.transform.SetParent(parent, false);
            RectTransform rt = sidebar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(0, -30);
            rt.sizeDelta = new Vector2(380, -60);

            Image bg = sidebar.AddComponent<Image>();
            bg.color = UIProceduralTheme.SidebarBg;

            // Left border line
            GameObject lBorder = new GameObject("LeftBorder");
            lBorder.transform.SetParent(sidebar.transform, false);
            RectTransform lbRT = lBorder.AddComponent<RectTransform>();
            lbRT.anchorMin = new Vector2(0, 0);
            lbRT.anchorMax = new Vector2(0, 1);
            lbRT.sizeDelta = new Vector2(1, 0);
            Image lbImg = lBorder.AddComponent<Image>();
            lbImg.color = UIProceduralTheme.CardBorder;

            VerticalLayoutGroup vlg = sidebar.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            // Section 1: Model Details Header
            GameObject detHeader = new GameObject("DetailsHeader");
            detHeader.transform.SetParent(sidebar.transform, false);
            RectTransform dhRT = detHeader.AddComponent<RectTransform>();
            dhRT.sizeDelta = new Vector2(0, 22);
            LayoutElement dhLE = detHeader.AddComponent<LayoutElement>();
            dhLE.minHeight = 22;
            dhLE.preferredHeight = 22;
            dhLE.flexibleHeight = 0;

            TextMeshProUGUI dhTMP = detHeader.AddComponent<TextMeshProUGUI>();
            dhTMP.font = font;
            dhTMP.text = "Model Details";
            dhTMP.fontSize = 18;
            dhTMP.fontStyle = FontStyles.Bold;
            dhTMP.color = UIProceduralTheme.TextWhite;

            // Model Header Card
            GameObject modelCard = new GameObject("ModelHeaderCard");
            modelCard.transform.SetParent(sidebar.transform, false);
            RectTransform mcRT = modelCard.AddComponent<RectTransform>();
            mcRT.sizeDelta = new Vector2(0, 68);
            LayoutElement mcLE = modelCard.AddComponent<LayoutElement>();
            mcLE.minHeight = 68;
            mcLE.preferredHeight = 68;
            mcLE.flexibleHeight = 0;

            HorizontalLayoutGroup mcHLG = modelCard.AddComponent<HorizontalLayoutGroup>();
            mcHLG.padding = new RectOffset(0, 0, 0, 0);
            mcHLG.spacing = 12;
            mcHLG.childAlignment = TextAnchor.MiddleLeft;
            mcHLG.childControlWidth = false;
            mcHLG.childControlHeight = false;

            GameObject thumbObj = new GameObject("Thumb");
            thumbObj.transform.SetParent(modelCard.transform, false);
            RectTransform tRT = thumbObj.AddComponent<RectTransform>();
            tRT.sizeDelta = new Vector2(60, 60);
            Image thumbImg = thumbObj.AddComponent<Image>();
            thumbImg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            ui.detailThumbnail = thumbImg;

            GameObject titleCol = new GameObject("TitleCol");
            titleCol.transform.SetParent(modelCard.transform, false);
            RectTransform tcRT = titleCol.AddComponent<RectTransform>();
            tcRT.sizeDelta = new Vector2(250, 56);
            VerticalLayoutGroup tcVLG = titleCol.AddComponent<VerticalLayoutGroup>();
            tcVLG.spacing = 4;
            tcVLG.childControlWidth = true;
            tcVLG.childControlHeight = true;

            GameObject titleText = new GameObject("TitleText");
            titleText.transform.SetParent(titleCol.transform, false);
            TextMeshProUGUI ttTMP = titleText.AddComponent<TextMeshProUGUI>();
            ttTMP.font = font;
            ttTMP.text = "Master_Controller";
            ttTMP.fontSize = 18;
            ttTMP.fontStyle = FontStyles.Bold;
            ttTMP.color = UIProceduralTheme.TextWhite;
            ui.detailModelName = ttTMP;

            GameObject subText = new GameObject("SubText");
            subText.transform.SetParent(titleCol.transform, false);
            TextMeshProUGUI stTMP = subText.AddComponent<TextMeshProUGUI>();
            stTMP.font = font;
            stTMP.text = ".fbx • 12 Sep 2025 • 24.8 MB";
            stTMP.fontSize = 11;
            stTMP.color = UIProceduralTheme.TextMuted;
            ui.detailSubtitle = stTMP;

            // Specs Table
            GameObject table = new GameObject("SpecsTable");
            table.transform.SetParent(sidebar.transform, false);
            RectTransform tabRT = table.AddComponent<RectTransform>();
            tabRT.sizeDelta = new Vector2(0, 115);
            LayoutElement tabLE = table.AddComponent<LayoutElement>();
            tabLE.minHeight = 115;
            tabLE.preferredHeight = 115;
            tabLE.flexibleHeight = 0;

            VerticalLayoutGroup tVLG = table.AddComponent<VerticalLayoutGroup>();
            tVLG.spacing = 4;
            tVLG.childControlWidth = true;
            tVLG.childControlHeight = false;

            ui.detailVertices = CreateSpecRow(table.transform, "Vertices", "125,482", font);
            ui.detailTriangles = CreateSpecRow(table.transform, "Triangles", "246,310", font);
            ui.detailDimensions = CreateSpecRow(table.transform, "Dimensions (m)", "1.20 x 0.60 x 2.10", font);
            ui.detailMaterials = CreateSpecRow(table.transform, "Materials", "8", font);
            ui.detailTextures = CreateSpecRow(table.transform, "Textures", "12", font);

            // Divider
            CreateDivider(sidebar.transform);

            // Section 2: Review Checklist Header
            GameObject checkHeader = new GameObject("ChecklistHeader");
            checkHeader.transform.SetParent(sidebar.transform, false);
            RectTransform chRT = checkHeader.AddComponent<RectTransform>();
            chRT.sizeDelta = new Vector2(0, 22);
            LayoutElement chLE = checkHeader.AddComponent<LayoutElement>();
            chLE.minHeight = 22;
            chLE.preferredHeight = 22;
            chLE.flexibleHeight = 0;

            TextMeshProUGUI chTMP = checkHeader.AddComponent<TextMeshProUGUI>();
            chTMP.font = font;
            chTMP.text = "Review Checklist";
            chTMP.fontSize = 18;
            chTMP.fontStyle = FontStyles.Bold;
            chTMP.color = UIProceduralTheme.TextWhite;

            // Checklist items container with explicit layout height
            GameObject checkListContainer = new GameObject("ChecklistContainer");
            checkListContainer.transform.SetParent(sidebar.transform, false);
            RectTransform clcRT = checkListContainer.AddComponent<RectTransform>();
            clcRT.sizeDelta = new Vector2(0, 264);
            LayoutElement clcLE = checkListContainer.AddComponent<LayoutElement>();
            clcLE.minHeight = 264;
            clcLE.preferredHeight = 264;
            clcLE.flexibleHeight = 0;

            VerticalLayoutGroup clVLG = checkListContainer.AddComponent<VerticalLayoutGroup>();
            clVLG.spacing = 6;
            clVLG.childControlWidth = true;
            clVLG.childControlHeight = false;
            ui.checklistContainer = checkListContainer.transform;

            // Status Card
            GameObject statusCard = new GameObject("StatusCard");
            statusCard.transform.SetParent(sidebar.transform, false);
            RectTransform scRT = statusCard.AddComponent<RectTransform>();
            scRT.sizeDelta = new Vector2(0, 68);
            LayoutElement scLE = statusCard.AddComponent<LayoutElement>();
            scLE.minHeight = 68;
            scLE.preferredHeight = 68;
            scLE.flexibleHeight = 0;

            Image scBg = statusCard.AddComponent<Image>();
            scBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 12, UIProceduralTheme.GreenReadyBg, UIProceduralTheme.GreenReady, 2);
            scBg.type = Image.Type.Sliced;
            ui.statusCardBg = scBg;

            HorizontalLayoutGroup scHLG = statusCard.AddComponent<HorizontalLayoutGroup>();
            scHLG.padding = new RectOffset(16, 16, 12, 12);
            scHLG.spacing = 12;
            scHLG.childAlignment = TextAnchor.MiddleLeft;
            scHLG.childControlWidth = false;
            scHLG.childControlHeight = false;

            GameObject scIcon = new GameObject("Icon");
            scIcon.transform.SetParent(statusCard.transform, false);
            RectTransform sciRT = scIcon.AddComponent<RectTransform>();
            sciRT.sizeDelta = new Vector2(32, 32);
            Image sciImg = scIcon.AddComponent<Image>();
            sciImg.sprite = UIProceduralTheme.GetCircleSprite(32, UIProceduralTheme.GreenReady);

            GameObject markObj = new GameObject("Mark");
            markObj.transform.SetParent(scIcon.transform, false);
            RectTransform mRT = markObj.AddComponent<RectTransform>();
            mRT.anchorMin = Vector2.zero;
            mRT.anchorMax = Vector2.one;
            mRT.sizeDelta = new Vector2(-6, -6);
            Image markImg = markObj.AddComponent<Image>();
            markImg.sprite = UIProceduralTheme.GetIconSprite(IconType.Checkmark, Color.white, 32);

            GameObject scTextCol = new GameObject("TextCol");
            scTextCol.transform.SetParent(statusCard.transform, false);
            RectTransform sctRT = scTextCol.AddComponent<RectTransform>();
            sctRT.sizeDelta = new Vector2(250, 44);
            VerticalLayoutGroup sctVLG = scTextCol.AddComponent<VerticalLayoutGroup>();
            sctVLG.spacing = 2;
            sctVLG.childControlWidth = true;
            sctVLG.childControlHeight = false;

            GameObject sctTitle = new GameObject("Title");
            sctTitle.transform.SetParent(scTextCol.transform, false);
            TextMeshProUGUI sctTMP = sctTitle.AddComponent<TextMeshProUGUI>();
            sctTMP.font = font;
            sctTitle.GetComponent<TextMeshProUGUI>().text = "Model Ready";
            sctTMP.fontSize = 16;
            sctTMP.fontStyle = FontStyles.Bold;
            sctTMP.color = UIProceduralTheme.GreenReady;
            ui.statusCardTitle = sctTMP;

            GameObject sctSub = new GameObject("Sub");
            sctSub.transform.SetParent(scTextCol.transform, false);
            TextMeshProUGUI scsubTMP = sctSub.AddComponent<TextMeshProUGUI>();
            scsubTMP.font = font;
            scsubTMP.text = "This model is finalized and approved.";
            scsubTMP.fontSize = 11;
            scsubTMP.color = UIProceduralTheme.TextMuted;
            ui.statusCardSubtitle = scsubTMP;

            // Action Button 1: Mark as Finalized
            GameObject finalizeBtnObj = new GameObject("FinalizeButton");
            finalizeBtnObj.transform.SetParent(sidebar.transform, false);
            RectTransform fbRT = finalizeBtnObj.AddComponent<RectTransform>();
            fbRT.sizeDelta = new Vector2(0, 48);
            LayoutElement fbLE = finalizeBtnObj.AddComponent<LayoutElement>();
            fbLE.minHeight = 48;
            fbLE.preferredHeight = 48;
            fbLE.flexibleHeight = 0;

            Image fbBg = finalizeBtnObj.AddComponent<Image>();
            fbBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 10, UIProceduralTheme.BluePrimary);
            fbBg.type = Image.Type.Sliced;
            Button fbBtn = finalizeBtnObj.AddComponent<Button>();
            ui.finalizeButton = fbBtn;

            HorizontalLayoutGroup fbHLG = finalizeBtnObj.AddComponent<HorizontalLayoutGroup>();
            fbHLG.spacing = 8;
            fbHLG.childAlignment = TextAnchor.MiddleCenter;
            fbHLG.childControlWidth = false;
            fbHLG.childControlHeight = false;

            GameObject fbIconObj = new GameObject("CheckIcon");
            fbIconObj.transform.SetParent(finalizeBtnObj.transform, false);
            RectTransform fbiRT = fbIconObj.AddComponent<RectTransform>();
            fbiRT.sizeDelta = new Vector2(18, 18);
            Image fbiImg = fbIconObj.AddComponent<Image>();
            fbiImg.sprite = UIProceduralTheme.GetIconSprite(IconType.Checkmark, Color.white, 24);
            ui.finalizeButtonIcon = fbiImg;

            GameObject fbText = new GameObject("Text");
            fbText.transform.SetParent(finalizeBtnObj.transform, false);
            RectTransform fbtRT = fbText.AddComponent<RectTransform>();
            fbtRT.sizeDelta = new Vector2(150, 40);
            TextMeshProUGUI fbtTMP = fbText.AddComponent<TextMeshProUGUI>();
            fbtTMP.font = font;
            fbtTMP.text = "Mark as Finalized";
            fbtTMP.fontSize = 14;
            fbtTMP.fontStyle = FontStyles.Bold;
            fbtTMP.alignment = TextAlignmentOptions.MidlineLeft;
            fbtTMP.color = Color.white;
            ui.finalizeButtonText = fbtTMP;

            // Action Button 2: Add Comments
            GameObject commentBtnObj = new GameObject("AddCommentButton");
            commentBtnObj.transform.SetParent(sidebar.transform, false);
            RectTransform cbRT = commentBtnObj.AddComponent<RectTransform>();
            cbRT.sizeDelta = new Vector2(0, 44);
            LayoutElement cbLE = commentBtnObj.AddComponent<LayoutElement>();
            cbLE.minHeight = 44;
            cbLE.preferredHeight = 44;
            cbLE.flexibleHeight = 0;

            Image cbBg = commentBtnObj.AddComponent<Image>();
            cbBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 10, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            cbBg.type = Image.Type.Sliced;
            Button cbBtn = commentBtnObj.AddComponent<Button>();
            ui.addCommentsButton = cbBtn;

            HorizontalLayoutGroup cbHLG = commentBtnObj.AddComponent<HorizontalLayoutGroup>();
            cbHLG.spacing = 8;
            cbHLG.childAlignment = TextAnchor.MiddleCenter;
            cbHLG.childControlWidth = false;
            cbHLG.childControlHeight = false;

            GameObject cbIconObj = new GameObject("CommentIcon");
            cbIconObj.transform.SetParent(commentBtnObj.transform, false);
            RectTransform cbiRT = cbIconObj.AddComponent<RectTransform>();
            cbiRT.sizeDelta = new Vector2(18, 18);
            Image cbiImg = cbIconObj.AddComponent<Image>();
            cbiImg.sprite = UIProceduralTheme.GetIconSprite(IconType.Comment, Color.white, 24);
            ui.addCommentsButtonIcon = cbiImg;

            GameObject cbText = new GameObject("Text");
            cbText.transform.SetParent(commentBtnObj.transform, false);
            RectTransform cbtRT = cbText.AddComponent<RectTransform>();
            cbtRT.sizeDelta = new Vector2(130, 40);
            TextMeshProUGUI cbtTMP = cbText.AddComponent<TextMeshProUGUI>();
            cbtTMP.font = font;
            cbtTMP.text = "Add Comments";
            cbtTMP.fontSize = 13;
            cbtTMP.alignment = TextAlignmentOptions.MidlineLeft;
            cbtTMP.color = UIProceduralTheme.TextWhite;
        }

        private static void PrepopulateEditModeUI(ModelViewerUI ui, TMP_FontAsset font)
        {
            ModelDataManager dm = ModelDataManager.Instance ?? FindFirstObjectByType<ModelDataManager>();
            if (dm == null || dm.models.Count == 0) return;

            // 1. Populate 5 Model Cards in Left List
            if (ui.modelListContainer != null)
            {
                for (int i = 0; i < dm.models.Count; i++)
                {
                    ModelItem item = dm.models[i];
                    ui.CreateModelCard(item, i, i == 0);
                }
            }

            // 2. Populate 5 Checklist Items in Right Sidebar
            if (ui.checklistContainer != null)
            {
                ModelItem firstModel = dm.models[0];
                ui.RefreshChecklist(firstModel);
            }

            // 3. Set Right Details
            if (dm.models.Count > 0)
            {
                ModelItem firstModel = dm.models[0];
                ui.RefreshDetails(firstModel);
            }
        }

        private static void SetupModals(Transform parent, ModelViewerUI ui, TMP_FontAsset font)
        {
            // Comments Modal
            GameObject cModal = CreateModalBase(parent, "CommentsModal", 450, 480);
            ui.commentsModal = cModal;

            Transform cContent = cModal.transform.GetChild(0);

            GameObject cTitle = new GameObject("Title");
            cTitle.transform.SetParent(cContent, false);
            TextMeshProUGUI cTitleTMP = cTitle.AddComponent<TextMeshProUGUI>();
            cTitleTMP.font = font;
            cTitleTMP.text = "Review Comments";
            cTitleTMP.fontSize = 16;
            cTitleTMP.fontStyle = FontStyles.Bold;
            cTitleTMP.color = UIProceduralTheme.TextWhite;

            GameObject cScroll = new GameObject("Scroll");
            cScroll.transform.SetParent(cContent, false);
            RectTransform csRT = cScroll.AddComponent<RectTransform>();
            csRT.sizeDelta = new Vector2(0, 260);

            ScrollRect csSR = cScroll.AddComponent<ScrollRect>();
            csSR.horizontal = false;
            csSR.vertical = true;

            GameObject csVP = new GameObject("VP");
            csVP.transform.SetParent(cScroll.transform, false);
            RectTransform csVPRT = csVP.AddComponent<RectTransform>();
            csVPRT.anchorMin = Vector2.zero;
            csVPRT.anchorMax = Vector2.one;
            csVPRT.sizeDelta = Vector2.zero;
            csVP.AddComponent<RectMask2D>();

            GameObject csList = new GameObject("List");
            csList.transform.SetParent(csVP.transform, false);
            RectTransform cslRT = csList.AddComponent<RectTransform>();
            cslRT.anchorMin = new Vector2(0, 1);
            cslRT.anchorMax = new Vector2(1, 1);
            cslRT.pivot = new Vector2(0.5f, 1);
            VerticalLayoutGroup cslVLG = csList.AddComponent<VerticalLayoutGroup>();
            cslVLG.spacing = 8;
            cslVLG.childControlWidth = true;
            cslVLG.childControlHeight = false;

            ContentSizeFitter csf = csList.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            csSR.viewport = csVPRT;
            csSR.content = cslRT;
            ui.commentsListContainer = csList.transform;

            // Comment input
            GameObject inObj = new GameObject("CommentInput");
            inObj.transform.SetParent(cContent, false);
            RectTransform inRT = inObj.AddComponent<RectTransform>();
            inRT.sizeDelta = new Vector2(0, 42);

            Image inBg = inObj.AddComponent<Image>();
            inBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            inBg.type = Image.Type.Sliced;

            TMP_InputField inField = inObj.AddComponent<TMP_InputField>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inObj.transform, false);
            RectTransform tRT = textObj.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(12, 0);
            tRT.offsetMax = new Vector2(-12, 0);
            TextMeshProUGUI inTMP = textObj.AddComponent<TextMeshProUGUI>();
            inTMP.font = font;
            inTMP.fontSize = 13;
            inTMP.color = UIProceduralTheme.TextWhite;
            inField.textComponent = inTMP;

            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(inObj.transform, false);
            RectTransform phRT = phObj.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(12, 0);
            phRT.offsetMax = new Vector2(-12, 0);
            TextMeshProUGUI phTMP = phObj.AddComponent<TextMeshProUGUI>();
            phTMP.font = font;
            phTMP.fontSize = 13;
            phTMP.text = "Write your feedback / comment...";
            phTMP.color = UIProceduralTheme.TextMuted;
            inField.placeholder = phTMP;

            ui.commentInput = inField;

            // Row with Post and Close buttons
            GameObject btnRow = new GameObject("BtnRow");
            btnRow.transform.SetParent(cContent, false);
            RectTransform brRT = btnRow.AddComponent<RectTransform>();
            brRT.sizeDelta = new Vector2(0, 40);

            HorizontalLayoutGroup brHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
            brHLG.spacing = 10;
            brHLG.childControlWidth = true;
            brHLG.childControlHeight = true;

            ui.postCommentBtn = CreateSimpleButton(btnRow.transform, "Post Comment", font, UIProceduralTheme.BluePrimary);
            ui.closeCommentsBtn = CreateSimpleButton(btnRow.transform, "Close", font, UIProceduralTheme.CardBg);

            cModal.SetActive(false);

            // Finalize Confirmation Modal
            GameObject fModal = CreateModalBase(parent, "FinalizeModal", 420, 260);
            ui.finalizeModal = fModal;
            Transform fContent = fModal.transform.GetChild(0);

            GameObject fTitle = new GameObject("Title");
            fTitle.transform.SetParent(fContent, false);
            TextMeshProUGUI fTitleTMP = fTitle.AddComponent<TextMeshProUGUI>();
            fTitleTMP.font = font;
            fTitleTMP.text = "Finalize Model Approval";
            fTitleTMP.fontSize = 16;
            fTitleTMP.fontStyle = FontStyles.Bold;
            fTitleTMP.color = UIProceduralTheme.TextWhite;
            ui.finalizeModalTitle = fTitleTMP;

            GameObject fMsg = new GameObject("Message");
            fMsg.transform.SetParent(fContent, false);
            TextMeshProUGUI fMsgTMP = fMsg.AddComponent<TextMeshProUGUI>();
            fMsgTMP.font = font;
            fMsgTMP.text = "This will mark all review criteria as approved and lock the model as Finalized.";
            fMsgTMP.fontSize = 12;
            fMsgTMP.color = UIProceduralTheme.TextMuted;
            ui.finalizeModalMessage = fMsgTMP;

            // Inspector Name Input
            GameObject nameInObj = new GameObject("ReviewerInput");
            nameInObj.transform.SetParent(fContent, false);
            RectTransform ninRT = nameInObj.AddComponent<RectTransform>();
            ninRT.sizeDelta = new Vector2(0, 40);

            Image ninBg = nameInObj.AddComponent<Image>();
            ninBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            ninBg.type = Image.Type.Sliced;

            TMP_InputField ninField = nameInObj.AddComponent<TMP_InputField>();

            GameObject ntextObj = new GameObject("Text");
            ntextObj.transform.SetParent(nameInObj.transform, false);
            RectTransform ntRT = ntextObj.AddComponent<RectTransform>();
            ntRT.anchorMin = Vector2.zero;
            ntRT.anchorMax = Vector2.one;
            ntRT.offsetMin = new Vector2(10, 0);
            ntRT.offsetMax = new Vector2(-10, 0);
            TextMeshProUGUI ninTMP = ntextObj.AddComponent<TextMeshProUGUI>();
            ninTMP.font = font;
            ninTMP.fontSize = 13;
            ninTMP.color = UIProceduralTheme.TextWhite;
            ninField.textComponent = ninTMP;
            ninField.text = "Chief 3D Inspector";

            ui.reviewerNameInput = ninField;

            // Buttons
            GameObject fBtnRow = new GameObject("BtnRow");
            fBtnRow.transform.SetParent(fContent, false);
            RectTransform fbrRT = fBtnRow.AddComponent<RectTransform>();
            fbrRT.sizeDelta = new Vector2(0, 42);

            HorizontalLayoutGroup fbrHLG = fBtnRow.AddComponent<HorizontalLayoutGroup>();
            fbrHLG.spacing = 10;
            fbrHLG.childControlWidth = true;
            fbrHLG.childControlHeight = true;

            ui.confirmFinalizeBtn = CreateSimpleButton(fBtnRow.transform, "Approve & Finalize", font, UIProceduralTheme.GreenReady);
            ui.cancelFinalizeBtn = CreateSimpleButton(fBtnRow.transform, "Cancel", font, UIProceduralTheme.CardBg);

            fModal.SetActive(false);

            // Upload Progress Modal
            GameObject upModal = CreateModalBase(parent, "UploadModal", 380, 160);
            ui.uploadProgressModal = upModal;
            Transform upContent = upModal.transform.GetChild(0);

            GameObject upTitle = new GameObject("Title");
            upTitle.transform.SetParent(upContent, false);
            TextMeshProUGUI upTMP = upTitle.AddComponent<TextMeshProUGUI>();
            upTMP.font = font;
            upTMP.text = "Uploading Model...";
            upTMP.fontSize = 15;
            upTMP.fontStyle = FontStyles.Bold;
            upTMP.color = UIProceduralTheme.TextWhite;
            ui.uploadStatusText = upTMP;

            // Track & Bar
            GameObject track = new GameObject("ProgressTrack");
            track.transform.SetParent(upContent, false);
            RectTransform trRT = track.AddComponent<RectTransform>();
            trRT.sizeDelta = new Vector2(0, 12);
            Image trImg = track.AddComponent<Image>();
            trImg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 6, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            trImg.type = Image.Type.Sliced;

            GameObject bar = new GameObject("ProgressBar");
            bar.transform.SetParent(track.transform, false);
            RectTransform barRT = bar.AddComponent<RectTransform>();
            barRT.anchorMin = Vector2.zero;
            barRT.anchorMax = Vector2.one;
            barRT.sizeDelta = Vector2.zero;
            Image barImg = bar.AddComponent<Image>();
            barImg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 6, UIProceduralTheme.BluePrimary);
            barImg.type = Image.Type.Filled;
            barImg.fillMethod = Image.FillMethod.Horizontal;
            barImg.fillAmount = 0.5f;
            ui.uploadProgressBar = barImg;

            upModal.SetActive(false);
        }

        private static GameObject CreateModalBase(Transform parent, string name, float width, float height)
        {
            GameObject modal = new GameObject(name);
            modal.transform.SetParent(parent, false);
            RectTransform mRT = modal.AddComponent<RectTransform>();
            mRT.anchorMin = Vector2.zero;
            mRT.anchorMax = Vector2.one;
            mRT.sizeDelta = Vector2.zero;

            // Dim backdrop
            Image dim = modal.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.65f);

            GameObject box = new GameObject("Box");
            box.transform.SetParent(modal.transform, false);
            RectTransform bRT = box.AddComponent<RectTransform>();
            bRT.sizeDelta = new Vector2(width, height);

            Image bBg = box.AddComponent<Image>();
            bBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 14, UIProceduralTheme.SidebarBg, UIProceduralTheme.CardBorder, 1);
            bBg.type = Image.Type.Sliced;

            VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 14;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            return modal;
        }

        private static TextMeshProUGUI CreateSpecRow(Transform parent, string label, string value, TMP_FontAsset font)
        {
            GameObject row = new GameObject("Spec_" + label);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 24);
            LayoutElement rLE = row.AddComponent<LayoutElement>();
            rLE.minHeight = 24;
            rLE.preferredHeight = 24;
            rLE.flexibleHeight = 0;

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;

            GameObject lObj = new GameObject("Label");
            lObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI lTMP = lObj.AddComponent<TextMeshProUGUI>();
            lTMP.font = font;
            lTMP.text = label;
            lTMP.fontSize = 14;
            lTMP.alignment = TextAlignmentOptions.MidlineLeft;
            lTMP.color = UIProceduralTheme.TextMuted;

            GameObject vObj = new GameObject("Value");
            vObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI vTMP = vObj.AddComponent<TextMeshProUGUI>();
            vTMP.font = font;
            vTMP.text = value;
            vTMP.fontSize = 14;
            vTMP.fontStyle = FontStyles.Bold;
            vTMP.alignment = TextAlignmentOptions.MidlineRight;
            vTMP.color = UIProceduralTheme.TextWhite;

            return vTMP;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject div = new GameObject("Divider");
            div.transform.SetParent(parent, false);
            RectTransform rt = div.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 1);
            LayoutElement le = div.AddComponent<LayoutElement>();
            le.minHeight = 1;
            le.preferredHeight = 1;
            le.flexibleHeight = 0;

            Image img = div.AddComponent<Image>();
            img.color = UIProceduralTheme.CardBorder;
        }

        private static Button CreateIconToolButton(Transform parent, IconType iconType, float size, bool isActive)
        {
            GameObject btnObj = new GameObject("ToolBtn_" + iconType);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8,
                isActive ? UIProceduralTheme.BluePrimary : Color.clear,
                isActive ? UIProceduralTheme.BluePrimary : Color.clear,
                0);
            img.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform irt = iconObj.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.sizeDelta = new Vector2(-12, -12);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = UIProceduralTheme.GetIconSprite(iconType, Color.white, 32);

            return btn;
        }

        private static Button CreateBottomPillButton(Transform parent, IconType iconType, string label, float width, float height, TMP_FontAsset font, bool isActive)
        {
            GameObject btnObj = new GameObject("BottomBtn_" + label);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            Color bg = isActive ? UIProceduralTheme.BluePrimary : Color.clear;
            Color border = isActive ? UIProceduralTheme.BluePrimary : Color.clear;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, bg, border, 0);
            img.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();

            HorizontalLayoutGroup hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform irt = iconObj.AddComponent<RectTransform>();
            irt.sizeDelta = new Vector2(16, 16);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = UIProceduralTheme.GetIconSprite(iconType, Color.white, 24);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.sizeDelta = new Vector2(width - 32, height);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = label;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            return btn;
        }

        private static Button CreateHeaderButton(Transform parent, IconType iconType, string label, float width, float height, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject("HeaderBtn_" + label);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            img.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();

            HorizontalLayoutGroup hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform irt = iconObj.AddComponent<RectTransform>();
            irt.sizeDelta = new Vector2(16, 16);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = UIProceduralTheme.GetIconSprite(iconType, Color.white, 24);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.sizeDelta = new Vector2(width - 32, height);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = label;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            return btn;
        }

        private static Button CreateSimpleButton(Transform parent, string text, TMP_FontAsset font, Color bgCol)
        {
            GameObject btnObj = new GameObject("Btn_" + text);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, bgCol, UIProceduralTheme.CardBorder, 1);
            img.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static void CreatePillBadge(Transform parent, string text, TMP_FontAsset font)
        {
            GameObject badge = new GameObject("Badge_" + text);
            badge.transform.SetParent(parent, false);
            RectTransform rt = badge.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50, 24);

            Image img = badge.AddComponent<Image>();
            img.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 32, 6, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            img.type = Image.Type.Sliced;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(badge.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = 10;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UIProceduralTheme.TextMuted;
        }

        private static Sprite LoadLogoSprite()
        {
            string path = Path.Combine(Application.dataPath, "Resources/CC_Engineers_Logo.png");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            return null;
        }

        private static void CreateDropdownTemplate(GameObject dropdownGo, TMP_Dropdown dropdown, TMP_FontAsset font)
        {
            GameObject template = new GameObject("Template");
            template.transform.SetParent(dropdownGo.transform, false);
            RectTransform tRT = template.AddComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 0);
            tRT.anchorMax = new Vector2(1, 0);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.anchoredPosition = new Vector2(0, 0);
            tRT.sizeDelta = new Vector2(0, 200);

            Image tBg = template.AddComponent<Image>();
            tBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.SidebarBg, UIProceduralTheme.CardBorder, 1);
            tBg.type = Image.Type.Sliced;

            ScrollRect sr = template.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            RectTransform vRT = viewport.AddComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero;
            vRT.anchorMax = Vector2.one;
            vRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup cVLG = content.AddComponent<VerticalLayoutGroup>();
            cVLG.childControlWidth = true;
            cVLG.childControlHeight = true;
            cVLG.childForceExpandWidth = true;
            cVLG.childForceExpandHeight = false;

            ContentSizeFitter cCSF = content.AddComponent<ContentSizeFitter>();
            cCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vRT;
            sr.content = cRT;

            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            RectTransform iRT = item.AddComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.5f);
            iRT.anchorMax = new Vector2(1, 0.5f);
            iRT.sizeDelta = new Vector2(0, 32);
            Toggle toggle = item.AddComponent<Toggle>();
            
            LayoutElement iLE = item.AddComponent<LayoutElement>();
            iLE.minHeight = 32;
            iLE.preferredHeight = 32;

            GameObject itemBgGo = new GameObject("Item Background");
            itemBgGo.transform.SetParent(item.transform, false);
            RectTransform ibgRT = itemBgGo.AddComponent<RectTransform>();
            ibgRT.anchorMin = Vector2.zero;
            ibgRT.anchorMax = Vector2.one;
            ibgRT.sizeDelta = Vector2.zero;
            Image itemBg = itemBgGo.AddComponent<Image>();
            itemBg.color = UIProceduralTheme.CardSelected;
            toggle.targetGraphic = itemBg;

            GameObject itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);
            RectTransform ilRT = itemLabel.AddComponent<RectTransform>();
            ilRT.anchorMin = Vector2.zero;
            ilRT.anchorMax = Vector2.one;
            ilRT.offsetMin = new Vector2(14, 0);
            ilRT.offsetMax = new Vector2(-14, 0);
            TextMeshProUGUI iTMP = itemLabel.AddComponent<TextMeshProUGUI>();
            iTMP.font = font;
            iTMP.fontSize = 13;
            iTMP.alignment = TextAlignmentOptions.MidlineLeft;
            iTMP.color = UIProceduralTheme.TextWhite;

            dropdown.template = tRT;
            dropdown.itemText = iTMP;

            template.SetActive(false);
        }

        private static void SetupGizmo()
        {
#if UNITY_EDITOR
            GameObject gizmoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/RuntimeSceneGizmo/Prefabs/GizmoRenderer.prefab");
            if (gizmoPrefab != null)
            {
                Transform canvasTrans = GameObject.Find("ModelViewerCanvas")?.transform;
                if (canvasTrans != null)
                {
                    GameObject gizmoInstance = (GameObject)PrefabUtility.InstantiatePrefab(gizmoPrefab, canvasTrans);
                    RectTransform grRT = gizmoInstance.GetComponent<RectTransform>();
                    if (grRT != null)
                    {
                        grRT.anchorMin = new Vector2(1, 1);
                        grRT.anchorMax = new Vector2(1, 1);
                        grRT.pivot = new Vector2(1, 1);
                        grRT.anchoredPosition = new Vector2(-410, -140);
                        grRT.sizeDelta = new Vector2(90, 90);
                    }

                    RawImage rawImg = gizmoInstance.GetComponentInChildren<RawImage>();
                    if (rawImg != null)
                    {
                        rawImg.texture = UIProceduralTheme.GetIconSprite(IconType.AxisGizmo, null, 128).texture;
                        rawImg.color = Color.white;
                    }
                }
            }
#endif
        }
    }
}
