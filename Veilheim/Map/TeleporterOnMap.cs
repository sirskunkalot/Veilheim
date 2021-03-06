using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Veilheim.Configurations;

namespace Veilheim.Map
{
    public class Portal
    {
        public Vector3 m_pos;
        public string m_tag;
        public bool m_con;

        public Portal(Vector3 pos, string tag, bool con)
        {
            m_pos = pos;
            m_tag = tag;
            m_con = con;
        }
    }

    public class TeleporterOnMap
    {
        // Holder for our pins
        public static List<Minimap.PinData> portalPins = new List<Minimap.PinData>();

        // Helper to create PinData objects without adding them to the map
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

        public static List<Portal> GetPortals()
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
            var ret = new List<Portal>();
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

        public static void CreatePins(List<Portal> portals)
        {
            // prevent MT crashing
            lock (portalPins)
            {
                // Add connected portals (separated connected and unconnected, maybe show another icon?)
                foreach (var portal in portals.FindAll(x => x.m_con))
                {
                    Logger.LogInfo(portal.m_tag);

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
                    Logger.LogInfo(portal.m_tag);

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
        }

        public static void UpdatePins()
        {
            var portals = GetPortals();
            CreatePins(portals);
        }

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

        public static void RPC_TeleporterSync(long sender, ZPackage teleporterZPackage)
        {
            // SERVER SIDE
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                // Create package and send it to all clients
                var package = new ZPackage();

                var portals = GetPortals();
                var connected = portals.Where(x => x.m_con);
                var unconnected = portals.Where(x => !x.m_con);

                package.Write(connected.Count());
                foreach (var connectedPortal in connected)
                {
                    package.Write(connectedPortal.m_pos);
                    package.Write("*" + connectedPortal.m_tag);
                }

                package.Write(unconnected.Count());
                foreach (var unconnectedPortal in unconnected)
                {
                    package.Write(unconnectedPortal.m_pos);
                    package.Write(unconnectedPortal.m_tag);
                }

                // Send only to single client (new peer) if zPackage is null
                if (teleporterZPackage == null)
                {
                    Logger.LogInfo("Sending portal information to client (new peer)");

                    // Send to single client (on new connections only)
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_TeleporterSync).Substring(4), package);
                }
                else
                {
                    Logger.LogInfo("Sending portal information to all clients");
                    foreach (var peer in ZNet.instance.m_peers)
                    {
                        if (!peer.m_server)
                        {
                            // Send to all clients
                            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, nameof(RPC_TeleporterSync).Substring(4), package);
                        }
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

                    var portals = new List<Portal>();

                    var numConnectedPortals = teleporterZPackage.ReadInt();

                    while (numConnectedPortals > 0)
                    {
                        var portalPosition = teleporterZPackage.ReadVector3();
                        var portalName = teleporterZPackage.ReadString();

                        Logger.LogInfo(portalName);
                        portals.Add(new Portal(portalPosition, portalName, true));

                        numConnectedPortals--;
                    }

                    var numUnconnectedPortals = teleporterZPackage.ReadInt();

                    while (numUnconnectedPortals > 0)
                    {
                        var portalPosition = teleporterZPackage.ReadVector3();
                        var portalName = teleporterZPackage.ReadString();

                        Logger.LogInfo(portalName);
                        portals.Add(new Portal(portalPosition, portalName, false));

                        numUnconnectedPortals--;
                    }

                    CreatePins(portals);
                }
            }
        }
    }

    /// <summary>
    ///     Register TeleporterSync RPC call
    /// </summary>
    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        private static void Prefix()
        {
            ZRoutedRpc.instance.Register(nameof(TeleporterOnMap.RPC_TeleporterSync).Substring(4),
                new Action<long, ZPackage>(TeleporterOnMap.RPC_TeleporterSync));
        }
    }

    /// <summary>
    ///     CLIENT SIDE: Update portals after SetMapData on Minimap
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "SetMapData")]
    public static class Minimap_SetMapData_Patch
    {
        public static void Postfix()
        {
            if (ZNet.instance.IsServerInstance()) return;

            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.showPortalsOnMap)
            {
                if (ZNet.instance.IsLocalInstance())
                {
                    Logger.LogInfo("Updating portals");
                    TeleporterOnMap.UpdatePins();
                }

                if (ZNet.instance.IsClientInstance())
                {
                    Logger.LogInfo("Sending message to server to trigger delivery of portals");
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(TeleporterOnMap.RPC_TeleporterSync).Substring(4),
                        new ZPackage());
                }
            }
        }
    }

    /// <summary>
    ///     CLIENT SIDE: React to setting tag on portal
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), "RPC_SetTag", typeof(long), typeof(string))]
    public static class TeleportWorld_RPCSetTag_Patch
    {
        public static void Postfix(TeleportWorld __instance, long sender, string tag)
        {
            if (ZNet.instance.IsLocalInstance())
            {
                // Update portal list
                Logger.LogInfo("Updating portals");
                TeleporterOnMap.UpdatePins();

                // Deliver updated list to all peers
                Logger.LogInfo("Trigger delivery of portals after renaming portal");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(TeleporterOnMap.RPC_TeleporterSync).Substring(4),
                    new ZPackage());
            }

            if (ZNet.instance.IsClientInstance())
            {
                // Force sending ZDO to server, when not local instance
                var temp = __instance.m_nview.GetZDO();

                ZDOMan.instance.GetZDO(temp.m_uid);

                ZDOMan.instance.GetPeer(ZRoutedRpc.instance.GetServerPeerID()).ForceSendZDO(temp.m_uid);
                
                Task.Factory.StartNew(() =>
                {
                    // Wait for ZDO to be sent else server won't have accurate information to send back
                    Thread.Sleep(5000);

                    // Send trigger to server
                    Logger.LogInfo("Sending message to server to trigger delivery of portals after renaming portal");
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(TeleporterOnMap.RPC_TeleporterSync).Substring(4),
                        new ZPackage());
                });
            }
        }
    }

    /// <summary>
    ///     CLIENT SIDE: Add pins to map after UpdateLocationPins on Minimap
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "UpdateLocationPins")]
    public static class Minimap_UpdateLocationPins_Patch
    {
        public static void Postfix()
        {
            if (ZNet.instance.IsServerInstance()) { return; }
            
            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.showPortalsOnMap)
            {
                TeleporterOnMap.UpdateMinimap();
            }
        }
    }

    /// <summary>
    ///     CLIENT SIDE: Create list of teleporter tags to choose from after Interact on TeleportWorld
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), "Interact", typeof(Humanoid), typeof(bool))]
    public static class TeleportWorld_Interact_Patch
    {

        public static RectTransform portalRect;

        private static readonly List<GameObject> teleporterButtons = new List<GameObject>();

        private static GameObject buttonList;
        private static GameObject TeleporterListScrollbar;

        private static GameObject viewport;

        public static void Postfix(ref TextInput __instance, Humanoid human, bool hold, ref bool __result)
        {
            // only act on clients
            //if (ZNet.instance.IsClientInstance())
            if (!ZNet.instance.IsServerInstance())
            {
                if (!PrivateArea.CheckAccess(__instance.transform.position, 0f, true) || hold)
                {
                    return;
                }
                if (TextInput.instance.m_panel.activeSelf)
                {
                    // set position of textinput (a bit higher)
                    TextInput.instance.m_panel.transform.localPosition = new Vector3(0, 270.0f, 0);

                    // Create gameobject(s) if not exist
                    if (portalRect == null)
                    {
                        portalRect = GenerateGUI();
                    }

                    // Generate list of single teleporters (exclude portal names starting with * )
                    var singleTeleports = new List<Minimap.PinData>();
                    lock (TeleporterOnMap.portalPins)
                    {
                        singleTeleports.AddRange(TeleporterOnMap.portalPins.Where(x => !x.m_name.StartsWith("*")).OrderBy(x => x.m_name));
                    }

                    // remove all buttons from earlier calls
                    foreach (var oldbt in teleporterButtons)
                    {
                        if (oldbt != null)
                        {
                            GameObject.Destroy(oldbt);
                        }
                    }

                    // empty the local list
                    teleporterButtons.Clear();

                    var idx = 0;
                    // calculate number of lines
                    var lines = singleTeleports.Count / 3;

                    // Get name of portal
                    var actualName = TextInput.instance.m_textField.text;
                    if (string.IsNullOrEmpty(actualName))
                    {
                        actualName = "<unnamed>";
                        TextInput.instance.m_textField.text = actualName;
                    }


                    if (TextInput.instance.m_panel.transform.Find("OK") != null)
                    {
                        var originalButton = TextInput.instance.m_panel.transform.Find("OK").gameObject;

                        foreach (var pin in singleTeleports)
                        {
                            // Skip if it is the selected teleporter
                            if ((pin.m_name == actualName) || (actualName=="<unnamed>" && string.IsNullOrEmpty(pin.m_name)))
                            {
                                continue;
                            }

                            // clone button
                            var newButton = GameObject.Instantiate(originalButton, buttonList.GetComponent<RectTransform>());
                            newButton.name = "TP" + idx;

                            // set position
                            newButton.transform.localPosition = new Vector3(-600.0f / 3f + idx % 3 * 600.0f / 3f, lines * 25f - idx / 3 * 50f, 0f);

                            // enable
                            newButton.SetActive(true);

                            // set button text
                            newButton.GetComponentInChildren<Text>().text = pin.m_name;

                            // add event payload
                            newButton.GetComponentInChildren<Button>().onClick.AddListener(() =>
                            {
                                // Set input field text to new name
                                TextInput.instance.m_textField.text = pin.m_name;

                                // simulate enter key
                                TextInput.instance.OnEnter(pin.m_name);

                                // hide textinput
                                TextInput.instance.Hide();
                            });

                            // Add to local list
                            teleporterButtons.Add(newButton);
                            idx++;
                        }
                    }

                    if (singleTeleports.Count > 0)
                    {
                        // show buttonlist only if single teleports are available to choose from
                        portalRect.gameObject.SetActive(true);
                    }

                    // Set anchor
                    buttonList.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(singleTeleports.Count / 3) * 25.0f);

                    // Set size
                    buttonList.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, singleTeleports.Count / 3 * 50.0f + 50f);

                    // release mouselock
                    GameCamera.instance.m_mouseCapture = false;
                    GameCamera.instance.UpdateMouseCapture();
                }
            }
        }

        /// <summary>
        ///     Create background for teleporter button list
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static RectTransform GetOrCreateBackground()
        {
            var transform = InventoryGui.instance.m_playerGrid.transform.parent.parent.parent.parent.Find(nameof(portalRect));
            var flag = transform == null;
            if (flag)
            {
                var background = InventoryGui.instance.m_playerGrid.transform.parent.Find("Bkg").gameObject;
                var newBackground = GameObject.Instantiate(background, background.transform.parent.parent.parent.parent);
                newBackground.name = nameof(portalRect);
                newBackground.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1);
                transform = newBackground.transform;
                newBackground.SetActive(true);
            }

            return transform as RectTransform;
        }

        private static RectTransform GenerateGUI()
        {
            // Create root gameobject with background image
            var portalRect = GetOrCreateBackground();

            // Clone scrollbar from existing in containergrid
            if (TeleporterListScrollbar == null)
            {
                // instantiate clone of scrollbar
                TeleporterListScrollbar = GameObject.Instantiate(InventoryGui.instance.m_containerGrid.m_scrollbar.gameObject, portalRect);
                TeleporterListScrollbar.name = nameof(TeleporterListScrollbar);
                var scrollBarRect = TeleporterListScrollbar.GetComponent<RectTransform>();

                // set size
                scrollBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200.0f);

                // set position
                scrollBarRect.localPosition = new Vector3(280f, 0, 0);

                // set scale
                scrollBarRect.localScale = new Vector3(1f, 0.9f, 1f);
            }

            // Set local position
            portalRect.localPosition = new Vector3(0, -100, 0);

            // Anchor to 0,0
            portalRect.anchoredPosition = new Vector2(0, 0);

            // Set size
            portalRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 640);
            portalRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 220);

            // and scale
            portalRect.localScale = new Vector3(1f, 1f, 1f);

            // Add scrollrect component
            portalRect.gameObject.AddComponent<ScrollRect>();

            // Movement elastic
            portalRect.gameObject.GetComponent<ScrollRect>().movementType = ScrollRect.MovementType.Elastic;

            // Vertical
            portalRect.gameObject.GetComponent<ScrollRect>().vertical = true;

            // link to scrollbar gameobject
            portalRect.gameObject.GetComponent<ScrollRect>().verticalScrollbar = TeleporterListScrollbar.GetComponent<Scrollbar>();

            // Set sensitivity
            portalRect.gameObject.GetComponent<ScrollRect>().scrollSensitivity = 10f;

            // Generate viewport gameobject
            viewport = new GameObject("ScrollViewportTeleporter", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));

            // Link to root object
            viewport.transform.SetParent(portalRect);

            // link viewport to scrollrect
            portalRect.gameObject.GetComponent<ScrollRect>().viewport = viewport.GetComponent<RectTransform>();

            var viewportRect = viewport.GetComponent<RectTransform>();

            // enable Mask2D
            viewportRect.GetComponent<RectMask2D>().enabled = true;

            // Set anchor
            viewportRect.anchoredPosition = new Vector2(0, 0);

            // Set size
            viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600);
            viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200);

            // Set scale
            viewportRect.localScale = new Vector3(0.9f, 0.9f, 0.9f);

            // Set position
            viewportRect.localPosition = new Vector3(0, 0, 0);

            // Finally create the real content object
            buttonList = new GameObject("BtnList", typeof(RectTransform), typeof(CanvasRenderer));

            // link to viewport
            buttonList.transform.SetParent(viewport.transform);

            var rectButtonList = buttonList.GetComponent<RectTransform>();

            // Set anchor
            rectButtonList.anchoredPosition = new Vector2(0, 0);

            // Set size
            rectButtonList.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
            rectButtonList.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 600f);

            // Set position
            rectButtonList.localPosition = new Vector3(0, 100f, 0);

            // Set scale
            rectButtonList.localScale = new Vector3(1f, 1f, 1f);

            // Link content to scrollrect component
            portalRect.GetComponent<ScrollRect>().content = rectButtonList;

            return portalRect;
        }
    }

    [HarmonyPatch(typeof(TextInput), "Hide")]
    public static class TextInput_Hide_Patch
    {
        public static void Postfix(TextInput __instance)
        {
            if (!ZNet.instance.IsServerInstance())
            {
                if (TeleportWorld_Interact_Patch.portalRect != null)
                {
                    if (TeleportWorld_Interact_Patch.portalRect.gameObject.activeSelf)
                    {
                        // hide teleporter button box
                        TeleportWorld_Interact_Patch.portalRect.gameObject.SetActive(false);
                    }
                }

                // reset position of textinput panel
                TextInput.instance.m_panel.transform.localPosition = new Vector3(0, 0f, 0);

                // restore mouse capture
                GameCamera.instance.m_mouseCapture = true;
                GameCamera.instance.UpdateMouseCapture();
            }
        }
    }
}