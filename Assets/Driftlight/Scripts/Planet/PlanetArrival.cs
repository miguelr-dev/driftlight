using System.Collections;
using UnityEngine;

namespace Driftlight
{
    /// <summary>Plays the arrival beat on a planet: fade in from the warp flash, title card, first objective.</summary>
    public class PlanetArrival : MonoBehaviour
    {
        public string planetName = "VERDANCE";
        public string subtitle = "Planet 1 of 3  -  The glowing jungle";
        public string objective = "Follow the beam of light to the signal fragment";
        public float fadeSeconds = 1.8f;

        IEnumerator Start()
        {
            ScreenFader.FadeFrom(Color.white, fadeSeconds);
            yield return null;
            Hud.Instance?.ShowTitle(planetName, subtitle, 5f);
            Hud.Instance?.SetObjective(objective);
        }
    }
}
