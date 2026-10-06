using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModelViewerApp
{
    public class ModelSelectionManager : MonoBehaviour
    {
        public static ModelSelectionManager Instance { get; private set; }

        public GameObject selectedObject = null;
        public string selectedObjectName = "None";
        public Bounds selectedObjectBounds;

        public event Action<GameObject, string> OnSelectionChanged;

        private Vector3 mouseDownPos;
        private float mouseDownTime;
        private const float CLICK_THRESHOLD = 5f; // pixels
        private const float CLICK_TIME_THRESHOLD = 0.3f; // seconds

        private Material highlightMaterial;
        private GameObject selectionIndicator;

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

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            highlightMaterial = new Material(unlitShader);
            highlightMaterial.color = new Color(0.2f, 0.7f, 1f, 0.4f);

            CreateSelectionIndicator();
        }

        private void CreateSelectionIndicator()
        {
            selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            selectionIndicator.name = "SelectionBoundingBox";
            Destroy(selectionIndicator.GetComponent<Collider>());

            Renderer r = selectionIndicator.GetComponent<Renderer>();
            r.material = highlightMaterial;
            selectionIndicator.SetActive(false);
        }

        private void Update()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                mouseDownPos = Input.mousePosition;
                mouseDownTime = Time.time;
            }

            // Only trigger selection on crisp click (not drag)
            if (Input.GetMouseButtonUp(0))
            {
                float dist = Vector3.Distance(Input.mousePosition, mouseDownPos);
                float duration = Time.time - mouseDownTime;

                if (dist < CLICK_THRESHOLD && duration < CLICK_TIME_THRESHOLD)
                {
                    PerformRaycastSelection();
                }
            }
        }

        private void PerformRaycastSelection()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Ensure colliders exist on the model
            EnsureCollidersOnCurrentModel();

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                GameObject hitObj = hit.collider.gameObject;
                SelectObject(hitObj, hit.point);
            }
            else
            {
                Deselect();
            }
        }

        public void SelectObject(GameObject target, Vector3 hitPoint)
        {
            selectedObject = target;
            selectedObjectName = target.name;

            // Compute bounds for the selected object
            Renderer rend = target.GetComponent<Renderer>();
            if (rend != null)
            {
                selectedObjectBounds = rend.bounds;
            }
            else
            {
                selectedObjectBounds = new Bounds(hitPoint, Vector3.one * 0.2f);
            }

            // Update bounding indicator
            if (selectionIndicator != null)
            {
                selectionIndicator.transform.position = selectedObjectBounds.center;
                selectionIndicator.transform.localScale = selectedObjectBounds.size * 1.02f;
                selectionIndicator.SetActive(true);
            }

            // Set dynamic pivot point to clicked spot
            if (ModelViewerCamera.Instance != null)
            {
                ModelViewerCamera.Instance.SetDynamicPivot(hitPoint);
            }

            OnSelectionChanged?.Invoke(selectedObject, selectedObjectName);
        }

        public void Deselect()
        {
            selectedObject = null;
            selectedObjectName = "None";

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            OnSelectionChanged?.Invoke(null, "None");
        }

        public void FocusSelected()
        {
            if (selectedObject != null && ModelViewerCamera.Instance != null)
            {
                ModelViewerCamera.Instance.FitModel(selectedObjectBounds);
            }
            else if (ModelDataManager.Instance != null)
            {
                var cur = ModelDataManager.Instance.GetCurrentModel();
                if (cur != null && ModelViewerCamera.Instance != null)
                {
                    ModelViewerCamera.Instance.FitModel(cur.modelBounds);
                }
            }
        }

        public void EnsureCollidersOnCurrentModel()
        {
            if (ModelDataManager.Instance == null) return;
            var currentModel = ModelDataManager.Instance.GetCurrentModel();
            if (currentModel == null || currentModel.modelObject == null) return;

            MeshFilter[] meshFilters = currentModel.modelObject.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }
    }
}
