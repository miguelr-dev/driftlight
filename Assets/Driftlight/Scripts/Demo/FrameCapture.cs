using System.Collections;
using System.IO;
using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// Saves every rendered frame as a JPG. Game time is locked to the capture frame rate, so the
    /// frames play back smoothly at that rate however slowly they were rendered.
    /// Turn the frames into a video with: ffmpeg -framerate 30 -i frame_%05d.jpg -pix_fmt yuv420p gameplay.mp4
    /// </summary>
    public class FrameCapture : MonoBehaviour
    {
        public string folder = "Captures";
        public int frameRate = 30;
        [Range(1, 100)] public int jpgQuality = 90;

        int frame;

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            Time.captureFramerate = frameRate;
            Debug.Log($"[DEMO] Capturing frames to {Path.GetFullPath(folder)}");

            var endOfFrame = new WaitForEndOfFrame();
            Texture2D shot = null;
            while (true)
            {
                yield return endOfFrame;
                // At the end of the frame the screen holds the finished image, HUD included.
                int width = Screen.width, height = Screen.height;
                if (shot == null || shot.width != width || shot.height != height)
                {
                    if (shot != null) Destroy(shot);
                    shot = new Texture2D(width, height, TextureFormat.RGB24, false);
                }
                shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                shot.Apply(false);
                File.WriteAllBytes(Path.Combine(folder, $"frame_{frame:D5}.jpg"), shot.EncodeToJPG(jpgQuality));
                frame++;
            }
        }
    }
}
