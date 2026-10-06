using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModelViewerApp
{
    public enum ReviewItemStatus
    {
        Pending,
        Passed,
        CorrectionRequired
    }

    public enum ModelStatus
    {
        NotReviewed,
        UnderReview,
        CorrectionRequired,
        Finalized
    }

    [Serializable]
    public class ReviewComment
    {
        public string author = "Reviewer";
        public string commentText = "";
        public string timestamp = "";

        public ReviewComment() { }

        public ReviewComment(string author, string text)
        {
            this.author = author;
            this.commentText = text;
            this.timestamp = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");
        }
    }

    [Serializable]
    public class ChecklistItem
    {
        public string title;
        public string subtitle;
        public ReviewItemStatus status;

        public ChecklistItem(string title, string subtitle, ReviewItemStatus initialStatus = ReviewItemStatus.Pending)
        {
            this.title = title;
            this.subtitle = subtitle;
            this.status = initialStatus;
        }
    }

    [Serializable]
    public class ModelItem
    {
        public string id;
        public string modelName;
        public string fileName;
        public string fileFormat; // e.g. ".fbx", ".glb", ".obj", ".gltf"
        public string fileSize;   // e.g. "24.8 MB"
        public string uploadDate; // e.g. "12 Sep 2025"
        
        public GameObject modelObject;
        public Texture2D thumbnailTexture;
        public Sprite thumbnailSprite;

        public ModelStatus status = ModelStatus.NotReviewed;
        public string finalizedDate = "";
        public string reviewerName = "";

        public List<ChecklistItem> checklist = new List<ChecklistItem>();
        public List<ReviewComment> comments = new List<ReviewComment>();

        // Calculated 3D statistics
        public int vertexCount = 0;
        public int triangleCount = 0;
        public Vector3 dimensions = Vector3.zero;
        public int materialCount = 0;
        public int textureCount = 0;
        public Bounds modelBounds;

        public ModelItem()
        {
            id = Guid.NewGuid().ToString();
            InitDefaultChecklist();
        }

        public void InitDefaultChecklist()
        {
            checklist = new List<ChecklistItem>
            {
                new ChecklistItem("Model Geometry", "No mesh errors / missing faces", ReviewItemStatus.Passed),
                new ChecklistItem("Textures & Materials", "All materials assigned correctly", ReviewItemStatus.Passed),
                new ChecklistItem("Textures Resolution", "Optimal quality", ReviewItemStatus.Passed),
                new ChecklistItem("Naming Convention", "As per project standards", ReviewItemStatus.Passed),
                new ChecklistItem("Overall Visual Quality", "Looks correct and complete", ReviewItemStatus.Passed)
            };
        }

        public void RecalculateStats()
        {
            if (modelObject == null) return;

            MeshFilter[] meshFilters = modelObject.GetComponentsInChildren<MeshFilter>(true);
            SkinnedMeshRenderer[] skinnedMeshes = modelObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Renderer[] renderers = modelObject.GetComponentsInChildren<Renderer>(true);

            int verts = 0;
            int tris = 0;

            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    verts += mf.sharedMesh.vertexCount;
                    tris += mf.sharedMesh.triangles.Length / 3;
                }
            }

            foreach (var smr in skinnedMeshes)
            {
                if (smr.sharedMesh != null)
                {
                    verts += smr.sharedMesh.vertexCount;
                    tris += smr.sharedMesh.triangles.Length / 3;
                }
            }

            vertexCount = verts;
            triangleCount = tris;

            // Compute combined bounds
            bool hasBounds = false;
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);

            HashSet<Material> uniqueMaterials = new HashSet<Material>();
            HashSet<Texture> uniqueTextures = new HashSet<Texture>();

            foreach (var rend in renderers)
            {
                if (rend is ParticleSystemRenderer) continue;

                if (!hasBounds)
                {
                    b = rend.bounds;
                    hasBounds = true;
                }
                else
                {
                    b.Encapsulate(rend.bounds);
                }

                foreach (var mat in rend.sharedMaterials)
                {
                    if (mat != null)
                    {
                        uniqueMaterials.Add(mat);

                        // Collect textures from material
                        Shader shader = mat.shader;
                        if (shader != null)
                        {
                            int propCount = shader.GetPropertyCount();
                            for (int i = 0; i < propCount; i++)
                            {
                                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                                {
                                    string propName = shader.GetPropertyName(i);
                                    Texture tex = mat.GetTexture(propName);
                                    if (tex != null)
                                    {
                                        uniqueTextures.Add(tex);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            modelBounds = b;
            dimensions = b.size;
            materialCount = uniqueMaterials.Count;
            textureCount = uniqueTextures.Count;
        }

        public void UpdateOverallStatus()
        {
            if (status == ModelStatus.Finalized) return;

            bool hasCorrection = false;
            bool hasPending = false;
            bool allPassed = true;

            foreach (var item in checklist)
            {
                if (item.status == ReviewItemStatus.CorrectionRequired)
                {
                    hasCorrection = true;
                    allPassed = false;
                }
                else if (item.status == ReviewItemStatus.Pending)
                {
                    hasPending = true;
                    allPassed = false;
                }
            }

            if (hasCorrection)
            {
                status = ModelStatus.CorrectionRequired;
            }
            else if (allPassed && checklist.Count > 0)
            {
                // Can be marked as finalized
                status = ModelStatus.UnderReview;
            }
            else if (hasPending)
            {
                status = ModelStatus.UnderReview;
            }
            else
            {
                status = ModelStatus.NotReviewed;
            }
        }
    }

    public class ModelDataManager : MonoBehaviour
    {
        public static ModelDataManager Instance { get; private set; }

        public List<ModelItem> models = new List<ModelItem>();
        public int currentModelIndex = 0;

        public event Action<ModelItem> OnModelSelected;
        public event Action OnModelListChanged;
        public event Action<ModelItem> OnModelDataUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        public ModelItem GetCurrentModel()
        {
            if (models.Count == 0) return null;
            if (currentModelIndex < 0 || currentModelIndex >= models.Count)
                currentModelIndex = 0;
            return models[currentModelIndex];
        }

        public void SelectModel(int index)
        {
            if (index < 0 || index >= models.Count) return;

            currentModelIndex = index;

            for (int i = 0; i < models.Count; i++)
            {
                if (models[i].modelObject != null)
                {
                    models[i].modelObject.SetActive(i == currentModelIndex);
                }
            }

            ModelItem selected = models[currentModelIndex];
            OnModelSelected?.Invoke(selected);
        }

        public void AddModel(ModelItem item, bool selectImmediately = true)
        {
            models.Add(item);
            item.RecalculateStats();
            OnModelListChanged?.Invoke();

            if (selectImmediately)
            {
                SelectModel(models.Count - 1);
            }
        }

        public void DeleteModel(int index)
        {
            if (index < 0 || index >= models.Count) return;

            ModelItem item = models[index];
            if (item.modelObject != null)
            {
                Destroy(item.modelObject);
            }

            models.RemoveAt(index);

            if (models.Count > 0)
            {
                currentModelIndex = Mathf.Clamp(currentModelIndex, 0, models.Count - 1);
                SelectModel(currentModelIndex);
            }
            else
            {
                currentModelIndex = -1;
                OnModelSelected?.Invoke(null);
            }

            OnModelListChanged?.Invoke();
        }

        public void NotifyModelDataUpdated(ModelItem item)
        {
            OnModelDataUpdated?.Invoke(item);
        }

        public void FinalizeModel(string id, string reviewer)
        {
            ModelItem item = models.Find(m => m.id == id);
            if (item != null)
            {
                item.status = ModelStatus.Finalized;
                item.reviewerName = reviewer;
                item.finalizedDate = DateTime.Now.ToString("dd MMM yyyy");
                foreach (var c in item.checklist)
                {
                    c.status = ReviewItemStatus.Passed;
                }
                NotifyModelDataUpdated(item);
                OnModelListChanged?.Invoke();
            }
        }

        public void AddComment(string id, string author, string text)
        {
            ModelItem item = models.Find(m => m.id == id);
            if (item != null)
            {
                item.comments.Add(new ReviewComment(author, text));
                NotifyModelDataUpdated(item);
            }
        }

        public void UploadModelFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;

            string ext = System.IO.Path.GetExtension(filePath).ToLower();
            string name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            long sizeBytes = new System.IO.FileInfo(filePath).Length;
            string sizeStr = $"{(sizeBytes / (1024f * 1024f)):F1} MB";

            GameObject loadedGo = null;
            if (ext == ".obj")
            {
                loadedGo = RuntimeObjLoader.LoadOBJFile(filePath);
            }
            else
            {
#if UNITY_EDITOR
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
                if (prefab != null)
                {
                    loadedGo = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                }
#endif
            }

            if (loadedGo == null)
            {
                loadedGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            loadedGo.name = name;
            GameObject modelsRoot = GameObject.Find("ModelsContainer");
            if (modelsRoot != null) loadedGo.transform.SetParent(modelsRoot.transform);

            ThumbnailGenerator tg = ThumbnailGenerator.Instance ?? FindFirstObjectByType<ThumbnailGenerator>();
            Sprite thumb = tg != null ? tg.GenerateThumbnail(loadedGo) : null;

            ModelItem item = new ModelItem
            {
                modelName = name,
                fileName = System.IO.Path.GetFileName(filePath),
                fileFormat = ext,
                fileSize = sizeStr,
                uploadDate = DateTime.Now.ToString("dd MMM yyyy"),
                modelObject = loadedGo,
                thumbnailSprite = thumb,
                status = ModelStatus.NotReviewed
            };

            AddModel(item, true);
        }
    }
}
