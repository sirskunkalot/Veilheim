// Veilheim
// a Valheim mod
// 
// File:    Veilheim.cs
// Project: Veilheim

using System.Collections.Generic;
using System.Diagnostics;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Veilheim.AssetUtils;
using Veilheim.PatchEvents;

namespace Veilheim
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class VeilheimPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.valheim.veilheim";
        public const string PluginName = "Veilheim";
        public const string PluginVersion = "0.0.1";

        // Static instance needed for Coroutines
        public static VeilheimPlugin instance = null;
        
        private readonly List<IDestroyable> m_destroyables = new List<IDestroyable>();

        private Harmony m_harmony;

        public void Awake()
        {
            m_harmony = new Harmony(PluginGUID);
            m_harmony.PatchAll();

            Veilheim.Logger.Init();

            AssetManager.Init();
            m_destroyables.Add(AssetManager.Instance);

            AssetLoader.LoadAssets();

            PatchDispatcher.Init();

            Veilheim.Logger.LogInfo("Plugin loaded");
            instance = this;
        }

        public void OnDestroy()
        {
            Veilheim.Logger.LogInfo("Destroying plugin");

            foreach (var destroyable in m_destroyables)
            {
                destroyable.Destroy();
            }

            Veilheim.Logger.Destroy();

            m_harmony.UnpatchAll(PluginGUID);
        }

        private void OnGUI()
        {
            // Display version in main menu
            if (SceneManager.GetActiveScene().name == "start")
            {
                GUI.Label(new Rect(Screen.width - 100, 5, 100, 25), "Veilheim v" + PluginVersion);
            }
        }
    }

    /// <summary>
    ///     A namespace wide Logger class, which automatically creates a <see cref="ManualLogSource" />
    ///     for every namespace from which it is being called
    /// </summary>
    internal class Logger
    {
        public static Logger Instance;

        private readonly Dictionary<string, ManualLogSource> m_logger = new Dictionary<string, ManualLogSource>();

        private Logger() { }

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = new Logger();
            }
        }

        public static void Destroy()
        {
            LogDebug("Destroying Logger");

            foreach (var entry in Instance.m_logger)
            {
                BepInEx.Logging.Logger.Sources.Remove(entry.Value);
            }

            Instance.m_logger.Clear();
        }

        private ManualLogSource GetLogger()
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

        internal static void LogFatal(object data)
        {
            Instance.GetLogger().LogFatal(data);
        }

        internal static void LogError(object data)
        {
            Instance.GetLogger().LogError(data);
        }

        internal static void LogWarning(object data)
        {
            Instance.GetLogger().LogWarning(data);
        }

        internal static void LogMessage(object data)
        {
            Instance.GetLogger().LogMessage(data);
        }

        internal static void LogInfo(object data)
        {
            Instance.GetLogger().LogInfo(data);
        }

        internal static void LogDebug(object data)
        {
            Instance.GetLogger().LogDebug(data);
        }
    }
}