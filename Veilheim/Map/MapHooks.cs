// Veilheim
// a Valheim mod
// 
// File:    MapHooks.cs
// Project: Veilheim

using Jotunn.Utils;
using UnityEngine;
using Veilheim.PatchEvents;
using Veilheim.Utils;

namespace Veilheim.Map
{
    public class Map_Patches : IPatchEventConsumer
    {

        [PatchInit(0)]
        public static void InitializePatches()
        {
            On.TextInput.Hide += ResetPortalSelector;
            On.TeleportWorld.Interact += ShowPortalSelection;
            On.Menu.IsVisible += PortalGUI_Mouselook_Patch;
        }

        /// <summary>
        ///     CLIENT SIDE: Disable mouselook when portal selection gui is visible
        /// </summary>
        private static bool PortalGUI_Mouselook_Patch(On.Menu.orig_IsVisible orig)
        {
            bool result = orig();
            result |= PortalSelectionGUI.IsVisible() && TextInput.instance.m_panel.activeSelf;
            return result;
        }

        /// <summary>
        ///     CLIENT SIDE: Creates a <see cref="PortalSelectionGUI" /> when interacting with a portal
        /// </summary>
        private static bool ShowPortalSelection(On.TeleportWorld.orig_Interact orig, TeleportWorld self, Humanoid human, bool hold)
        {
            bool result = orig(self, human, hold);
            // only act on clients
            if (ZNet.instance.IsServerInstance())
            {
                return result;
            }

            // must be enabled
            if (!ConfigUtil.Get<bool>("Map","IsEnabled") || !ConfigUtil.Get<bool>("Map","showPortalSelection"))
            {
                return result;
            }

            // i like my personal space
            if (!PrivateArea.CheckAccess(self.transform.position) || hold)
            {
                return result;
            }

            PortalSelectionGUI.OpenPortalSelection();

            return result;
        }

        /// <summary>
        ///     CLIENT SIDE: Destroy the <see cref="PortalSelectionGUI" /> when active
        /// </summary>
        private static void ResetPortalSelector(On.TextInput.orig_Hide orig, TextInput self)
        {
            orig(self);

            if (ZNet.instance.IsServerInstance())
            {
                return;
            }

            PortalSelectionGUI.Hide();

            // reset position of textinput panel
            self.m_panel.transform.localPosition = new Vector3(0, 0f, 0);

            // restore mouse capture
            GameCamera.instance.m_mouseCapture = true;
            GameCamera.instance.UpdateMouseCapture();
        }
    }
}