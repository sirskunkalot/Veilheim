using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        private new readonly Logger Logger = new Logger();

        private Harmony m_harmony;

        private readonly List<IDestroyable> m_destroyables = new List<IDestroyable>();

        public void Awake()
        {
            m_harmony = new Harmony(PluginGUID);
            m_harmony.PatchAll();

            m_destroyables.Add(new AssetManager());

            var assets = new AssetLoader();
            assets.LoadAssets();
            m_destroyables.Add(assets);

            PatchDispatcher.Init();

            Logger.LogInfo("Plugin loaded");
        }

        public void OnDestroy()
        {
            Logger.LogInfo("Destroying plugin");

            foreach (var destroyable in m_destroyables)
            {
                destroyable.Destroy();
            }

            Logger.Destroy();

            m_harmony.UnpatchAll(PluginGUID);
        }
    }

    /// <summary>
    /// A namespace wide Logger class, which automatically creates a <see cref="ManualLogSource"/> 
    /// for every namespace from which it is being called
    /// </summary>
    internal class Logger
    {
        private readonly Dictionary<string, ManualLogSource> m_logger 
            = new Dictionary<string, ManualLogSource>();

        public static Logger Instance;

        public Logger()
        {
            Instance = this;
        }

        public void Destroy()
        {
            Logger.LogDebug("Destroying Logger");
            
            foreach (var entry in m_logger)
            {
                BepInEx.Logging.Logger.Sources.Remove(entry.Value);
            }

            m_logger.Clear();
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
        internal static void LogFatal(object data) { Instance.GetLogger().LogFatal(data); }
        internal static void LogError(object data) { Instance.GetLogger().LogError(data); }
        internal static void LogWarning(object data) { Instance.GetLogger().LogWarning(data); }
        internal static void LogMessage(object data) { Instance.GetLogger().LogMessage(data); }
        internal static void LogInfo(object data) { Instance.GetLogger().LogInfo(data); }
        internal static void LogDebug(object data) { Instance.GetLogger().LogDebug(data); }
    }
}
