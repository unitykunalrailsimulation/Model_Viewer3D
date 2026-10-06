using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModelViewerApp
{
    public enum ViewerInteractionMode
    {
        Orbit,
        Pan,
        Zoom
    }

    public enum CameraViewPreset
    {
        Default,
        Front,
        Back,
        Left,
        Right,
        Top,
        Bottom,
        Isometric
    }

    public class ModelViewerCamera : MonoBehaviour
    {
        public static ModelViewerCamera Instance { get; private set; }

        [Header("Camera & Target")]
        public Camera targetCamera;
        public Vector3 currentPivot = Vector3.zero;
        public Vector3 defaultPivot = Vector3.zero;

        [Header("Distance Limits")]
        public float distance = 4.5f;
        public float defaultDistance = 4.5f;
        public float minDistance = 0.2f;
        public float maxDistance = 30f;

        [Header("Rotation Settings")]
        public float yaw = 35f;
        public float pitch = 20f;
        public float defaultYaw = 35f;
        public float defaultPitch = 20f;
        public float minPitch = -89.5f;
        public float maxPitch = 89.5f;

        [Header("Sensitivity")]
        public float orbitSensitivity = 3.5f;
        public float panSensitivity = 0.005f;
        public float zoomSensitivity = 1.2f;
        public float smoothDampTime = 0.08f;

        [Header("Current Mode")]
        public ViewerInteractionMode currentMode = ViewerInteractionMode.Orbit;

        // Smooth damping targets
        private float targetYaw;
        private float targetPitch;
        private float targetDistance;
        private Vector3 targetPivot;

        private float yawVelocity;
        private float pitchVelocity;
        private float distanceVelocity;
        private Vector3 pivotVelocity;

        private Vector3 lastMousePos;
        private bool isInteracting = false;
        private bool isTransitioningPreset = false;

        // Visual pivot marker
        private GameObject pivotMarker;
        private float markerFadeTimer = 0f;

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

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;
            targetPivot = currentPivot;

            CreatePivotMarker();
        }

        private void Start()
        {
            SetViewPreset(CameraViewPreset.Isometric, immediate: true);
        }

        private void CreatePivotMarker()
        {
            pivotMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pivotMarker.name = "DynamicPivotMarker";
            Destroy(pivotMarker.GetComponent<Collider>());
            
            // Set unlit cyan glowing color
            Renderer r = pivotMarker.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            mat.color = new Color(0.15f, 0.65f, 1f, 0.85f);
            r.material = mat;
            
            pivotMarker.transform.localScale = Vector3.one * 0.05f;
            pivotMarker.SetActive(false);
        }

        private void Update()
        {
            HandleInput();
            UpdateCameraTransform();
            UpdatePivotMarker();
        }

        private void HandleInput()
        {
            // Do not interact if mouse is over UI elements (buttons, sidebars)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (!isInteracting) return;
            }

            // Mouse wheel zoom works anywhere if not on scrollable UI
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                Zoom(scroll * zoomSensitivity * (targetDistance * 0.5f + 1f));
            }

            // Left click down
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                lastMousePos = Input.mousePosition;
                isInteracting = true;
            }

            if (Input.GetMouseButtonUp(0) && Input.GetMouseButtonUp(1) && Input.GetMouseButtonUp(2))
            {
                isInteracting = false;
            }

            if (isInteracting)
            {
                Vector3 delta = Input.mousePosition - lastMousePos;
                lastMousePos = Input.mousePosition;

                // Middle Mouse or Shift + Left Mouse or Pan Mode = PAN
                if (Input.GetMouseButton(2) || (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftShift) || currentMode == ViewerInteractionMode.Pan)))
                {
                    Pan(delta);
                }
                // Right Mouse or Zoom Mode = ZOOM
                else if (Input.GetMouseButton(1) || (Input.GetMouseButton(0) && currentMode == ViewerInteractionMode.Zoom))
                {
                    Zoom(delta.y * 0.02f * zoomSensitivity * (targetDistance * 0.3f + 0.5f));
                }
                // Left Mouse = ORBIT
                else if (Input.GetMouseButton(0) && currentMode == ViewerInteractionMode.Orbit)
                {
                    Orbit(delta);
                }
            }
        }

        public void Orbit(Vector2 delta)
        {
            if (isTransitioningPreset) isTransitioningPreset = false;

            targetYaw += delta.x * orbitSensitivity * 0.1f;
            targetPitch -= delta.y * orbitSensitivity * 0.1f;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        public void Pan(Vector2 delta)
        {
            if (isTransitioningPreset) isTransitioningPreset = false;

            // Pan in camera plane
            float factor = targetDistance * panSensitivity;
            Vector3 right = targetCamera.transform.right;
            Vector3 up = targetCamera.transform.up;

            targetPivot -= (right * delta.x + up * delta.y) * factor;
        }

        public void Zoom(float delta)
        {
            if (isTransitioningPreset) isTransitioningPreset = false;

            targetDistance -= delta;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        public void ZoomIn()
        {
            Zoom(targetDistance * 0.2f);
        }

        public void ZoomOut()
        {
            Zoom(-targetDistance * 0.2f);
        }

        public void SetInteractionMode(ViewerInteractionMode mode)
        {
            currentMode = mode;
        }

        public void SetDynamicPivot(Vector3 newPivot)
        {
            targetPivot = newPivot;

            // Show pivot marker
            if (pivotMarker != null)
            {
                pivotMarker.transform.position = newPivot;
                pivotMarker.SetActive(true);
                pivotMarker.transform.localScale = Vector3.one * (targetDistance * 0.035f);
                markerFadeTimer = 1.5f;
            }
        }

        private void UpdatePivotMarker()
        {
            if (pivotMarker == null || !pivotMarker.activeSelf) return;

            markerFadeTimer -= Time.deltaTime;
            if (markerFadeTimer <= 0f)
            {
                pivotMarker.SetActive(false);
            }
            else
            {
                // Pulse scale
                float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 12f);
                pivotMarker.transform.localScale = Vector3.one * (targetDistance * 0.035f * pulse);
            }
        }

        public void FitModel(Bounds bounds, bool animate = true)
        {
            Vector3 center = bounds.center;
            targetPivot = center;

            float fov = targetCamera.fieldOfView * Mathf.Deg2Rad;
            float aspect = targetCamera.aspect > 0.1f ? targetCamera.aspect : 1.777f;

            float vertDist = (bounds.extents.y * 1.18f) / Mathf.Tan(fov * 0.5f);
            float horizDist = (bounds.extents.x * 1.18f) / (Mathf.Tan(fov * 0.5f) * aspect);
            float depthDist = bounds.extents.z * 1.2f;

            float fitDistance = Mathf.Max(Mathf.Max(vertDist, horizDist), depthDist);
            if (fitDistance < 0.5f) fitDistance = 2.85f;

            targetDistance = Mathf.Clamp(fitDistance, minDistance, maxDistance);

            defaultPivot = center;
            defaultDistance = targetDistance;

            if (!animate)
            {
                currentPivot = targetPivot;
                distance = targetDistance;
            }
        }

        public void ResetView()
        {
            targetYaw = defaultYaw;
            targetPitch = defaultPitch;
            targetDistance = defaultDistance;
            targetPivot = defaultPivot;
            isTransitioningPreset = false;

            if (targetCamera.orthographic)
            {
                targetCamera.orthographic = false;
            }
        }

        public void SetViewPreset(CameraViewPreset preset, bool immediate = false)
        {
            float newYaw = yaw;
            float newPitch = pitch;

            switch (preset)
            {
                case CameraViewPreset.Default:
                case CameraViewPreset.Isometric:
                    newYaw = 45f;
                    newPitch = 35.264f;
                    break;
                case CameraViewPreset.Front:
                    newYaw = 180f;
                    newPitch = 0f;
                    break;
                case CameraViewPreset.Back:
                    newYaw = 0f;
                    newPitch = 0f;
                    break;
                case CameraViewPreset.Left:
                    newYaw = 90f;
                    newPitch = 0f;
                    break;
                case CameraViewPreset.Right:
                    newYaw = -90f;
                    newPitch = 0f;
                    break;
                case CameraViewPreset.Top:
                    newYaw = 0f;
                    newPitch = 89.9f;
                    break;
                case CameraViewPreset.Bottom:
                    newYaw = 0f;
                    newPitch = -89.9f;
                    break;
            }

            targetYaw = newYaw;
            targetPitch = newPitch;

            if (immediate)
            {
                yaw = targetYaw;
                pitch = targetPitch;
                currentPivot = targetPivot;
                distance = targetDistance;
            }
        }

        public void ToggleOrthographic()
        {
            StartCoroutine(TransitionProjection(!targetCamera.orthographic));
        }

        private IEnumerator TransitionProjection(bool makeOrtho)
        {
            targetCamera.orthographic = makeOrtho;
            if (makeOrtho)
            {
                targetCamera.orthographicSize = targetDistance * 0.5f;
            }
            yield return null;
        }

        private void UpdateCameraTransform()
        {
            // Smoothly damp pitch, yaw, distance, and pivot
            yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVelocity, smoothDampTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, smoothDampTime);
            distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, smoothDampTime);
            currentPivot = Vector3.SmoothDamp(currentPivot, targetPivot, ref pivotVelocity, smoothDampTime);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

            transform.position = currentPivot + offset;
            transform.rotation = rotation;

            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = distance * 0.45f;
            }
        }
    }
}
