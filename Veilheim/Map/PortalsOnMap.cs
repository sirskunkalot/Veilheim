using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Veilheim.Configurations;

namespace Veilheim.Map
{
    public class PortalsOnMap
    {
        /// <summary>
        /// Holder for our pins, these get drawn to the map
        /// </summary>
        public static List<Minimap.PinData> portalPins = new List<Minimap.PinData>();

        /// <summary>
        /// Create PinData objects without adding them to the map directly
        /// </summary>
        public static Minimap.PinData CreatePinData(Vector3 pos, Minimap.PinType type, string name, bool save, bool isChecked)
        {
            var pinData = new Minimap.PinData();
            pinData.m_type = type;
            pinData.m_name = name;
            pinData.m_pos = pos;
            pinData.m_icon = Minimap.instance.GetSprite(type);
            pinData.m_save = save;
            pinData.m_checked = isChecked;
            return pinData;
        }

        /// <summary>
        /// Add and remove pins in internal list based on a list of portals
        /// </summary>
        /// <param name="portals"></param>
        public static void UpdatePins(PortalList portals)
        {
            Logger.LogDebug("Updating pins of portals on minimap");
            
            // prevent MT crashing
            lock (portalPins)
            {
                // Add connected portals (separated connected and unconnected, maybe show another icon?)
                foreach (var portal in portals.FindAll(x => x.m_con))
                {
                    Logger.LogDebug(portal);

                    // Was pin already added?
                    var foundPin = portalPins.FirstOrDefault(x => x.m_pos == portal.m_pos);
                    if (foundPin != null)
                    {
                        // Did the pin's name change?
                        if (foundPin.m_name != portal.m_tag)
                        {
                            // Remove pin at location and readd with new name
                            portalPins.Remove(foundPin);
                            portalPins.Add(CreatePinData(portal.m_pos, Minimap.PinType.Icon4, portal.m_tag, false, false));
                        }
                    }
                    else
                    {
                        // Add new pin
                        portalPins.Add(CreatePinData(portal.m_pos, Minimap.PinType.Icon4, portal.m_tag, false, false));
                    }
                }

                // Add unconnected portals (maybe show another icon / text?)
                foreach (var portal in portals.FindAll(x => !x.m_con))
                {
                    Logger.LogDebug(portal);

                    // Was pin already added?
                    var foundPin = portalPins.FirstOrDefault(x => x.m_pos == portal.m_pos);
                    if (foundPin != null)
                    {
                        // Did the pin's name change?
                        if (foundPin.m_name != portal.m_tag)
                        {
                            // Remove pin at location and readd with new name
                            portalPins.Remove(foundPin);
                            portalPins.Add(CreatePinData(portal.m_pos, Minimap.PinType.Icon4, portal.m_tag, false, false));
                        }
                    }
                    else
                    {
                        // Add new pin
                        portalPins.Add(CreatePinData(portal.m_pos, Minimap.PinType.Icon4, portal.m_tag, false, false));
                    }
                }

                // Remove destroyed portals from map
                // doesn't really react on portal destruction, only works if after a portal was destroyed, someone set the name on another portal
                foreach (var kv in portalPins.ToList())
                {
                    if (portals.All(x => x.m_pos != kv.m_pos))
                    {
                        portalPins.Remove(kv);
                    }
                }
            }

            Logger.LogInfo("Portal pins updated succesfully");
        }

        /// <summary>
        /// Add and remove pins on minimap based on the internal list
        /// </summary>
        public static void UpdateMinimap()
        {
            List<Minimap.PinData> copy;

            lock (portalPins)
            {
                copy = portalPins.ToList();
            }

            foreach (var pin in copy)
            {
                var foundPin = Minimap.instance.m_pins.FirstOrDefault(x => x.m_pos == pin.m_pos && x.m_type == pin.m_type);

                if (foundPin == null)
                {
                    // Pin not on map, add
                    Minimap.instance.AddPin(pin.m_pos, pin.m_type, pin.m_name, pin.m_save, pin.m_checked);
                }
                else if (foundPin.m_name != pin.m_name)
                {
                    // Pin name change, remove and add new
                    Minimap.instance.RemovePin(foundPin);
                    Minimap.instance.AddPin(pin.m_pos, pin.m_type, pin.m_name, pin.m_save, pin.m_checked);
                }
            }

            // remove all teleporter pins (type 4, position in copy list)
            foreach (var pin in Minimap.instance.m_pins.Where(x => !x.m_save && x.m_type == Minimap.PinType.Icon4).ToList()
                .Where(pin => Minimap.instance.m_locationPins.Values.All(x => x.m_pos != pin.m_pos)).Where(pin => copy.All(x => x.m_pos != pin.m_pos)))
            {
                Minimap.instance.RemovePin(pin);
            }
        }

