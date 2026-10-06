using System.Collections.Generic;
using UnityEngine;

namespace ModelViewerApp
{
    public enum ModelViewMode
    {
        Shaded,
        Wireframe,
        XRay
    }

    public class ModelMaterialController : MonoBehaviour
    {
        public static ModelMaterialController Instance { get; private set; }

        public ModelViewMode currentMode = ModelViewMode.Shaded;

        private Material xrayMaterial;
        private Material wireframeMaterial;
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private GameObject currentModelRoot;
        private List<GameObject> generatedWireframeObjects = new List<GameObject>();

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

            InitMaterials();
        }

        private void InitMaterials()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

            // X-Ray Material (Holographic Cyan Semi-Transparent)
            xrayMaterial = new Material(unlitShader);
            xrayMaterial.name = "XRay_Material";
            xrayMaterial.color = new Color(0.1f, 0.75f, 1f, 0.35f);

            // Configure transparent mode in URP if available
            xrayMaterial.SetFloat("_Surface", 1); // 1 = Transparent
            xrayMaterial.SetFloat("_Blend", 0);   // 0 = Alpha blend
            xrayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            xrayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            xrayMaterial.SetInt("_ZWrite", 0);
            xrayMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Wireframe Material
            wireframeMaterial = new Material(unlitShader);
            wireframeMaterial.name = "Wireframe_Line_Material";
            wireframeMaterial.color = new Color(0.2f, 0.85f, 1f, 0.95f);
        }

        public void RegisterModel(GameObject modelRoot)
        {
            currentModelRoot = modelRoot;
            ClearGeneratedWireframes();
            originalMaterials.Clear();

            if (modelRoot == null) return;

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                if (rend is ParticleSystemRenderer) continue;
                originalMaterials[rend] = rend.sharedMaterials;
            }

            // Apply current mode
            SetViewMode(currentMode);
        }

        public void SetViewMode(ModelViewMode mode)
        {
            currentMode = mode;
            if (currentModelRoot == null) return;

            ClearGeneratedWireframes();

            switch (mode)
            {
                case ModelViewMode.Shaded:
                    RestoreOriginalMaterials();
                    break;

                case ModelViewMode.XRay:
                    ApplyXRayMaterials();
                    break;

                case ModelViewMode.Wireframe:
                    ApplyWireframeMode();
                    break;
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    kvp.Key.sharedMaterials = kvp.Value;
                    kvp.Key.enabled = true;
                }
            }
        }

        private void ApplyXRayMaterials()
        {
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null)
                {
                    Material[] xrays = new Material[kvp.Value.Length];
                    for (int i = 0; i < xrays.Length; i++)
                    {
                        xrays[i] = xrayMaterial;
                    }
                    kvp.Key.sharedMaterials = xrays;
                    kvp.Key.enabled = true;
                }
            }
        }

        private void ApplyWireframeMode()
        {
            // First apply a dark ghost base so wireframe lines stand out
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material darkBaseMat = new Material(unlitShader);
            darkBaseMat.color = new Color(0.05f, 0.08f, 0.14f, 0.85f);
            darkBaseMat.SetFloat("_Surface", 1);
            darkBaseMat.SetInt("_ZWrite", 1);

            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null)
                {
                    Material[] darks = new Material[kvp.Value.Length];
                    for (int i = 0; i < darks.Length; i++)
                    {
                        darks[i] = darkBaseMat;
                    }
                    kvp.Key.sharedMaterials = darks;
                }
            }

            // Generate wireframe line meshes from MeshFilters
            MeshFilter[] meshFilters = currentModelRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                Mesh originalMesh = mf.sharedMesh;
                Mesh wireMesh = CreateWireframeMesh(originalMesh);
                if (wireMesh != null)
                {
                    GameObject wireObj = new GameObject(mf.gameObject.name + "_Wireframe");
                    wireObj.transform.SetParent(mf.transform, false);
                    wireObj.transform.localPosition = Vector3.zero;
                    wireObj.transform.localRotation = Quaternion.identity;
                    wireObj.transform.localScale = Vector3.one;

                    MeshFilter wfMF = wireObj.AddComponent<MeshFilter>();
                    wfMF.sharedMesh = wireMesh;

                    MeshRenderer wfMR = wireObj.AddComponent<MeshRenderer>();
                    wfMR.sharedMaterial = wireframeMaterial;

                    generatedWireframeObjects.Add(wireObj);
                }
            }
        }

        private Mesh CreateWireframeMesh(Mesh mesh)
        {
            if (mesh == null) return null;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            if (triangles == null || triangles.Length == 0) return null;

            List<int> lineIndices = new List<int>(triangles.Length * 2);
            HashSet<long> uniqueEdges = new HashSet<long>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                AddEdge(triangles[i], triangles[i + 1], uniqueEdges, lineIndices);
                AddEdge(triangles[i + 1], triangles[i + 2], uniqueEdges, lineIndices);
                AddEdge(triangles[i + 2], triangles[i], uniqueEdges, lineIndices);
            }

            Mesh linesMesh = new Mesh();
            linesMesh.name = mesh.name + "_WireLines";
            linesMesh.vertices = vertices;
            linesMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);

            return linesMesh;
        }

        private void AddEdge(int a, int b, HashSet<long> edges, List<int> indices)
        {
            int min = Mathf.Min(a, b);
            int max = Mathf.Max(a, b);
            long key = ((long)min << 32) | (uint)max;

            if (edges.Add(key))
            {
                indices.Add(min);
                indices.Add(max);
            }
        }

        private void ClearGeneratedWireframes()
        {
            foreach (var go in generatedWireframeObjects)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            generatedWireframeObjects.Clear();
        }
    }
}
