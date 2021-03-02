using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace Veilheim
{
    [BepInPlugin("de.sirskunkalot.valheim.veilheim", "Veilheim", "0.0.1")]
    class VeilheimPlugin : BaseUnityPlugin
    {
        // Awake is called once when both the game and the plug-in are loaded
        void Awake()
        {
            // Expose Logger
            Veilheim.Logger.Instance = Logger;
            
            // Create and patch
            var harmony = new Harmony("mod.veilheim");
            harmony.PatchAll();
        }
    }

    public static class Logger
    {
        public static ManualLogSource Instance { get; set; }

        public static void LogFatal(Object data) { Instance.LogFatal(data); }
        public static void LogError(Object data) { Instance.LogError(data); }
        public static void LogWarning(Object data) { Instance.LogWarning(data); }
        public static void LogMessage(Object data) { Instance.LogMessage(data); }
        public static void LogInfo(Object data) { Instance.LogInfo(data); }
        public static void LogDebug(Object data) { Instance.LogDebug(data); }
    }
}
