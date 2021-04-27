// Veilheim
// a Valheim mod
// 
// File:    NoMinimap.cs
// Project: Veilheim

using Jotunn.Utils;
using UnityEngine;
using Veilheim.Utils;

namespace Veilheim.Map
{
    public class NoMinimap
    {

        [PatchInit(0)]
        public static void InitializePatches()
        {
            On.Minimap.Awake += Minimap_Awake_NoMinimap_Patch;
            On.Minimap.SetMapMode += DontShowMinimap_Patch;
        }

        private static void DontShowMinimap_Patch(On.Minimap.orig_SetMapMode orig, Minimap self, int mode)
        {
            if (ConfigUtil.Get<bool>("Map", "showNoMinimap"))
            {
                if ((Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !InventoryGui.IsVisible() && !Minimap.InTextInput())
                {
                    if (ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyMap") || (self.m_mode == Minimap.MapMode.Large &&
                                                                                          (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))))
                    {
                        if (mode == (int)Minimap.MapMode.Large)
                        {
                            return;
                        }

                        if ((mode == (int)Minimap.MapMode.Small) && (self.m_mode == Minimap.MapMode.Large))
                        {
                            mode = (int)Minimap.MapMode.None;
                        }
                    }
                    else
                    {
                        if ((mode == (int)Minimap.MapMode.Small) && (self.m_mode == Minimap.MapMode.None))
                        {
                            mode = (int)Minimap.MapMode.None;
                        }
                    }
                }
            }

            orig(self, mode);
        }

        private static void Minimap_Awake_NoMinimap_Patch(On.Minimap.orig_Awake orig, Minimap self)
        {
            orig(self);

            if (ConfigUtil.Get<bool>("Map", "showNoMinimap"))
            {
                self.SetMapMode(Minimap.MapMode.None);
            }
        }
    }
}