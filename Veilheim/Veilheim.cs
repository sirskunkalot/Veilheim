using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using IniParser;
using System;
using System.IO;
using Veilheim.Configurations;
using Veilheim.ConsoleCommands;

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
            
            // Configuration
            Logger.LogInfo("Trying to load configuration");

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

    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        private static void Prefix()
        {
            //Config Sync
            ZRoutedRpc.instance.Register("ConfigSync", new Action<long, ZPackage>(ConfigSync.RPC_ConfigSync));

            // Configuration console command RPC
            ZRoutedRpc.instance.Register("SetConfigurationValue", new Action<long, ZPackage>(SetConfigurationValue.RPC_SetConfigurationValue));

            // register all console commands
            BaseConsoleCommand.InitializeCommand<SetConfigurationValue>();
        }
    }

    [HarmonyPatch(typeof(ZNet), "Awake")]
    public static class ZNet_Awake_Patch
    {
        private static void Postfix()
        {
            bool isClient = ZNet.instance.IsClientInstance();
            string msg = isClient ? "Loading client configuration" : "Loading server configuration";
            Logger.LogInfo(msg);

            // NEED PROPER DETECTION IF IT IS SERVER HERE
            if (!Configuration.LoadConfiguration(isClient))
            {
                Logger.LogInfo("Error while loading configuration file.");
            }
            else
            {
                Logger.LogInfo("Configuration file loaded succesfully.");
            }
        }
    }
}
