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
    public class TeleporterOnMap
    {
        // Holder for our pins
        public static List<Minimap.PinData> addedPins = new List<Minimap.PinData>();

        // Helper to create PinData objects without adding them to the map
        public static Minimap.PinData AddPinHelper(Vector3 pos, Minimap.PinType type, string name, bool save, bool isChecked)
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

        public static void RPC_TeleporterSync(long sender, ZPackage teleporterZPackage)
        {
            // SERVER SIDE
            if (ZNet.instance.IsServer())
            {
                // Create package and send it to all clients
                var package = new ZPackage();

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

                package.Write(connected.Count);
                foreach (var connectedPortal in connected)
                {
                    package.Write(connectedPortal.m_position);
                    package.Write("*" + connectedPortal.GetString("tag"));
                }

                package.Write(unconnected.Count);
                foreach (var unconnectedPortal in unconnected)
                {
                    package.Write(unconnectedPortal.Value.m_position);
                    package.Write(unconnectedPortal.Value.GetString("tag"));
                }

                // Send only to single client (new peer) if zPackage is null
                if (teleporterZPackage == null)
                {
                    ZLog.Log("Sending portal information to client (new peer)");

                    // Send to single client (on new connections only)
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_TeleporterSync).Substring(4), package);
                }
                else
                {
                    ZLog.Log("Sending portal information to all clients");
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
            else
            {
                if (teleporterZPackage != null && teleporterZPackage.Size() > 0 && sender == ZRoutedRpc.instance.GetServerPeerID())
                {
                    var numConnectedPortals = teleporterZPackage.ReadInt();
                    ZLog.Log("Received portal data from server");

                    var checkList = new Dictionary<Vector3, string>();

                    // prevent MT crashing
                    lock (addedPins)
                    {
                        while (numConnectedPortals > 0)
                        {
                            var portalPosition = teleporterZPackage.ReadVector3();
                            var portalName = teleporterZPackage.ReadString();

                            ZLog.Log(portalName);
                            checkList.Add(portalPosition, portalName);

                            // Was pin already added?
                            var foundPin = addedPins.FirstOrDefault(x => x.m_pos == portalPosition);
                            if (foundPin != null)
                            {
                                // Did the pin's name change?
                                if (foundPin.m_name != portalName)
                                {
                                    // Remove pin at location and readd with new name
                                    addedPins.Remove(foundPin);
                                    addedPins.Add(AddPinHelper(portalPosition, Minimap.PinType.Icon4, portalName, false, false));
                                }
                            }
                            else
                            {
                                // Add new pin
                                addedPins.Add(AddPinHelper(portalPosition, Minimap.PinType.Icon4, portalName, false, false));
                            }

                            numConnectedPortals--;
                        }

                        var numUnconnectedPortals = teleporterZPackage.ReadInt();

                        while (numUnconnectedPortals > 0)
                        {
                            var portalPosition = teleporterZPackage.ReadVector3();
                            var portalName = teleporterZPackage.ReadString();
                            Debug.Log(portalName);

                            checkList.Add(portalPosition, portalName);

                            // Was pin already added?
                            var foundPin = addedPins.FirstOrDefault(x => x.m_pos == portalPosition);
                            if (foundPin != null)
                            {
                                // Did the pin's name change?
                                if (foundPin.m_name != portalName)
                                {
                                    // Remove pin at location and readd with new name
                                    addedPins.Remove(foundPin);
                                    addedPins.Add(AddPinHelper(portalPosition, Minimap.PinType.Icon4, portalName, false, false));
                                }
                            }
                            else
                            {
                                // Add new pin
                                addedPins.Add(AddPinHelper(portalPosition, Minimap.PinType.Icon4, portalName, false, false));
                            }

                            numUnconnectedPortals--;
                        }

                        // Remove destroyed portals from map
                        // doesn't really react on portal destruction, only works if after a portal was destroyed, someone set the name on another portal
                        foreach (var kv in addedPins.ToList())
                        {
                            if (checkList.All(x => x.Key != kv.m_pos))
                            {
                                addedPins.Remove(kv);
                            }
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        private static void Prefix()
        {
            //Config Sync
            ZRoutedRpc.instance.Register(nameof(TeleporterOnMap.RPC_TeleporterSync).Substring(4),
                new Action<long, ZPackage>(TeleporterOnMap.RPC_TeleporterSync));
        }
    }

    // React to setting tag on portal
    // Send special Chatmessage to server
    [HarmonyPatch(typeof(TeleportWorld), "RPC_SetTag", typeof(long), typeof(string))]
    public static class TeleportWorld_RPCSetTag_Patch
    {
        public static void Postfix(TeleportWorld __instance, long sender, string tag)
        {
            if (ZNet.instance.IsServer())
            {
                return;
            }

            // Force sending ZDO to server
            var temp = __instance.m_nview.GetZDO();

            ZDOMan.instance.GetZDO(temp.m_uid);

            ZDOMan.instance.GetPeer(ZRoutedRpc.instance.GetServerPeerID()).ForceSendZDO(temp.m_uid);
            Task.Factory.StartNew(() =>
            {
                // Wait for ZDO to be sent else server won't have accurate information to send back
                Thread.Sleep(5000);

                // Send trigger to server
                ZLog.Log("Sending message to server to trigger delivery of map icons after renaming portal");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(TeleporterOnMap.RPC_TeleporterSync).Substring(4),
                    new ZPackage());
            });
        }
    }


    // CLIENT SIDE
    [HarmonyPatch(typeof(Minimap), "SetMapData")]
    public static class Minimap_SetMapData_Patch
    {
        public static void Postfix()
        {
            if (ZNet.m_isServer)
            {
                return;
            }

            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.showPortalsOnMap)
            {
                ZLog.Log("Sending message to server to trigger delivery of map icons");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(TeleporterOnMap.RPC_TeleporterSync).Substring(4),
                    new ZPackage());
            }
        }
    }

    // CLIENT SIDE Set map pins after UpdateLocationPins
    [HarmonyPatch(typeof(Minimap), "UpdateLocationPins")]
    public static class Minimap_UpdateLocationPins_Patch
    {
        public static void Postfix()
        {
            if (ZNet.instance.IsClientInstance())
            {
                if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.showPortalsOnMap)
                {
                    List<Minimap.PinData> copy;

                    lock (TeleporterOnMap.addedPins)
                    {
                        copy = TeleporterOnMap.addedPins.ToList();
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
            }
        }
    }

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
            if (ZNet.instance.IsClientInstance())
            {
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
                    lock (TeleporterOnMap.addedPins)
                    {
                        singleTeleports.AddRange(TeleporterOnMap.addedPins.Where(x => !x.m_name.StartsWith("*")).OrderBy(x => x.m_name));
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


                    if (TextInput.instance.m_panel.transform.Find("OK") != null)
                    {
                        var originalButton = TextInput.instance.m_panel.transform.Find("OK").gameObject;

                        foreach (var pin in singleTeleports)
                        {
                            // Skip if it is the selected teleporter
                            if (pin.m_name == actualName)
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
            if (ZNet.instance.IsClientInstance())
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