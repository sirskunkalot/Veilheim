using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Veilheim.Configurations;
using Veilheim.PatchEvents;

// ToDo add packet system to convey map markers

namespace Veilheim.Map
{

    public class SharedMapPatches : Payload
    {
        
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
                    List<ZNet.PlayerInfo> tempPlayerInfos = new List<ZNet.PlayerInfo>();
                    ZNet.instance.GetOtherPublicPlayers(tempPlayerInfos);
                    foreach (var player in tempPlayerInfos)
                    {
                        instance.Explore(player.m_position, Configuration.Current.MapServer.exploreRadius);
                    }
                }
            }

            instance.Explore(Player.m_localPlayer.transform.position,Configuration.Current.MapServer.exploreRadius);
        }
    }
}
