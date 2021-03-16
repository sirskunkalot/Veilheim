// Veilheim
// a Valheim mod
// 
// File:    ConfigSync.cs
// Project: Veilheim

namespace Veilheim.Configurations
{
    public class ConfigSync
    {
        public static void RPC_ConfigSync(long sender, ZPackage configPkg)
        {
            if (ZNet.m_isServer) //Server
            {
                Logger.LogInfo($"Sending configuration data to peer #{sender}");

                if (Configuration.Current == null)
                {
                    Configuration.LoadConfiguration();
                }

                var pkg = new ZPackage();
                var data = Configuration.Current.GetSyncableSections();

                //Add number of clean lines to package
                pkg.Write(data);

                ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_ConfigSync), pkg);
            }
            else //Client
            {
                if (configPkg != null && configPkg.Size() > 0 && sender == ZRoutedRpc.instance.GetServerPeerID()
                ) // Validate the message is from the server and not another client.
                {
                    Logger.LogInfo("Received configuration data from server.");

                    var receivedConfig = new Configuration();
                    Configuration.LoadFromIniString(receivedConfig, configPkg.ReadString());

                    Configuration.SetSyncableValues(receivedConfig);
                }
            }
        }
    }
}