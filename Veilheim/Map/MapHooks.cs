// Veilheim
// a Valheim mod
// 
// File:    MapHooks.cs
// Project: Veilheim

using UnityEngine;
using Veilheim.Configurations;
using Veilheim.PatchEvents;

namespace Veilheim.Map
{
    public class Map_Patches : Payload
    {
        /// <summary>
        ///     CLIENT SIDE: Destroy the <see cref="PortalSelectionGUI" /> when active
        /// </summary>
        /// <param name="instance">TextInput instance</param>
        [PatchEvent(typeof(TextInput), nameof(TextInput.Hide), PatchEventType.Postfix)]
        public static void ResetPortalSelector(TextInput instance)
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
            instance.m_panel.transform.localPosition = new Vector3(0, 0f, 0);

            // restore mouse capture
            GameCamera.instance.m_mouseCapture = true;
            GameCamera.instance.UpdateMouseCapture();
        }

        /// <summary>
        ///     CLIENT SIDE: Creates a <see cref="PortalSelectionGUI" /> when interacting with a portal
        /// </summary>
        /// <param name="instance">Teleporter instance</param>
        /// <param name="human">unused</param>
        /// <param name="hold"></param>
        [PatchEvent(typeof(TeleportWorld), nameof(TeleportWorld.Interact), PatchEventType.Postfix)]
        public static void ShowPortalSelection(TeleportWorld instance, Humanoid human, bool hold)
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
            if (!PrivateArea.CheckAccess(instance.transform.position) || hold)
            {
                return;
            }

            PortalSelectionGUI.OpenPortalSelection();
        }
    }
}