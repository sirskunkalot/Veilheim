// Veilheim
// a Valheim mod
// 
// File:    ConfigurationRPC.cs
// Project: Veilheim

using System;
using System.Linq;
using Veilheim.PatchEvents;

namespace Veilheim.Configurations
{
    public class ConfigurationRPC : IPatchEventConsumer
    {
        /// <summary>
        ///     Register RPC function for configuration data
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Game), nameof(Game.Start), PatchEventType.Prefix)]
        public static void RegisterRPC(Game instance)
        {
            // Admin status
            ZRoutedRpc.instance.Register(nameof(RPC_IsAdmin), new Action<long, bool>(RPC_IsAdmin));

            // Config Sync
            ZRoutedRpc.instance.Register(nameof(RPC_ConfigSync), new Action<long, ZPackage>(RPC_ConfigSync));
        }

        /// <summary>
        ///     Send configuration data requests
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(ZNet), nameof(ZNet.RPC_PeerInfo), PatchEventType.Postfix)]
        public static void RequestConfigInformation(ZNet instance)
        {
            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogInfo("Querying admin status from server");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_IsAdmin), false);

                Logger.LogInfo("Sending config sync request to server");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_ConfigSync), new ZPackage());
            }
        }

        public static void RPC_IsAdmin(long sender, bool isAdmin)
        {
            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogDebug("Received player admin status from server");
                Configuration.PlayerIsAdmin = isAdmin;
            }
            if (ZNet.instance.IsServerInstance())
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Logger.LogDebug("Sending admin status to peer #" + sender);
                    bool result = ZNet.instance.m_adminList.Contains(peer.m_socket.GetHostName());
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_IsAdmin), result);
                }
            }
        }

        public static void RPC_ConfigSync(long sender, ZPackage configPkg)
        {
            if (ZNet.instance.IsClientInstance())
            {
                // Validate the message is from the server and not another client.
                if (configPkg != null && configPkg.Size() > 0 && sender == ZRoutedRpc.instance.GetServerPeerID())
                {
                    Logger.LogMessage("Received configuration data from server");

                    var receivedConfig = new Configuration();
                    Configuration.LoadFromIniString(receivedConfig, configPkg.ReadString());

                    Configuration.SetSyncableValues(receivedConfig);
                }
            }
            if (ZNet.instance.IsServerInstance())
            {
                if (Configuration.Current == null)
                {
                    Configuration.LoadConfiguration();
                }

                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Logger.LogMessage($"Sending configuration data to peer #{sender}");
                    var pkg = new ZPackage();
                    var data = Configuration.Current.GetSyncableSections();

                    //Add number of clean lines to package
                    pkg.Write(data);

                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_ConfigSync), pkg);
                }
            }
        }

    }
}