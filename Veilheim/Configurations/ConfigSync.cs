using HarmonyLib;
using System;

namespace Veilheim.Configurations
{
    public class ConfigSync
    {
        public static void RPC_ConfigSync(long sender, ZPackage configPkg)
        {
            if (ZNet.m_isServer) //Server
            {
                if (Configuration.Current == null)
                {
                    Configuration.LoadConfiguration();
                }

                ZPackage pkg = new ZPackage();
                string data = Configuration.Current.GetSyncableSections();

                //Add number of clean lines to package
                pkg.Write(data);

                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "ConfigSync", new object[]
                {
                    pkg
                });

                //ZLog.Log("Configuration synced to peer #" + sender);
                Logger.LogInfo($"Configuration synced to peer #{sender}");
            }
            else //Client
            {
                if (configPkg != null &&
                    configPkg.Size() > 0 &&
                    sender == ZRoutedRpc.instance.GetServerPeerID()) //Validate the message is from the server and not another client.
                {

                    Configuration receivedConfig = new Configuration();
                    Configuration.LoadFromIniString(receivedConfig, configPkg.ReadString());

                    Configuration.SetSyncableValues(receivedConfig);

                    //ZLog.Log("Successfully synced configuration from server.");
                    Logger.LogInfo("Successfully synced configuration from server.");
                }
            }
        }
    }

    /// <summary>
    /// Register RPC function for config sync
    /// </summary>
    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        private static void Prefix()
        {
            // Config Sync
            ZRoutedRpc.instance.Register("ConfigSync", new Action<long, ZPackage>(ConfigSync.RPC_ConfigSync));
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
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "ConfigSync", new object[] { new ZPackage() });
            }
        }
    }

}