// Veilheim
// a Valheim mod
// 
// File:    SharedMapSystem.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zlib;
using UnityEngine;
using Veilheim.Configurations;
using Veilheim.Extensions;
using Veilheim.PatchEvents;
using CompressionLevel = Ionic.Zlib.CompressionLevel;

namespace Veilheim.Map
{
    public class SharedMapPatches : PatchEventConsumer
    {
        private static bool isInSetMapData;
        private static readonly List<int> explorationQueue = new List<int>();

        /// <summary>
        ///     Apply other player's locations as own exploration
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Minimap), nameof(Minimap.UpdateExplore), PatchEventType.Prefix)]
        public static void GetSharedExploration(Minimap instance)
        {
            if (!Configuration.Current.MapServer.IsEnabled)
            {
                return;
            }

            if (Configuration.Current.MapServer.shareMapProgression)
            {
                if (instance.m_exploreTimer + Time.deltaTime > instance.m_exploreInterval)
                {
                    var tempPlayerInfos = new List<ZNet.PlayerInfo>();
                    ZNet.instance.GetOtherPublicPlayers(tempPlayerInfos);

                    foreach (var player in tempPlayerInfos)
                    {
                        ExploreLocal(player.m_position);
                    }
                }
            }

            instance.Explore(Player.m_localPlayer.transform.position, Configuration.Current.MapServer.exploreRadius);
        }

        /// <summary>
        ///     On server, load saved data on minimap awake
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Minimap), nameof(Minimap.Awake), PatchEventType.Postfix, 600)]
        public static void LoadExplorationData(Minimap instance)
        {
            if (Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.shareMapProgression)
            {
                if (ZNet.instance.IsServerInstance())
                {
                    Minimap.instance.m_explored = new bool[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize];
                    if (File.Exists(Path.Combine(Configuration.ConfigIniPath, ZNet.instance.GetWorldUID().ToString(), "Explorationdata.bin")))
                    {
                        var mapData = ZPackageExtension.ReadFromFile(Path.Combine(Configuration.ConfigIniPath, ZNet.instance.GetWorldUID().ToString(),
                            "Explorationdata.bin"));
                        ApplyMapData(mapData);
                    }
                    else
                    {
                        for (var i = 0; i < Minimap.instance.m_explored.Length; i++)
                        {
                            Minimap.instance.m_explored[i] = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Apply sent exploration data to local map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mapData"></param>
        public static void RPC_ReceiveExploration(long sender, ZPackage mapData)
        {
            if (mapData == null)
            {
                return;
            }

            if (ZNet.instance.IsServerInstance())
            {
                Logger.LogInfo($"Received map data from client #{sender}");
                ApplyMapData(mapData);

                var tempPlayerInfos = new List<ZNet.PlayerInfo>();
                ZNet.instance.GetOtherPublicPlayers(tempPlayerInfos);

                var newMapData = new ZPackage(CreateExplorationData());

                foreach (var player in tempPlayerInfos)
                {
                    Logger.LogInfo($"Sending map data to player {player.m_name} #{ZNet.instance.GetPeerByPlayerName(player.m_name)}");
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetPeerByPlayerName(player.m_name).m_uid, nameof(RPC_ReceiveExploration),
                        newMapData);
                }
            }

            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogInfo("Received map data from server");

                // Set flag to prevent enqueuing again for sending, since it can be new
                ApplyMapData(mapData);
            }
        }

        /// <summary>
        ///     Apply exploration data to local map (server and client)
        /// </summary>
        /// <param name="mapData"></param>
        public static void ApplyMapData(ZPackage mapData)
        {
            try
            {
                var isServer = ZNet.instance.IsServerInstance();
                mapData.SetPos(0);
                using (var gz = new ZlibStream(mapData.m_stream, CompressionMode.Decompress))
                {
                    using (var br = new BinaryReader(gz))
                    {
                        var state = br.ReadBoolean();
                        var idx = 0;

                        while (idx < Minimap.instance.m_explored.Length)
                        {
                            var count = br.ReadInt32();
                            while (count > 0)
                            {
                                if (state && !isServer)
                                {
                                    // Use local helper to prevent enqueuing again for sending
                                    ExploreLocal(idx % Minimap.instance.m_textureSize, idx / Minimap.instance.m_textureSize);
                                }
                                else if (state)
                                {
                                    Minimap.instance.m_explored[idx] |= state;
                                }

                                idx++;
                                count--;
                            }

                            state = !state;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Application of mapdata gone wrong.{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                Logger.LogError("Texture size: " + Minimap.instance.m_textureSize);
            }
        }

        /// <summary>
        ///     Before ZNet destroy, save data to file on server
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(ZNet), nameof(ZNet.Shutdown), PatchEventType.Prefix)]
        public static void SaveExplorationData(ZNet instance)
        {
            // Save exploration data only on the server
            if (ZNet.instance.IsServerInstance() && Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.shareMapProgression)
            {
                Logger.LogInfo($"Saving shared exploration data");
                var mapData = new ZPackage(CreateExplorationData().ToArray());
                mapData.WriteToFile(Path.Combine(Configuration.ConfigIniPath, ZNet.instance.GetWorldUID().ToString(), "Explorationdata.bin"));
            }
        }

        /// <summary>
        ///     Register needed RPC's
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Game), nameof(Game.Start), PatchEventType.Prefix)]
        public static void Register_RPC_MapSharing(Game instance)
        {
            // Map data Receive
            ZRoutedRpc.instance.Register(nameof(RPC_ReceiveExploration), new Action<long, ZPackage>(RPC_ReceiveExploration));
            ZRoutedRpc.instance.Register(nameof(RPC_ReceiveExploration_OnExplore), new Action<long, ZPackage>(RPC_ReceiveExploration_OnExplore));
        }

        /// <summary>
        ///     Before SetMapData is applying loaded map, prevent enqueuing, since it is sent in gzip package
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Minimap), nameof(Minimap.SetMapData), PatchEventType.Prefix)]
        public static void PreventInitial(Minimap instance)
        {
            // Prevent queueing up loaded data
            isInSetMapData = true;
        }

        /// <summary>
        ///     After SetMapData is done, send it to the server
        ///     TODO: Check if configuration is loaded already, data should not be sent if map sharing is disabled
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Minimap), nameof(Minimap.SetMapData), PatchEventType.Postfix)]
        public static void InitialSendRequest(Minimap instance)
        {
            // if (Configuration.Current.MapServer.IsEnabled && Configuration.Current.MapServer.shareMapProgression)
            {
                Logger.LogInfo("Sending Map data initially to server");
                // After login, send map data to server (and get new map data back)
                var pkg = new ZPackage(CreateExplorationData().ToArray());
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_ReceiveExploration), pkg);
            } 

            isInSetMapData = false;
        }

        /// <summary>
        ///     Enqueue new exploration data if not added from SetMapData
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="result"></param>
        [PatchEvent(typeof(Minimap), nameof(Minimap.Explore), PatchEventType.Postfix)]
        public static void EnqueueExploreData(Minimap instance, int x, int y, bool result)
        {
            if (result && !isInSetMapData)
            {
                lock (explorationQueue)
                {
                    explorationQueue.Add(x + y * Minimap.instance.m_textureSize);
                }
            }
        }

        /// <summary>
        ///     Send queued exploration data to server
        /// </summary>
        /// <param name="instance"></param>
        [PatchEvent(typeof(Minimap), nameof(Minimap.UpdateExplore), PatchEventType.Postfix, 800)]
        public static void SendQueuedExploreData(Minimap instance)
        {
            if (explorationQueue.Count == 0)
            {
                return;
            }

            // disregard mini changes for now, lets build up some first
            if (explorationQueue.Count < 10)
            {
                return;
            }

            Logger.LogDebug($"UpdateExplore - sending newly explored locations to server ({explorationQueue.Count})");

            var toSend = new List<int>();
            lock (explorationQueue)
            {
                toSend.AddRange(explorationQueue.Distinct());
                explorationQueue.Clear();
            }

            var queueData = new ZPackage();
            queueData.Write(toSend.Count);
            foreach (var data in toSend)
            {
                queueData.Write(data);
            }

            // Invoke RPC on server and send data
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_ReceiveExploration_OnExplore), queueData);
        }

        /// <summary>
        ///     RPC to receive new exploration data from client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mapData"></param>
        public static void RPC_ReceiveExploration_OnExplore(long sender, ZPackage mapData)
        {
            if (mapData == null)
            {
                return;
            }

            if (ZNet.instance.IsServerInstance())
            {
                var numberOfEntries = mapData.ReadInt();
                Logger.LogInfo($"Received exploration diff data from client #{sender}, {numberOfEntries} items");

                while (numberOfEntries > 0)
                {
                    var toExplore = mapData.ReadInt();
                    Minimap.instance.m_explored[toExplore] = true;
                    numberOfEntries--;
                }
            }
        }

        // Helpers (copied from original assembly) to prevent enqueuing unneeded exploration data
        public static void ExploreLocal(Vector3 position)
        {
            var num = (int) Mathf.Ceil(Configuration.Current.MapServer.exploreRadius / Minimap.instance.m_pixelSize);
            var flag = false;
            int num2;
            int num3;
            Minimap.instance.WorldToPixel(position, out num2, out num3);
            for (var i = num3 - num; i <= num3 + num; i++)
            {
                for (var j = num2 - num; j <= num2 + num; j++)
                {
                    if (j >= 0 && i >= 0 && j < Minimap.instance.m_textureSize && i < Minimap.instance.m_textureSize &&
                        new Vector2(j - num2, i - num3).magnitude <= num && ExploreLocal(j, i))
                    {
                        flag = true;
                    }
                }
            }

            if (flag)
            {
                Minimap.instance.m_fogTexture.Apply();
            }
        }

        // Second helper
        public static bool ExploreLocal(int x, int y)
        {
            if (Minimap.instance.m_explored[y * Minimap.instance.m_textureSize + x])
            {
                return false;
            }

            Minimap.instance.m_fogTexture.SetPixel(x, y, new Color32(0, 0, 0, 0));
            Minimap.instance.m_explored[y * Minimap.instance.m_textureSize + x] = true;
            return true;
        }

        /// <summary>
        ///     Create compressed byte array
        /// </summary>
        /// <returns>compressed data</returns>
        public static byte[] CreateExplorationData()
        {
            var result = new MemoryStream();
            using (var gz = new ZlibStream(result, CompressionMode.Compress, CompressionLevel.BestCompression))
            {
                using (var binaryWriter = new BinaryWriter(gz))
                {
                    var idx = 0;

                    var state = Minimap.instance.m_explored[0];

                    binaryWriter.Write(state);
                    var count = 0;


                    var length = Minimap.instance.m_explored.Length;
                    while (idx < length)
                    {
                        while (idx < length && state == Minimap.instance.m_explored[idx])
                        {
                            count++;
                            idx++;
                        }

                        state = !state;
                        binaryWriter.Write(count);
                        count = 0;
                    }

                    binaryWriter.Flush();
                  //  gz.Flush();
                }

                return result.ToArray();
            }
        }
    }
}