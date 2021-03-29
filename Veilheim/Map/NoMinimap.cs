// Veilheim
// a Valheim mod
// 
// File:    NoMinimap.cs
// Project: Veilheim

using Veilheim.Configurations;
using Veilheim.PatchEvents;

namespace Veilheim.Map
{
    public class NoMinimap : IPatchEventConsumer
    {

        [PatchEvent(typeof(Minimap), nameof(Minimap.SetMapMode), PatchEventType.Prefix)]
        public static void DontShowMinimap_Patch(Minimap instance, ref Minimap.MapMode mode)
        {
            if (Configuration.Current.Map.showNoMinimap)
            {
                if (mode == Minimap.MapMode.Small)
                {
                    mode = Minimap.MapMode.None;
                }
            }
        }

    }
}