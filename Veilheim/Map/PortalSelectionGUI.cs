using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Veilheim.Configurations;

namespace Veilheim.Map
{
    /// <summary>
    /// When renaming/tagging a portal read all tags from unconnected portals in the world and make a list of them to tag it.
    /// Coded by https://github.com/Algorithman
    /// </summary>
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
                Logger.LogInfo("Generating portal selection");

                // set position of textinput (a bit higher)
                TextInput.instance.m_panel.transform.localPosition = new Vector3(0, 270.0f, 0);

                // Create gameobject(s) if not exist
                if (portalRect == null)
                {
                    portalRect = GenerateGUI();
                }

                // Generate list of unconnected portals from ZDOMan
                var singlePortals = PortalList.GetPortals().Where(x => !x.m_con);

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
                var lines = singlePortals.Count() / 3;

                // Get name of portal
                var currentTag = TextInput.instance.m_textField.text;
                if (string.IsNullOrEmpty(currentTag))
                {
                    currentTag = "<unnamed>";
                    TextInput.instance.m_textField.text = currentTag;
                }

                if (TextInput.instance.m_panel.transform.Find("OK") != null)
                {
                    var originalButton = TextInput.instance.m_panel.transform.Find("OK").gameObject;

                    foreach (var portal in singlePortals)
                    {
                        // Skip if it is the selected teleporter
                        if ((portal.m_tag == currentTag) || (currentTag == "<unnamed>" && string.IsNullOrEmpty(portal.m_tag)))
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
                        newButton.GetComponentInChildren<Text>().text = portal.m_tag;

                        // add event payload
                        newButton.GetComponentInChildren<Button>().onClick.AddListener(() =>
                        {
                            // Set input field text to new name
                            TextInput.instance.m_textField.text = portal.m_tag;

                            // simulate enter key
                            TextInput.instance.OnEnter(portal.m_tag);

                            // hide textinput
                            TextInput.instance.Hide();
                        });

                        // Add to local list
                        teleporterButtons.Add(newButton);
                        idx++;
                    }
                }

                if (singlePortals.Count() > 0)
                {
                    // show buttonlist only if single teleports are available to choose from
                    portalRect.gameObject.SetActive(true);
                }

                // Set anchor
                buttonList.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(singlePortals.Count() / 3) * 25.0f);

                // Set size
                buttonList.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, singlePortals.Count() / 3 * 50.0f + 50f);

                // release mouselock
                GameCamera.instance.m_mouseCapture = false;
                GameCamera.instance.UpdateMouseCapture();
            }
        }

        /// <summary>
        /// Clones the original valheim <see cref="InventoryGui"/> background <see cref="GameObject"/>
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

        /// <summary>
        /// Creates the base canvas for the portal buttons
        /// </summary>
        /// <returns></returns>
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
}
