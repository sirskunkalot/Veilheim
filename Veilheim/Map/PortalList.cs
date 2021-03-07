using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Veilheim.Map
{
    /// <summary>
    /// Portal meta struct. Holds a portals coordinates,
    /// its tag and if it is connected to another portal or not.
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
    }

    public class PortalList : List<Portal>
    {
        /// <summary>
        /// Get the current list of <see cref="Portal"/> ZDOs
        /// </summary>
        /// <returns></returns>
        public static PortalList GetPortals()
        {
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
            foreach (var portal in connected)
            {
                ret.Add(new Portal(portal.m_position, portal.GetString("tag"), true));
            }
            foreach (var portal in unconnected)
            {
                ret.Add(new Portal(portal.Value.m_position, portal.Value.GetString("tag"), false));
            }
            return ret;
        }

        /// <summary>
        /// Create a <see cref="Portal"/> list from a <see cref="ZPackage"/>
        /// </summary>
        /// <param name="zpkg"></param>
        /// <returns></returns>
        public static PortalList FromZPackage(ZPackage zpkg)
        {
            var ret = new PortalList();
            
            var numConnectedPortals = zpkg.ReadInt();

            while (numConnectedPortals > 0)
            {
                var portalPosition = zpkg.ReadVector3();
                var portalName = zpkg.ReadString();

                Logger.LogInfo(portalName);
                ret.Add(new Portal(portalPosition, portalName, true));

                numConnectedPortals--;
            }

            var numUnconnectedPortals = zpkg.ReadInt();

            while (numUnconnectedPortals > 0)
            {
                var portalPosition = zpkg.ReadVector3();
                var portalName = zpkg.ReadString();

                Logger.LogInfo(portalName);
                ret.Add(new Portal(portalPosition, portalName, false));

                numUnconnectedPortals--;
            }

            return ret;
        }

        /// <summary>
        /// Create a <see cref="ZPackage"/> of this portal list
        /// </summary>
        /// <returns></returns>
        public ZPackage ToZPackage()
        {
            var package = new ZPackage();

            var connected = this.Where(x => x.m_con);

            package.Write(connected.Count());
            foreach (var connectedPortal in connected)
            {
                package.Write(connectedPortal.m_pos);
                package.Write("*" + connectedPortal.m_tag);
            }
            
            var unconnected = this.Where(x => !x.m_con);

            package.Write(unconnected.Count());
            foreach (var unconnectedPortal in unconnected)
            {
                package.Write(unconnectedPortal.m_pos);
                package.Write(unconnectedPortal.m_tag);
            }

            return package;
        }

    }
}
