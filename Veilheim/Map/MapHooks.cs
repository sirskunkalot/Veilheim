using HarmonyLib;
using UnityEngine;
using Veilheim.Configurations;

namespace Veilheim.Map
{

    /// <summary>
    /// CLIENT SIDE: Creates a <see cref="PortalSelectionGUI"/> when interacting with a portal
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
    /// CLIENT SIDE: Destroy the <see cref="PortalSelectionGUI"/> when active
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