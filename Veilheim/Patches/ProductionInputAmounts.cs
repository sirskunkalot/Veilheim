// Veilheim
// a Valheim mod
// 
// File:    ProductionInputAmounts.cs
// Project: Veilheim

using Jotunn.Utils;
using Veilheim.Utils;

namespace Veilheim.Patches
{
    public class ProductionInputAmounts 
    {
        [PatchInit(0)]
        public static void InitializePatches()
        {
            On.Smelter.Awake += SetSmelterInputAmounts;
        }

        private static void SetSmelterInputAmounts(On.Smelter.orig_Awake orig, Smelter self)
        {
            orig(self);

            if (ConfigUtil.Get<bool>("ProductionInputAmounts", "IsEnabled"))
            {
                var prefab = self.m_nview.GetPrefabName();
                if (prefab == "piece_spinningwheel")
                {
                    self.m_maxOre = ConfigUtil.Get<int>("ProductionInputAmounts", "spinningWheelFlachsAmount");
                }
                else if (prefab == "charcoal_kiln")
                {
                    self.m_maxOre = ConfigUtil.Get<int>("ProductionInputAmounts", "kilnWoodAmount");
                }
                else if (prefab == "blastfurnace")
                {
                    self.m_maxOre = ConfigUtil.Get<int>("ProductionInputAmounts", "blastfurnaceOreAmount");
                    self.m_maxFuel = ConfigUtil.Get<int>("ProductionInputAmounts", "blastfurnaceCoalAmount");
                }
                else if (prefab == "smelter")
                {
                    self.m_maxOre = ConfigUtil.Get<int>("ProductionInputAmounts", "furnaceOreAmount");
                    self.m_maxFuel = ConfigUtil.Get<int>("ProductionInputAmounts", "furnaceCoalAmount");
                }
                else if (prefab == "windmill")
                {
                    self.m_maxOre = ConfigUtil.Get<int>("ProductionInputAmounts", "windmillBarleyAmount");
                }
            }
        }
    }
}