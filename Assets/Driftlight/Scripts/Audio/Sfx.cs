using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Driftlight
{
    /// <summary>
    /// Sound effects synthesised in code the first time they're used, so the game has sound before
    /// we add recorded audio. Swap any of these for a real AudioClip later.
    /// </summary>
    public static class Sfx
    {
        const int SampleRate = 44100;

        static AudioClip playerShot, enemyShot, impact, explosion, warpIn, alarm, shieldHit, hullHit;

        public static AudioClip PlayerShot => playerShot != null ? playerShot : playerShot = Build("Player Shot", 0.16f, d => Laser(d, 1700f, 380f, 20f, 0.35f));
        public static AudioClip EnemyShot => enemyShot != null ? enemyShot : enemyShot = Build("Enemy Shot", 0.22f, d => Laser(d, 900f, 170f, 13f, 0.75f));
        public static AudioClip Impact => impact != null ? impact : impact = Build("Impact", 0.12f, d => FilteredNoise(d, 40f, 0.8f, 3));
        public static AudioClip Explosion => explosion != null ? explosion : explosion = Build("Explosion", 1.6f, Boom);
        public static AudioClip WarpIn => warpIn != null ? warpIn : warpIn = Build("Warp In", 0.7f, d => Whoosh(d, 220f, 1500f, 0.45f, 5));
        public static AudioClip Alarm => alarm != null ? alarm : alarm = Build("Alarm", 0.9f, AlarmTones);
        public static AudioClip ShieldHit => shieldHit != null ? shieldHit : shieldHit = Build("Shield Hit", 0.25f, d => Laser(d, 2400f, 900f, 18f, 0.1f));
        public static AudioClip HullHit => hullHit != null ? hullHit : hullHit = Build("Hull Hit", 0.3f, d => FilteredNoise(d, 16f, 0.25f, 7));

        /// <summary>Plays a clip in the world (mostly 3D, so it pans with position).</summary>
        public static void Play(AudioClip clip, Vector3 position, float volume = 1f) => Spawn(clip, position, volume, 0.75f);

        /// <summary>Plays a clip at full volume regardless of position (UI, the player's own ship).</summary>
        public static void Play2D(AudioClip clip, float volume = 1f)
        {
            var cam = Camera.main;
            Spawn(clip, cam ? cam.transform.position : Vector3.zero, volume, 0f);
        }

        static void Spawn(AudioClip clip, Vector3 position, float volume, float spatialBlend)
        {
            if (clip == null) return;
            var go = new GameObject("SFX " + clip.name);
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.minDistance = 30f;
            source.maxDistance = 700f;
            source.dopplerLevel = 0f;
            source.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            source.Play();
            Object.Destroy(go, clip.length / source.pitch + 0.1f);
        }

        // ------------------------------------------------------------------ synthesis

        static AudioClip Build(string name, float seconds, Action<float[]> fill)
        {
            int length = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[length];
            fill(data);

            // Short fades at both ends prevent clicks.
            int fade = Mathf.Min(96, length / 4);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[length - 1 - i] *= k;
            }

            var clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return clip;
        }

        static void Laser(float[] d, float startHz, float endHz, float decay, float squareMix)
        {
            double phase = 0;
            float duration = d.Length / (float)SampleRate;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                float hz = Mathf.Lerp(startHz, endHz, Mathf.Sqrt(t / duration));
                phase += hz / SampleRate;
                float s = Mathf.Sin((float)(phase * 2.0 * Math.PI));
                d[i] = Mathf.Lerp(s, Mathf.Sign(s), squareMix) * 0.5f * Mathf.Exp(-t * decay);
            }
        }

        static void FilteredNoise(float[] d, float decay, float brightness, int seed)
        {
            var random = new System.Random(seed);
            float low = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = (float)(random.NextDouble() * 2.0 - 1.0);
                low += (n - low) * brightness;
                d[i] = low * Mathf.Exp(-t * decay) * 0.9f;
            }
        }

        static void Boom(float[] d)
        {
            var random = new System.Random(11);
            float low1 = 0f, low2 = 0f;
            float duration = d.Length / (float)SampleRate;
            double phase = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = (float)(random.NextDouble() * 2.0 - 1.0);
                float cutoff = Mathf.Lerp(0.35f, 0.015f, Mathf.Clamp01(t / (duration * 0.8f)));
                low1 += (n - low1) * cutoff;
                low2 += (low1 - low2) * cutoff;
                float rumble = low2 * 2.8f * Mathf.Exp(-t * 2.2f);

                phase += Mathf.Lerp(65f, 32f, Mathf.Clamp01(t * 2f)) / SampleRate;
                float thump = Mathf.Sin((float)(phase * 2.0 * Math.PI)) * Mathf.Exp(-t * 6f) * 0.8f;
                float crack = t < 0.05f ? n * (1f - t / 0.05f) * 0.6f : 0f;
                d[i] = Mathf.Clamp(rumble + thump + crack, -1f, 1f);
            }
        }

        static void Whoosh(float[] d, float startHz, float endHz, float noiseMix, int seed)
        {
            var random = new System.Random(seed);
            float low = 0f;
            double phase = 0;
            float duration = d.Length / (float)SampleRate;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / duration;
                phase += Mathf.Lerp(startHz, endHz, k * k) / SampleRate;
                float tone = Mathf.Sin((float)(phase * 2.0 * Math.PI));
                float n = (float)(random.NextDouble() * 2.0 - 1.0);
                low += (n - low) * Mathf.Lerp(0.05f, 0.4f, k);
                float envelope = Mathf.Sin(Mathf.PI * k);
                d[i] = (tone * (1f - noiseMix) + low * noiseMix * 2f) * envelope * 0.45f;
            }
        }

        static void AlarmTones(float[] d)
        {
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                int beep = (int)(t / 0.22f);
                bool on = t % 0.22f < 0.16f;
                float hz = beep % 2 == 0 ? 880f : 660f;
                d[i] = on ? Mathf.Sin(2f * Mathf.PI * hz * t) * 0.35f * Mathf.Exp(-t * 0.8f) : 0f;
            }
        }
    }
}
