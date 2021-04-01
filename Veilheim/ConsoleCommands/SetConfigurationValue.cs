// Veilheim
// a Valheim mod
// 
// File:    SetConfigurationValue.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Veilheim.Configurations;

namespace Veilheim.ConsoleCommands
{
    public class SetConfigurationValue : BaseConsoleCommand
    {
        public SetConfigurationValue()
        {
            CommandName = "SetValue";
            HelpText = "SetValue - Set configuration values, see SetValue help for more information";
        }


        public override bool ParseCommand(ref string input, bool isFromRemote)
        {
            var inputCopy = input;
            var parts = input.Replace("  ", " ").Split(' ').ToList();
            var command = parts.Count >= 1 ? parts[0] : null;
            var sectionPropPart = parts.Count >= 2 ? parts[1] : null;
            var valuePart = parts.Count == 3 ? parts[2] : null;

            var configParts = new List<string>();
            string sectionName = null;
            string valueName = null;
            if (!string.IsNullOrEmpty(sectionPropPart))
            {
                configParts.AddRange(sectionPropPart.Split('.'));
                sectionName = configParts.Count >= 1 ? configParts[0] : null;
                valueName = configParts.Count == 2 ? configParts[1] : null;
            }

            // Set input to nothing, so it won't be added again after our messages
            input = "";
            if (string.IsNullOrEmpty(valueName) && string.Equals(sectionName, "help", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!isFromRemote)
                {
                    Console.instance.AddString("Usage:");
                    Console.instance.AddString("SetValue (get the list of sections)");
                    Console.instance.AddString("SetValue <SectionName> (get the list of values from that section)");
                    Console.instance.AddString("SetValue <SectionName>.<ValueName> (get information about that value)");
                    Console.instance.AddString("SetValue <SectionName>.<ValueName> <value> (set that value to <value>)");
                }

                return false;
            }

            if (string.IsNullOrEmpty(sectionName))
            {
                Console.instance.AddString("List of configuration sections:");
                foreach (var property in Configuration.propertyCache)
                {
                    var syncable = typeof(ISyncableSection).IsAssignableFrom(property.PropertyType);
                    Console.instance.AddString($"{property.Name} \t\t{(syncable ? "Admin only" : "")}");
                }
            }


            if (!string.IsNullOrEmpty(sectionName) && string.IsNullOrEmpty(valuePart))
            {
                if (string.IsNullOrEmpty(valueName))
                {
                    // write info about section

                    var sectProperty =
                        Configuration.propertyCache.FirstOrDefault(x => string.Equals(x.Name, sectionName, StringComparison.CurrentCultureIgnoreCase));
                    if (sectProperty == null)
                    {
                        Console.instance.AddString($"No section {sectionName} available.");
                        return false;
                    }

                    Console.instance.AddString($"Section {sectProperty.Name}");
                    Console.instance.AddString("Properties:");

                    var sectValue = sectProperty.GetValue(Configuration.Current, null);

                    foreach (var prop in BaseConfig.GetProps(sectProperty.PropertyType))
                    {
                        var val = prop.GetValue(sectValue, null);
                        var valDefault = ((BaseConfig) sectValue).GetDefault(sectProperty.PropertyType, prop.Name);
                        Console.instance.AddString($"{prop.Name} {prop.PropertyType.Name} ({val}, default:{valDefault})");
                    }
                }
                else
                {
                    var sectProperty =
                        Configuration.propertyCache.FirstOrDefault(x => string.Equals(x.Name, sectionName, StringComparison.CurrentCultureIgnoreCase));
                    var valueProp = BaseConfig.GetProps(sectProperty.PropertyType)
                        .FirstOrDefault(x => string.Equals(x.Name, valueName, StringComparison.CurrentCultureIgnoreCase));
                    var ca = (ConfigurationAttribute) valueProp.GetCustomAttributes(false).FirstOrDefault(x => x is ConfigurationAttribute);
                    var valueComment = "";
                    if (ca != null)
                    {
                        valueComment = ca.Comment;
                    }

                    Console.instance.AddString($"Section {sectProperty.Name} value {valueProp.Name}, type {valueProp.PropertyType.Name}");

                    if (!string.IsNullOrEmpty(valueComment))
                    {
                        Console.instance.AddString(valueComment);
                    }
                }

                return false;
            }

            if (parts.Count != 3)
            {
                if (!isFromRemote)
                {
                    Console.instance.AddString("Usage: SetValue <SectionName>.<ValueName> <value>");
                }

                return false;
            }


            var sectionProperty =
                Configuration.propertyCache.FirstOrDefault(x => string.Equals(x.Name, sectionName, StringComparison.CurrentCultureIgnoreCase));
            if (sectionProperty == null)
            {
                if (!isFromRemote)
                {
                    Console.instance.AddString($"Section '{sectionName}' does not exist.");
                }

                return false;
            }

            var section = sectionProperty.GetValue(Configuration.Current, null);

            var needsSync = typeof(ISyncableSection).IsAssignableFrom(sectionProperty.PropertyType);

            var valueProperty = BaseConfig.GetProps(sectionProperty.PropertyType)
                .FirstOrDefault(x => string.Equals(x.Name, valueName, StringComparison.CurrentCultureIgnoreCase));

            if (valueProperty == null)
            {
                if (!isFromRemote)
                {
                    Console.instance.AddString($"Value '{valueName}' does not exist in section '{sectionName}'");
                }

                return false;
            }

            // Switch for value type
            if (valueProperty.PropertyType == typeof(float))
            {
                var newValue = GetFloat(valuePart);
                var oldValue = (float) valueProperty.GetValue(section, null);


                if (!isFromRemote)
                {
                    Console.instance.Print($"Setting {sectionName}.{valueName} to {newValue} (old: {oldValue})");
                }

                if (needsSync && !isFromRemote)
                {
                    SyncToClients(inputCopy);
                }
                else if (needsSync == isFromRemote)
                {
                    valueProperty.SetValue(section, newValue, null);
                }

                return true;
            }

            if (valueProperty.PropertyType == typeof(int))
            {
                var newValue = GetInt(valuePart);
                var oldValue = (int) valueProperty.GetValue(section, null);

                if (!isFromRemote)
                {
                    Console.instance.AddString($"Setting {sectionName}.{valueName} to {newValue} (old: {oldValue})");
                }

                if (needsSync && !isFromRemote)
                {
                    SyncToClients(inputCopy);
                }
                else if (needsSync == isFromRemote)
                {
                    valueProperty.GetSetMethod().Invoke(section, new object[] {newValue});
                    valueProperty.SetValue(section, newValue, null);
                }

                return true;
            }

            if (valueProperty.PropertyType == typeof(KeyCode))
            {
                var newValue = GetKeyCode(valuePart);
                var oldValue = (KeyCode) valueProperty.GetValue(section, null);

                if (!isFromRemote)
                {
                    Console.instance.AddString($"Setting {sectionName}.{valueName} to {newValue} (old: {oldValue})");
                }

                if (needsSync && !isFromRemote)
                {
                    SyncToClients(inputCopy);
                }
                else if (needsSync == isFromRemote)
                {
                    valueProperty.SetValue(section, newValue, null);
                }

                return true;
            }

            if (valueProperty.PropertyType == typeof(bool))
            {
                var newValue = GetBool(valuePart);
                var oldValue = (bool) valueProperty.GetValue(section, null);
                if (!isFromRemote)
                {
                    Console.instance.AddString($"Setting {sectionName}.{valueName} to {newValue} (old: {oldValue})");
                }

                if (needsSync && !isFromRemote)
                {
                    SyncToClients(inputCopy);
                }
                else if (needsSync == isFromRemote)
                {
                    valueProperty.SetValue(section, newValue, null);
                }

                return true;
            }

            // If it got here, we done something weird in the configuration files
            // All types should be int,float, bool or KeyCode

            return false;
        }

