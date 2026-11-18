using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Driftlight
{
    /// <summary>
    /// Command-line switches for builds:
    ///   -autoplay          the game plays itself (attract mode / automated playtest)
    ///   -capture &lt;folder&gt;  saves every frame as a JPG at a fixed 30 fps (for gameplay videos)
    /// Example: DRIFTLIGHT.app/Contents/MacOS/DRIFTLIGHT -autoplay -capture ~/frames
    /// </summary>
    public static class DemoMode
    {
        public static bool Autoplay { get; private set; }
        public static string CaptureFolder { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Autoplay = false;
            CaptureFolder = null;
            if (Application.isEditor) return;

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-autoplay") Autoplay = true;
                else if (args[i] == "-capture" && i + 1 < args.Length) CaptureFolder = args[++i];
            }
            if (!Autoplay && CaptureFolder == null) return;

            var go = new GameObject("Demo Mode");
            Object.DontDestroyOnLoad(go);
            if (Autoplay) go.AddComponent<DemoDirector>();
            if (CaptureFolder != null) go.AddComponent<FrameCapture>().folder = CaptureFolder;
        }
    }
}
