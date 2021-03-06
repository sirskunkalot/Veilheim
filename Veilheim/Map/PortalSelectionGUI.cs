using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Veilheim.Configurations;

namespace Veilheim.Map
{
    class PortalSelectionGUI
    {
        public static RectTransform portalRect;

        private static readonly List<GameObject> teleporterButtons = new List<GameObject>();

        private static GameObject buttonList;
        private static GameObject TeleporterListScrollbar;

        private static GameObject viewport;

        public static void OpenPortalSelection()
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
                lock (PortalsOnMap.portalPins)
                {
                    singleTeleports.AddRange(PortalsOnMap.portalPins.Where(x => !x.m_name.StartsWith("*")).OrderBy(x => x.m_name));
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
                        if ((pin.m_name == actualName) || (actualName == "<unnamed>" && string.IsNullOrEmpty(pin.m_name)))
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

        /// <summary>
        /// Create background for teleporter button list
        /// </summary>
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

    /// <summary>
    /// CLIENT SIDE: Create list of teleporter tags to choose from
    /// </summary>
    [HarmonyPatch(typeof(TeleportWorld), "Interact", typeof(Humanoid), typeof(bool))]
    public static class TeleportWorld_Interact_Patch
    {
        public static void Postfix(ref TextInput __instance, Humanoid human, bool hold, ref bool __result)
        {
            // only act on clients
            if (ZNet.instance.IsServerInstance())
            {
                return;
            }
            // must be enabled
            if (!Configuration.Current.Map.IsEnabled || !Configuration.Current.Map.showPortalSelection)
            {
                return;
            }
            // i like my personal space
            if (!PrivateArea.CheckAccess(__instance.transform.position, 0f, true) || hold)
            {
                return;
            }

            PortalSelectionGUI.OpenPortalSelection();
        }
    }

    /// <summary>
    /// CLIENT SIDE: Destroy portal tag list
    /// </summary>
    [HarmonyPatch(typeof(TextInput), "Hide")]
    public static class TextInput_Hide_Patch
    {
        public static void Postfix(TextInput __instance)
        {
            if (ZNet.instance.IsServerInstance())
            {
                return;
            }
            if (PortalSelectionGUI.portalRect != null)
            {
                if (PortalSelectionGUI.portalRect.gameObject.activeSelf)
                {
                    // hide teleporter button box
                    PortalSelectionGUI.portalRect.gameObject.SetActive(false);
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
