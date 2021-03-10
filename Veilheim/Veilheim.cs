using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using Veilheim.Util;

namespace Veilheim
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    class VeilheimPlugin : BaseUnityPlugin
    {
        private const string PluginGUID = "de.sirskunkalot.valheim.veilheim";
        private const string PluginName = "Veilheim";
        private const string PluginVersion = "0.0.1";

        internal static Harmony Harmony { get; private set; }

        internal static VeilheimPlugin Instance { get; private set; }

        // Awake is called once when both the game and the plug-in are loaded
        void Awake()
        {
            Instance = this;
            Harmony = new Harmony(PluginGUID);
            Harmony.PatchAll();

            var assetBundle = AssetLoader.GetAssetBundleFromResources("item_skunkaxe");
            AssetLoader.LoadPiecePrefab(assetBundle, "piece_trashcan", new PieceDef()
            {
                Table = "_HammerPieceTable",
                CraftingStation = "piece_workbench",
                Resources = new List<RecipeRequirementConfig>
                {
                    new RecipeRequirementConfig { item = "Stone", amount = 1 },
                    new RecipeRequirementConfig { item = "Wood", amount = 1 }
                }
            });
            assetBundle.Unload(false);
        }
    }

    /// <summary>
    /// A namespace wide Logger class, which automatically creates a <see cref="ManualLogSource"/> 
    /// for every namespace from which it is been called
    /// </summary>
    internal static class Logger
    {
        private static readonly Dictionary<string, ManualLogSource> m_logger 
            = new Dictionary<string, ManualLogSource>();

        private static ManualLogSource GetLogger()
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
        internal static void LogFatal(object data) { GetLogger().LogFatal(data); }
        internal static void LogError(object data) { GetLogger().LogError(data); }
        internal static void LogWarning(object data) { GetLogger().LogWarning(data); }
        internal static void LogMessage(object data) { GetLogger().LogMessage(data); }
        internal static void LogInfo(object data) { GetLogger().LogInfo(data); }
        internal static void LogDebug(object data) { GetLogger().LogDebug(data); }
    }
}
