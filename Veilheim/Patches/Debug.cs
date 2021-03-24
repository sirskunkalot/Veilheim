// Veilheim
// a Valheim mod
// 
// File:    Debug.cs
// Project: Veilheim

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Veilheim.Patches
{
    class Debug
    {
#if DEBUG
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        public static class OnSpawnedDebugPatch
        {
            public static void Prefix(ref Player __instance, ref bool ___m_firstSpawn)
            {
                // Temp: disable valkyrie animation during testing for sanity reasons
                ___m_firstSpawn = false;
            }
        }
#endif
    }
}