        /// <summary>
        /// RPC to handle initial sync to a new peer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="teleporterZPackage"></param>
        public static void RPC_TeleporterSyncInit(long sender, ZPackage teleporterZPackage)
        {
            // SERVER SIDE
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                Logger.LogInfo($"Sending portal data to peer #{sender}");

                var portals = PortalList.GetPortals();

                if (ZNet.instance.IsLocalInstance())
                {
                    UpdatePins(portals);
                }

                var package = portals.ToZPackage();

                ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_TeleporterSyncInit), new object[] { package });
            }

            // CLIENT SIDE
            if (ZNet.instance.IsClientInstance())
            {
                if (teleporterZPackage != null && teleporterZPackage.Size() > 0 && sender == ZRoutedRpc.instance.GetServerPeerID())
                {
                    // Read package and create pins from portal list
                    Logger.LogInfo("Received portal data from server");

                    var portals = PortalList.FromZPackage(teleporterZPackage);

                    UpdatePins(portals);
                }
            }
        }

        /// <summary>
        /// RPC to handle sync to all peers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="teleporterZPackage"></param>
        public static void RPC_TeleporterSync(long sender, ZPackage teleporterZPackage)
        {
            // SERVER SIDE
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                Logger.LogInfo("Sending portal data to all peers");

                var portals = PortalList.GetPortals();

                if (ZNet.instance.IsLocalInstance())
                {
                    UpdatePins(portals);
                }

                var package = portals.ToZPackage();

                foreach (var peer in ZNet.instance.m_peers)
                {
                    if (!peer.m_server)
                    {
                        Logger.LogInfo($"Sending portal data to peer #{peer.m_uid}");

                        ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, nameof(RPC_TeleporterSync), new object[] { package });
                    }
                }
            }

            // CLIENT SIDE
            if (ZNet.instance.IsClientInstance())
            {
                if (teleporterZPackage != null && teleporterZPackage.Size() > 0 && sender == ZRoutedRpc.instance.GetServerPeerID())
                {
                    // Read package and create pins from portal list
                    Logger.LogInfo("Received portal data from server");

                    var portals = PortalList.FromZPackage(teleporterZPackage);

                    UpdatePins(portals);
                }
            }
        }
    }

    /// <summary>
    /// Register TeleporterSync RPC calls
    /// </summary>
    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        private static void Prefix()
        {
            ZRoutedRpc.instance.Register(nameof(PortalsOnMap.RPC_TeleporterSyncInit),
                new Action<long, ZPackage>(PortalsOnMap.RPC_TeleporterSyncInit));
            ZRoutedRpc.instance.Register(nameof(PortalsOnMap.RPC_TeleporterSync),
                new Action<long, ZPackage>(PortalsOnMap.RPC_TeleporterSync));
        }
    }

    /// <summary>
    /// CLIENT SIDE: Initially pull portals after SetMapData on Minimap
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "SetMapData")]
    public static class Minimap_SetMapData_Patch
    {
        public static void Postfix()
        {
            if (ZNet.instance.IsServerInstance())
            {
                return;
            }
            if (!Configuration.Current.Map.IsEnabled || !Configuration.Current.Map.showPortalsOnMap)
            {
                return;
            }
            if (ZNet.instance.IsLocalInstance())
            {
                Logger.LogInfo("Initializing portals");
                PortalsOnMap.UpdatePins(PortalList.GetPortals());
            }
            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogInfo("Sending portal sync request to server");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(PortalsOnMap.RPC_TeleporterSyncInit),
                    new object[] { new ZPackage() });
            }
        }
    }

    /// <summary>
    /// CLIENT SIDE: React to setting tag on portal
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), "RPC_SetTag", typeof(long), typeof(string))]
    public static class TeleportWorld_RPCSetTag_Patch
    {
        public static void Postfix(TeleportWorld __instance, long sender, string tag)
        {
            if (ZNet.instance.IsServerInstance())
            {
                return;
            }

            Logger.LogInfo("Portal tag changed");

            if (ZNet.instance.IsLocalInstance())
            {
                Logger.LogInfo("Sending portal sync request to server");

                // Send trigger to server
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(PortalsOnMap.RPC_TeleporterSync),
                    new object[] { new ZPackage() });
            }

            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogDebug("Forcing ZDO to server");

                // Force sending ZDO to server
                var temp = __instance.m_nview.GetZDO();

                ZDOMan.instance.GetZDO(temp.m_uid);

                ZDOMan.instance.GetPeer(ZRoutedRpc.instance.GetServerPeerID()).ForceSendZDO(temp.m_uid);

                Logger.LogInfo("Sending deferred portal sync request to server");

                Task.Factory.StartNew(() =>
                {
                    // Wait for ZDO to be sent else server won't have accurate information to send back
                    Thread.Sleep(5000);

                    // Send trigger to server
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(PortalsOnMap.RPC_TeleporterSync),
                        new object[] { new ZPackage() });
                });
            }
        }
    }

    /// <summary>
    /// CLIENT SIDE: Update portal pins on the minimap in each update cycle
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "UpdateLocationPins")]
    public static class Minimap_UpdateLocationPins_Patch
    {
        public static void Postfix()
        {
            if (ZNet.instance.IsServerInstance())
            {
                return;
            }
            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.showPortalsOnMap)
            {
                PortalsOnMap.UpdateMinimap();
            }
        }
    }

    /// <summary>
    /// CLIENT SIDE: React to a placement of a portal
    /// </summary>
    [HarmonyPatch(typeof(Player), "PlacePiece", typeof(Piece))]
    public static class Player_PlacePiece_Patch
    {
        public static void Postfix(Piece piece, ref bool __result)
        {
            if (ZNet.instance.IsServerInstance())
            {
                return;
            }
            if (__result && !piece.IsCreator() && piece.m_name == "$piece_portal")
            {
                Logger.LogInfo("Portal created");

                if (ZNet.instance.IsLocalInstance())
                {
                    Logger.LogInfo("Sending portal sync request to server");

                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(PortalsOnMap.RPC_TeleporterSync),
                        new object[] { new ZPackage() });
                }

                if (ZNet.instance.IsClientInstance())
                {
                    Logger.LogInfo("Sending deferred portal sync request to server");

                    Task.Factory.StartNew(() =>
                    {
                        // Wait for ZDO to be sent else server won't have accurate information to send back
                        Thread.Sleep(5000);

                        // Send trigger to server
                        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(PortalsOnMap.RPC_TeleporterSync),
                            new object[] { new ZPackage() });
                    });
                }
            }
        }
    }

    /// <summary>
    /// CLIENT SIDE: React to a destruction of a portal
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "Destroy")]
    public static class WearNTear_Destroy_Patch
    {
        public static void Prefix(WearNTear __instance)
        {
            if (ZNet.instance.IsServerInstance())
            {
                return;
            }
            if (__instance.m_piece && __instance.m_piece.m_name == "$piece_portal")
            {
                Logger.LogInfo("Portal destroyed");

                if (ZNet.instance.IsLocalInstance())
                {
                    Logger.LogInfo("Sending portal sync request to server");

                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(PortalsOnMap.RPC_TeleporterSync),
                        new object[] { new ZPackage() });
                }

                if (ZNet.instance.IsClientInstance())
                {
                    Logger.LogInfo("Sending deferred portal sync request to server");

                    Task.Factory.StartNew(() =>
                    {
                        // Wait for ZDO to be sent else server won't have accurate information to send back
                        Thread.Sleep(5000);

                        // Send trigger to server
                        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(PortalsOnMap.RPC_TeleporterSync),
                            new object[] { new ZPackage() });
                    });
                }
            }
        }
    }
}