        private static void SyncToClients(string inputCopy)
        {
            var zPgk = new ZPackage();
            zPgk.Write(inputCopy);
            ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_Veilheim_SetConfigurationValue), zPgk);
        }

        public static void RPC_Veilheim_SetConfigurationValue(long sender, ZPackage inputPkg)
        {
            if (ZNet.instance.IsLocalInstance()) // Local game
            {
                Logger.LogInfo("RPC_Veilheim_SetConfigurationValue LOCAL");
                var input = inputPkg.ReadString();
                var inputCopy = (input + " ").Trim();
                TryExecuteCommand(ref input, true);
            }
            if (ZNet.instance.IsServerInstance()) // Server
            {
                var peer = ZNet.instance.GetPeer(sender);
                if (peer == null)
                {
                    return;
                }

                Logger.LogInfo("RPC_Veilheim_SetConfigurationValue SERVER");

                // Check if peer is in admin list
                var steamId = peer.m_socket.GetHostName();
                if (ZNet.instance.m_adminList.Contains(steamId))
                {
                    var input = inputPkg.ReadString();
                    var inputCopy = (input + " ").Trim();
                    TryExecuteCommand(ref input, true);
                    foreach (var peerEntry in ZNet.instance.m_peers)
                    {
                        Logger.LogDebug($"SENDING {inputCopy}");

                        // Send same back to all clients to actually also set the value on the client
                        ZRoutedRpc.instance.InvokeRoutedRPC(peerEntry.m_uid, nameof(RPC_Veilheim_SetConfigurationValue), inputPkg);
                    }
                }
            }
            if (ZNet.instance.IsClientInstance()) // Client
            {
                Logger.LogInfo("RPC_Veilheim_SetConfigurationValue CLIENT");
                var input = inputPkg.ReadString();
                var inputCopy = (input + " ").Trim();
                TryExecuteCommand(ref input, true);
                Console.instance.AddString($"Command '{inputCopy}' executed");
            }
        }
    }
}