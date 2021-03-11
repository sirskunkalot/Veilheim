using HarmonyLib;
using System;

namespace Veilheim.Configurations
{

    /// <summary>
    /// Register RPC function for config sync
    /// </summary>
    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        private static void Prefix()
        {
            // Config Sync
            ZRoutedRpc.instance.Register(nameof(ConfigSync.RPC_ConfigSync),
                new Action<long, ZPackage>(ConfigSync.RPC_ConfigSync));
        }
    }

    /// <summary>
    /// Send config sync request
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    public static class ZNet_RPCPeerInfo_Patch
    {
        private static void Postfix(ref ZNet __instance)
        {
            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogInfo("Sending config sync request to server");
                ZRoutedRpc.instance.InvokeRoutedRPC(
                    ZRoutedRpc.instance.GetServerPeerID(),
                    nameof(ConfigSync.RPC_ConfigSync),
                    new object[] { new ZPackage() });
            }
        }
    }

    /// <summary>
    /// Load configuration files when creating/joining a game (e.g. instantiating a new ZNet)
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "Awake")]
    public static class ZNet_Awake_Patch
    {
        private static void Postfix()
        {
            string msg = $"Loading {ZNet.instance.GetInstanceType()} configuration";
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
    }

    /// <summary>
    /// Save configuration after a save command is issued
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "RPC_Save")]
    public static class ZNet_RPCSave_Patch
    {
        public static void Postfix()
        {
            Logger.LogInfo("Saving configuration via RPC_Save");
            Configuration.Current.SaveConfiguration();
        }
    }

    /// <summary>
    /// Save config when the game quits a world normally (e.g. ZNet is destroyed)
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    public static class ZNet_OnDestroy_Patch
    {
        private static void Prefix()
        {
            Logger.LogInfo("Saving configuration");
            Configuration.Current.SaveConfiguration();
        }
    }

}