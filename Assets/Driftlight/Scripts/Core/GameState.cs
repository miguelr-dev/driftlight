using System;
using UnityEngine;

namespace Driftlight
{
    /// <summary>Progress for the current run that carries across scene loads (salvage, signal fragments).</summary>
    public static class GameState
    {
        public const int TotalFragments = 3;

        public static int Salvage { get; private set; }
        public static int FragmentsFound { get; private set; }

        /// <summary>Raised whenever salvage or fragments change.</summary>
        public static event Action Changed;

        public static void AddSalvage(int amount)
        {
            Salvage += amount;
            Changed?.Invoke();
        }

        public static void AddFragment()
        {
            FragmentsFound = Mathf.Min(TotalFragments, FragmentsFound + 1);
            Changed?.Invoke();
        }

        // Reset when entering Play mode, even with domain reload turned off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlayMode()
        {
            Salvage = 0;
            FragmentsFound = 0;
            Changed = null;
        }
    }
}
