using System;
using System.Collections;
using UnityEngine;

namespace Driftlight
{
    /// <summary>
    /// Runs the fight at the signal relay: an intro, then waves of drones that warp in around the
    /// player. Raises <see cref="Cleared"/> once every wave is destroyed.
    /// </summary>
    public class EncounterDirector : MonoBehaviour
    {
        public EnemyDrone dronePrefab;
        public GameObject warpInEffect;
        [Tooltip("Number of drones in each wave.")]
        public int[] waves = { 3, 5 };
        public float startDelay = 8f;
        public float timeBetweenWaves = 4f;
        public float spawnDistance = 230f;

        public string areaTitle = "THE DRIFT";
        public string areaSubtitle = "Outer system. The signal starts here.";
        public string introObjective = "Fly to the signal relay";
        public string clearedTitle = "AREA CLEAR";
        public string clearedSubtitle = "The relay is quiet. The signal is still calling.";
        public string clearedObjective = "Relay secured";

        public event Action Cleared;
        public bool IsCleared { get; private set; }
        public int CurrentWave { get; private set; }

        IEnumerator Start()
        {
            yield return null;
            Hud.Instance?.ShowTitle(areaTitle, areaSubtitle, 4.5f);
            Hud.Instance?.SetObjective(introObjective);
            yield return new WaitForSeconds(startDelay);

            for (int w = 0; w < waves.Length; w++)
            {
                CurrentWave = w + 1;
                Hud.Instance?.ShowTitle("HOSTILES INBOUND", $"Wave {w + 1} of {waves.Length}", 2.5f);
                Sfx.Play2D(Sfx.Alarm, 0.5f);

                for (int i = 0; i < waves[w]; i++)
                {
                    SpawnDrone();
                    yield return new WaitForSeconds(0.35f);
                }

                while (EnemyDrone.Active.Count > 0)
                {
                    int left = EnemyDrone.Active.Count;
                    Hud.Instance?.SetObjective(left == 1 ? "Destroy the last drone" : $"Destroy the drones ({left} left)");
                    yield return null;
                }

                if (w < waves.Length - 1)
                {
                    Hud.Instance?.SetObjective("Wave cleared. More are coming...");
                    yield return new WaitForSeconds(timeBetweenWaves);
                }
            }

            IsCleared = true;
            Hud.Instance?.ShowTitle(clearedTitle, clearedSubtitle, 4.5f);
            Hud.Instance?.SetObjective(clearedObjective);
            Cleared?.Invoke();
        }

        void SpawnDrone()
        {
            var player = ShipController.Player;
            if (!player || !dronePrefab) return;

            Vector3 direction = (player.transform.forward * 0.8f + UnityEngine.Random.insideUnitSphere * 0.7f).normalized;
            Vector3 position = player.transform.position + direction * spawnDistance;
            Instantiate(dronePrefab, position, Quaternion.LookRotation(-direction));
            if (warpInEffect) Instantiate(warpInEffect, position, Quaternion.identity);
            Sfx.Play(Sfx.WarpIn, position, 0.6f);
        }
    }
}
