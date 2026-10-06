using UnityEngine;

namespace RuntimeSceneGizmo
{
    public class MobileCameraMovement : MonoBehaviour
    {
        [System.Serializable]
        public class CameraView
        {
            public Vector3 position;
            public Vector3 rotation;
        }

        [Header("Camera")]
        public Camera mainCamera;

        [Header("Touch Settings")]
        [Range(0.01f, 1f)]
        public float rotationSensitivity = 0.05f;

        [Range(0.001f, 0.05f)]
        public float zoomSensitivity = 0.01f;

        [Header("Mouse Settings")]
        public float mouseRotationSensitivity = 0.1f;
        public float mouseZoomSensitivity = 1f;

        [Header("Zoom Limits")]
        public float minZoom = 2f;
        public float maxZoom = 15f;

        [Header("Touch Movement")]
        public float touchMovementThreshold = 2f;

        [Header("Camera Views")]
        public CameraView defaultView;

        private Vector2 previousTouchPosition;
        private Vector3 previousMousePosition;

        private float previousPinchDistance;

        private bool rotationInitialized;
        private bool pinchInitialized;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleTouchInput();

            if (Input.touchCount == 0)
            {
                HandleMouseInput();
            }
        }

        //=====================================================
        // TOUCH INPUT
        //=====================================================

        private void HandleTouchInput()
        {
            // No fingers
            if (Input.touchCount == 0)
            {
                rotationInitialized = false;
                pinchInitialized = false;
                return;
            }

            // One finger = Rotate
            if (Input.touchCount == 1)
            {
                pinchInitialized = false;
                RotateCameraTouch();
                return;
            }

            // Two fingers = Zoom
            if (Input.touchCount == 2)
            {
                rotationInitialized = false;
                ZoomCameraTouch();
                return;
            }

            // Ignore 3+ fingers
            rotationInitialized = false;
            pinchInitialized = false;
        }

        //=====================================================
        // ROTATION
        //=====================================================

        private void RotateCameraTouch()
        {
            Touch touch = Input.GetTouch(0);

            // First frame of touch
            if (!rotationInitialized)
            {
                previousTouchPosition = touch.position;
                rotationInitialized = true;
                return;
            }

            // Wait until finger actually drags
            if (touch.phase != TouchPhase.Moved)
            {
                previousTouchPosition = touch.position;
                return;
            }

            Vector2 delta =
                touch.position - previousTouchPosition;

            previousTouchPosition =
                touch.position;

            if (delta.magnitude < touchMovementThreshold)
                return;

            Rotate(delta, rotationSensitivity);
        }

        //=====================================================
        // PINCH ZOOM
        //=====================================================

        private void ZoomCameraTouch()
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            // Wait until at least one finger is actually moving
            if (touch1.phase != TouchPhase.Moved &&
                touch2.phase != TouchPhase.Moved)
            {
                return;
            }

            float currentDistance =
                Vector2.Distance(
                    touch1.position,
                    touch2.position);

            // First drag frame after entering pinch
            if (!pinchInitialized)
            {
                previousPinchDistance = currentDistance;
                pinchInitialized = true;
                return;
            }

            float delta =
                currentDistance - previousPinchDistance;

            previousPinchDistance =
                currentDistance;

            if (Mathf.Abs(delta) < 1f)
                return;

            Zoom(delta * zoomSensitivity);
        }
        //=====================================================
        // WINDOWS MOUSE INPUT
        //=====================================================

        private void HandleMouseInput()
        {
            // ==========================================
            // LEFT MOUSE BUTTON - ROTATE
            // ==========================================

            if (Input.GetMouseButtonDown(0))
            {
                previousMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 currentMousePosition =
                    Input.mousePosition;

                Vector2 delta =
                    currentMousePosition -
                    previousMousePosition;

                previousMousePosition =
                    currentMousePosition;

                // Rotate only when mouse is dragged
                if (delta.magnitude >= touchMovementThreshold)
                {
                    Rotate(
                        delta,
                        mouseRotationSensitivity
                    );
                }
            }


            // ==========================================
            // MOUSE SCROLL - ZOOM IN / OUT
            // ==========================================

            float scroll =
                Input.mouseScrollDelta.y;

            if (scroll != 0f)
            {
                // Scroll UP   = Zoom In
                // Scroll DOWN = Zoom Out

                Zoom(
                    scroll * mouseZoomSensitivity
                );
            }
        }

        //=====================================================
        // CAMERA ROTATION
        //=====================================================

        private void Rotate(
            Vector2 delta,
            float sensitivity)
        {
            Vector3 rotation =
                transform.localEulerAngles;

            if (rotation.x > 180f)
                rotation.x -= 360f;

            rotation.x -=
                delta.y * sensitivity;

            rotation.y +=
                delta.x * sensitivity;

            rotation.x =
                Mathf.Clamp(
                    rotation.x,
                    -89.8f,
                    89.8f);

            rotation.z = 0f;

            transform.localEulerAngles =
                rotation;
        }

        //=====================================================
        // CAMERA ZOOM
        //=====================================================

        private void Zoom(float amount)
        {
            Vector3 position =
                mainCamera.transform.localPosition;

            position.z += amount;

            position.z =
                Mathf.Clamp(
                    position.z,
                    -maxZoom,
                    -minZoom);

            mainCamera.transform.localPosition =
                position;
        }


        //=====================================================
        // RESET CAMERA
        //=====================================================

        public void ResetCamera()
        {
            if (defaultView == null)
                return;

            transform.localPosition =
                defaultView.position;

            transform.localEulerAngles =
                defaultView.rotation;

            rotationInitialized = false;
            pinchInitialized = false;
        }
    }
}