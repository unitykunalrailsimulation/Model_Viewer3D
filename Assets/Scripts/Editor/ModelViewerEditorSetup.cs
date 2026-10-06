using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModelViewerApp.Editor
{
    [InitializeOnLoad]
    public static class ModelViewerEditorSetup
    {
        private const string SETUP_SESSION_KEY = "ModelViewer2_Setup_Version_v6";

        static ModelViewerEditorSetup()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (Application.isPlaying) return;

            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name == "3Dmodelviewer2.0" || activeScene.name == "3DmodelViewer")
            {
                if (!SessionState.GetBool(SETUP_SESSION_KEY, false))
                {
                    SessionState.SetBool(SETUP_SESSION_KEY, true);
                    Debug.Log($"[ModelViewerEditorSetup] Automatically rebuilding scene '{activeScene.name}' to match target design...");
                    ModelViewerSceneBuilder.BuildFullScene();
                    EditorSceneManager.SaveScene(activeScene);
                    Debug.Log($"[ModelViewerEditorSetup] Scene '{activeScene.name}' successfully rebuilt and saved!");
                }
            }
        }

        [MenuItem("Tools/Model Viewer/Build & Setup Scene (3D Model Viewer 2.0)", false, 1)]
        public static void ForceBuildScene()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[ModelViewerEditorSetup] Force building scene '{activeScene.name}'...");
            ModelViewerSceneBuilder.BuildFullScene();
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[ModelViewerEditorSetup] Scene successfully built and saved!");
        }

        [MenuItem("Tools/Model Viewer/Select Active Model in Hierarchy", false, 2)]
        public static void SelectActiveModel()
        {
            GameObject container = GameObject.Find("ModelsContainer");
            if (container != null)
            {
                Selection.activeGameObject = container;
            }
        }
    }
}
