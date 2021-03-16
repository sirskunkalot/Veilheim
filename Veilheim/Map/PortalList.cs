// Veilheim
// a Valheim mod
// 
// File:    PortalList.cs
// Project: Veilheim

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Veilheim.Map
{
    /// <summary>
    ///     Portal meta struct. Holds a portals coordinates,
    ///     its tag and if it is connected to another portal or not.
    /// </summary>
    public struct Portal
    {
        public readonly Vector3 m_pos;
        public readonly string m_tag;
        public readonly bool m_con;

        public Portal(Vector3 pos, string tag, bool con)
        {
            m_pos = pos;
            m_tag = tag;
            m_con = con;
        }

        public override string ToString()
        {
            return $"{m_tag}@{m_pos}";
        }
    }

    public class PortalList : List<Portal>
    {
        /// <summary>
        ///     Get the current list of <see cref="Portal" /> ZDOs
        /// </summary>
        /// <returns></returns>
        public static PortalList GetPortals()
        {
            Logger.LogDebug("Creating portal list from ZDOMan");

            // Collect all portal locations/names
            var connected = new List<ZDO>();
            var unconnected = new Dictionary<string, ZDO>();

            foreach (var zdoarray in ZDOMan.instance.m_objectsBySector)
            {
                if (zdoarray != null)
                {
                    foreach (var zdo in zdoarray.Where(x => x.m_prefab == -661882940))
                    {
                        var tag = zdo.GetString("tag");

                        if (!unconnected.ContainsKey(tag))
                        {
                            unconnected.Add(tag, zdo);
                        }
                        else
                        {
                            connected.Add(zdo);
                            connected.Add(unconnected[tag]);
                            unconnected.Remove(tag);
                        }
                    }
                }
            }

            // Make a list of all Portals
            var ret = new PortalList();
            Portal portal;
            Logger.LogDebug("Connected Portals");
            foreach (var entry in connected.OrderBy(x => x.GetString("tag")))
            {
                portal = new Portal(entry.m_position, entry.GetString("tag"), true);
                Logger.LogDebug(portal);
                ret.Add(portal);
            }

            Logger.LogDebug("Unconnected Portals");
            foreach (var entry in unconnected.OrderBy(x => x.Value.GetString("tag")))
            {
                portal = new Portal(entry.Value.m_position, entry.Value.GetString("tag"), false);
                Logger.LogDebug(portal);
                ret.Add(portal);
            }

            return ret;
        }

        /// <summary>
        ///     Create a <see cref="Portal" /> list from a <see cref="ZPackage" />
        /// </summary>
        /// <param name="zpkg"></param>
        /// <returns></returns>
        public static PortalList FromZPackage(ZPackage zpkg)
        {
            Logger.LogDebug("Deserializing portal list from ZPackage");

            var ret = new PortalList();

            var numConnectedPortals = zpkg.ReadInt();

            while (numConnectedPortals > 0)
            {
                var portalPosition = zpkg.ReadVector3();
                var portalName = zpkg.ReadString();

                Logger.LogDebug($"{portalName}@{portalPosition}");
                ret.Add(new Portal(portalPosition, portalName, true));

                numConnectedPortals--;
            }

            var numUnconnectedPortals = zpkg.ReadInt();

            while (numUnconnectedPortals > 0)
            {
                var portalPosition = zpkg.ReadVector3();
                var portalName = zpkg.ReadString();

                Logger.LogDebug($"{portalName}@{portalPosition}");
                ret.Add(new Portal(portalPosition, portalName, false));

                numUnconnectedPortals--;
            }

            return ret;
        }

        /// <summary>
        ///     Create a <see cref="ZPackage" /> of this portal list
        /// </summary>
        /// <returns></returns>
        public ZPackage ToZPackage()
        {
            Logger.LogDebug("Serializing portal list to ZPackage");

            var package = new ZPackage();

            var connected = this.Where(x => x.m_con);

            package.Write(connected.Count());
            foreach (var connectedPortal in connected)
            {
                Logger.LogDebug($"{connectedPortal.m_tag}@{connectedPortal.m_pos}");
                package.Write(connectedPortal.m_pos);
                package.Write(connectedPortal.m_tag);
            }

            var unconnected = this.Where(x => !x.m_con);

            package.Write(unconnected.Count());
            foreach (var unconnectedPortal in unconnected)
            {
                Logger.LogDebug($"{unconnectedPortal.m_tag}@{unconnectedPortal.m_pos}");
                package.Write(unconnectedPortal.m_pos);
                package.Write(unconnectedPortal.m_tag);
            }

            return package;
        }
    }
}