using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Driftlight.EditorTools
{
    /// <summary>
    /// Renders scenes through their main camera (with post-processing) into docs/screenshots.
    /// Menu: DRIFTLIGHT > Save Screenshot of Open Scene.
    /// Batch: -executeMethod Driftlight.EditorTools.DriftlightScreenshots.CaptureAll
    /// </summary>
    public static class DriftlightScreenshots
    {
        const string Folder = "docs/screenshots";

        [MenuItem("DRIFTLIGHT/Save Screenshot of Open Scene", priority = 50)]
        static void CaptureOpenScene()
        {
            string path = Capture(EditorSceneManager.GetActiveScene().name, 1920, 1080);
            if (path != null) EditorUtility.RevealInFinder(path);
        }

        /// <summary>Regenerates every scene, then screenshots each one (batch mode helper).</summary>
        public static void BuildAllAndCapture()
        {
            DriftlightBuilder.BuildAll();
            CaptureAll();
        }

        /// <summary>Opens every scene in the build list and saves a screenshot of each.</summary>
        public static void CaptureAll()
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                Capture(Path.GetFileNameWithoutExtension(scene.path), 1920, 1080);
            }
        }

        public static string Capture(string name, int width, int height)
        {
            var cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogWarning($"[DRIFTLIGHT] No camera to capture in {name}.");
                return null;
            }

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                RenderPipeline.SubmitRenderRequest(cam, request);
            }
            else
            {
                var previous = cam.targetTexture;
                cam.targetTexture = target;
                cam.Render();
                cam.targetTexture = previous;
            }

            var active = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = active;

            Directory.CreateDirectory(Folder);
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
            Debug.Log($"[DRIFTLIGHT] Saved {path}");
            return path;
        }
    }
}
