// Veilheim
// a Valheim mod
// 
// File:    ProductionInputAmounts.cs
// Project: Veilheim

using Veilheim.Configurations;
using Veilheim.PatchEvents;

namespace Veilheim.Patches
{
    public class ProductionInputAmounts : IPatchEventConsumer
    {
        [PatchEvent(typeof(Smelter), nameof(Smelter.Awake), PatchEventType.Postfix)]
        public static void SetSmelterInputAmounts(Smelter instance)
        {
            if (Configuration.Current.ProductionInputAmounts.IsEnabled)
            {
                var prefab = instance.m_nview.GetPrefabName();
                if (prefab == "piece_spinningwheel")
                {
                    instance.m_maxOre = Configuration.Current.ProductionInputAmounts.spinningWheelFlachsAmount;
                }
                else if (prefab == "charcoal_kiln")
                {
                    instance.m_maxOre = Configuration.Current.ProductionInputAmounts.kilnWoodAmount;
                }
                else if (prefab == "blastfurnace")
                {
                    instance.m_maxOre = Configuration.Current.ProductionInputAmounts.blastfurnaceOreAmount;
                    instance.m_maxFuel = Configuration.Current.ProductionInputAmounts.blastfurnaceCoalAmount;
                }
                else if (prefab == "smelter")
                {
                    instance.m_maxOre = Configuration.Current.ProductionInputAmounts.furnaceOreAmount;
                    instance.m_maxFuel = Configuration.Current.ProductionInputAmounts.furnaceCoalAmount;
                }
                else if (prefab == "windmill")
                {
                    instance.m_maxOre = Configuration.Current.ProductionInputAmounts.windmillBarleyAmount;
                }
            }
        }
    }
}