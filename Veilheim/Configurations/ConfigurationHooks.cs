// Veilheim
// a Valheim mod
// 
// File:    ConfigurationHooks.cs
// Project: Veilheim

using System;
using System.Linq;
using Veilheim.PatchEvents;

namespace Veilheim.Configurations
{
    public class ConfigurationPatches : IPatchEventConsumer
    {
        /// <summary>
        ///     Load configuration files when creating/joining a game (i.e. instantiating a new ZNet).<br />
        ///     Has a high priority (0), so it is assured, that the configuration is loaded before any other hooked code is
        ///     executed.
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(ZNet), nameof(ZNet.Awake), PatchEventType.Postfix)]
        public static void LoadConfiguration(ZNet instance)
        {
            var msg = $"Loading {instance.GetInstanceType()} configuration";
            Logger.LogMessage(msg);

            if (!Configuration.LoadConfiguration())
            {
                Logger.LogError("Error while loading configuration");
            }
            else
            {
                Logger.LogMessage("Configuration loaded succesfully");
            }
        }

        /// <summary>
        ///     Save configuration after a save command is issued
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(ZNet), nameof(ZNet.RPC_Save), PatchEventType.Postfix)]
        public static void SaveConfigOnRPCSave(ZNet instance)
        {
            Logger.LogInfo("Saving configuration via RPC_Save");
            Configuration.Current.SaveConfiguration();
        }

        /// <summary>
        ///     Save config when the game quits a world normally (e.g. ZNet is destroyed)
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(ZNet), nameof(ZNet.Shutdown), PatchEventType.Prefix)]
        public static void SaveConfigurationOnDestroy(ZNet instance)
        {
            Logger.LogInfo("Saving configuration");
            Configuration.Current?.SaveConfiguration();
        }
    }
}