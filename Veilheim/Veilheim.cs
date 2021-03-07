using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Veilheim
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    class VeilheimPlugin : BaseUnityPlugin
    {
        private const string PluginGUID = "de.sirskunkalot.valheim.veilheim";
        private const string PluginName = "Veilheim";
        private const string PluginVersion = "0.0.1";

        // Awake is called once when both the game and the plug-in are loaded
        void Awake()
        {
            // Create and patch
            var harmony = new Harmony(PluginGUID);
            harmony.PatchAll();
        }
    }

    /// <summary>
    /// A namespace wide Logger class, which automatically creates a <see cref="ManualLogSource"/> 
    /// for every namespace from which it is been called
    /// </summary>
    public static class Logger
    {
        private static readonly Dictionary<string, ManualLogSource> m_logger 
            = new Dictionary<string, ManualLogSource>();

        public static ManualLogSource GetLogger()
        {
            var type = new StackFrame(2).GetMethod().DeclaringType;

            ManualLogSource ret;
            if (!m_logger.TryGetValue(type.Namespace, out ret))
            {
                ret = BepInEx.Logging.Logger.CreateLogSource(type.Namespace);
                m_logger.Add(type.Namespace, ret);
            }
            return ret;
        }
        public static void LogFatal(Object data) { GetLogger().LogFatal(data); }
        public static void LogError(Object data) { GetLogger().LogError(data); }
        public static void LogWarning(Object data) { GetLogger().LogWarning(data); }
        public static void LogMessage(Object data) { GetLogger().LogMessage(data); }
        public static void LogInfo(Object data) { GetLogger().LogInfo(data); }
        public static void LogDebug(Object data) { GetLogger().LogDebug(data); }
    }
}
