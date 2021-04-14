﻿// Veilheim
// a Valheim mod
// 
// File:    NoMinimap.cs
// Project: Veilheim

using UnityEngine;
using Veilheim.Configurations;
using Veilheim.PatchEvents;

namespace Veilheim.Map
{
    public class NoMinimap : IPatchEventConsumer
    {
        [PatchEvent(typeof(Minimap), nameof(Minimap.SetMapMode), PatchEventType.BlockingPrefix)]
        public static void DontShowMinimap_Patch(Minimap instance, ref Minimap.MapMode mode, ref bool cancel)
        {
            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.showNoMinimap)
            {
                if ((Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !InventoryGui.IsVisible() && !Minimap.InTextInput())
                {
                    if (ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyMap") || (instance.m_mode == Minimap.MapMode.Large &&
                                                                                          (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))))
                    {
                        if (mode == Minimap.MapMode.Large)
                        {
                            return;
                        }
                        
                        if ((mode == Minimap.MapMode.Small) && (instance.m_mode == Minimap.MapMode.Large))
                        {
                            mode = Minimap.MapMode.None;
                        }
                    }
                    else
                    {
                        if ((mode == Minimap.MapMode.Small) && (instance.m_mode == Minimap.MapMode.None))
                        {
                            mode = Minimap.MapMode.None;
                        }
                    }
                }
            }
        }

        [PatchEvent(typeof(Minimap), nameof(Minimap.Awake), PatchEventType.Postfix)]
        public static void Minimap_Awake_NoMinimap_Patch(Minimap instance)
        {
            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.showNoMinimap)
            {
                instance.SetMapMode(Minimap.MapMode.None);
            }
        }
    }
}